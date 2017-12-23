// Reference: System.Drawing
// Requires: Clans

namespace Oxide.Plugins
{
  using Oxide.Core;
  using Oxide.Core.Configuration;
  using Oxide.Core.Plugins;
  using System.Collections.Generic;
  using UnityEngine;

  [Info("RustFactions", "chucklenugget", "0.7.0")]
  public partial class RustFactions : RustPlugin
  {

    // Definitions -----------------------------------------------------------------------------------------

    [PluginReference] Plugin Clans;

    DynamicConfigFile DataFile;
    RustFactionsOptions Options;

    UserManager Users;
    AreaManager Areas;
    ClaimManager Claims;
    TaxManager Taxes;
    BadlandsManager Badlands;

    Dictionary<uint, StorageContainer> TaxChests = new Dictionary<uint, StorageContainer>();
    uint CurrentMapOverlayImageId;

    const string PERM_CHANGE_CLAIMS = "rustfactions.claims";
    const string PERM_CHANGE_BADLANDS = "rustfactions.badlands";

    // Lifecycle Hooks -------------------------------------------------------------------------------------

    void Init()
    {
      PrintToChat($"{this.Title} {this.Version} initialized.");

      Users = new UserManager(this);
      Areas = new AreaManager(this);
      Claims = new ClaimManager(this);
      Taxes = new TaxManager(this);
      Badlands = new BadlandsManager(this);
    }

    void Loaded()
    {
      InitLang();

      DataFile = Interface.Oxide.DataFileSystem.GetFile("RustFactions");

      Options = LoadOptions(Config);
      Puts("Area Claims are " + (Options.EnableAreaClaims ? "enabled" : "disabled"));
      Puts("Taxation is " + (Options.EnableTaxation ? "enabled" : "disabled"));
      Puts("Badlands are " + (Options.EnableBadlands ? "enabled" : "disabled"));

      LoadData(this, DataFile);
      Puts($"Loaded {Claims.Count} area claims.");
      Puts($"Loaded {Taxes.Count} area claims.");
      Puts($"Loaded {Badlands.Count} badlands areas.");
    }

    void Unload()
    {
      Users.Destroy();
      Areas.Destroy();
    }

    void OnServerInitialized()
    {
      if (Clans == null)
        PrintWarning("RustFactions requires the Rust:IO Clans plugin, but it was not found!");

      Areas.Init();
      Users.Init();
      CacheTaxChests();
      GenerateMapOverlayImage();

      permission.RegisterPermission(PERM_CHANGE_BADLANDS, this);
      permission.RegisterPermission(PERM_CHANGE_CLAIMS, this);
    }

    void OnServerSave()
    {
      SaveData(DataFile);
    }

    void OnPlayerInit(BasePlayer player)
    {
      if (player == null) return;
      if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot) || player.IsSleeping())
      {
        timer.In(2, () => OnPlayerInit(player));
        return;
      }

      Users.Add(player);
    }

    void OnPlayerDisconnected(BasePlayer player)
    {
      if (player != null)
        Users.Remove(player);
    }

    // Game Event Hooks ------------------------------------------------------------------------------------

    [ChatCommand("cancel")]
    void OnCancelCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);

      if (user.PendingInteraction == Interaction.None)
      {
        SendMessage(player, Messages.NoInteractionInProgress);
        return;
      }

      SendMessage(player, Messages.InteractionCanceled);
      user.PendingInteraction = Interaction.None;
    }

    void OnHammerHit(BasePlayer player, HitInfo hit)
    {
      User user = Users.Get(player);

      switch (user.PendingInteraction)
      {
        case Interaction.AddingClaim:
          if (TryAddClaim(player, hit))
            user.PendingInteraction = Interaction.None;
          break;
        case Interaction.RemovingClaim:
          if (TryRemoveClaim(player, hit))
            user.PendingInteraction = Interaction.None;
          break;
        case Interaction.SelectingHeadquarters:
          if (TrySetHeadquarters(player, hit))
            user.PendingInteraction = Interaction.None;
          break;
        case Interaction.SelectingTaxChest:
          if (TrySetTaxChest(player, hit))
            user.PendingInteraction = Interaction.None;
          break;
        default:
          break;
      }
    }

    void OnEntityKill(BaseNetworkable entity)
    {
      // If a player dies in an area, remove them from the area.
      var player = entity as BasePlayer;
      if (player != null)
      {
        User user = Users.Get(player);
        if (user.CurrentArea != null)
        {
          user.CurrentArea.Players.Remove(player);
          user.CurrentArea = null;
        }
      }

      // If a claim TC is destroyed, remove the claim from the area.
      var cupboard = entity as BuildingPrivlidge;
      if (cupboard != null)
      {
        var claim = Claims.GetByCupboard(cupboard);
        if (claim != null)
        {
          PrintToChat("<color=#ff0000ff>AREA CLAIM LOST:</color> [{0}] has lost its claim on {1}, because the tool cupboard was destroyed!", claim.FactionId, claim.AreaId);
          Claims.Remove(claim);
        }
      }

      // If a tax container is destroyed, remove it from the tax policy.
      if (Options.EnableTaxation)
      {
        var container = entity as StorageContainer;
        if (container != null)
        {
          var policy = Taxes.Get(container);
          if (policy != null)
          {
            Puts($"[{policy.FactionId}] has lost their ability to tax because their tax chest was destroyed.");
            Taxes.RemoveTaxChest(policy);
            TaxChests.Remove(entity.net.ID);
          }
        }
      }
    }

    void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      ChargeTaxIfApplicable(dispenser, entity, item);
      AwardBadlandsBonusIfApplicable(dispenser, entity, item);
    }

    void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      ChargeTaxIfApplicable(dispenser, entity, item);
      AwardBadlandsBonusIfApplicable(dispenser, entity, item);
    }

    void OnPlayerEnterArea(Area area, BasePlayer player)
    {
      User user = Users.Get(player);

      Area previousArea = user.CurrentArea;
      Claim previousClaim = null;
      if (previousArea != null)
        previousClaim = Claims.Get(previousArea);

      Claim currentClaim = Claims.Get(area);

      user.CurrentArea = area;
      user.LocationPanel.Refresh();

      if (Badlands.Contains(area.Id) && (previousArea == null || !Badlands.Contains(previousArea)))
      {
        // The player has crossed into the badlands.
        SendMessage(player, Messages.EnteredBadlands);
      }
      else if (currentClaim == null && previousClaim != null)
      {
        // The player has crossed a border between the land of a faction and unclaimed land.
        SendMessage(player, Messages.EnteredUnclaimedArea);
      }
      else if (currentClaim != null && previousClaim == null)
      {
        // The player has crosed a border between unclaimed land and the land of a faction.
        SendMessage(player, Messages.EnteredClaimedArea, currentClaim.FactionId);
      }
      else if ((currentClaim != null && previousClaim != null) && (currentClaim.FactionId != previousClaim.FactionId))
      {
        // The player has crossed a border between two factions.
        SendMessage(player, Messages.EnteredClaimedArea, currentClaim.FactionId);
      }
    }

    void OnPlayerExitArea(Area area, BasePlayer player)
    {
      // TODO: If we don't need this hook, we should remove it.
    }

    void OnClanDestroy(string factionId)
    {
      var claims = Claims.GetAllClaimsForFaction(factionId);

      if (claims.Length > 0)
      {
        foreach (var claim in claims)
          PrintToChat("<color=#ff0000ff>AREA CLAIM LOST:</color> [{0}] has been disbanded, losing its claim on {1}!", claim.FactionId, claim.AreaId);
        Claims.Remove(claims);
      }

      if (Options.EnableTaxation)
        Taxes.Remove(factionId);
    }

    void OnTaxPoliciesChanged()
    {
      RefreshUiForAllPlayers();
    }

    void OnClaimsChanged()
    {
      GenerateMapOverlayImage();
      RefreshUiForAllPlayers();
    }

    void OnBadlandsChanged()
    {
      GenerateMapOverlayImage();
      RefreshUiForAllPlayers();
    }
    
    void CacheTaxChests()
    {
      if (!Options.EnableTaxation) return;

      // Find and cache references to the StorageContainers that act as tax chests.
      Puts("Caching references to tax chests...");

      foreach (var policy in Taxes.GetAllActiveTaxPolicies())
      {
        var containerId = (uint)policy.TaxChestId;
        var container = BaseNetworkable.serverEntities.Find(containerId) as StorageContainer;
        TaxChests[containerId] = container;
      }

      Puts($"Cached references to {TaxChests.Values.Count} tax chests.");
    }

  }

}
