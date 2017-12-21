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

    ClaimCollection Claims;
    TaxPolicyCollection TaxPolicies;
    BadlandsCollection Badlands;

    PlayerStateCollection<PlayerInteractionState> PlayerInteractionStates = new PlayerStateCollection<PlayerInteractionState>();
    PlayerStateCollection<PlayerMapState> PlayerMapStates = new PlayerStateCollection<PlayerMapState>();

    Dictionary<string, Area> Areas = new Dictionary<string, Area>();
    Dictionary<ulong, Area> PlayersInAreas = new Dictionary<ulong, Area>();
    Dictionary<uint, StorageContainer> TaxChests = new Dictionary<uint, StorageContainer>();

    const string PERM_CHANGE_CLAIMS = "rustfactions.claims";
    const string PERM_CHANGE_BADLANDS = "rustfactions.badlands";

    // Lifecycle Hooks -------------------------------------------------------------------------------------

    void Init()
    {
      PrintToChat($"{this.Title} {this.Version} initialized.");

      Claims = new ClaimCollection(this);
      TaxPolicies = new TaxPolicyCollection(this);
      Badlands = new BadlandsCollection(this);
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
      Puts($"Loaded {TaxPolicies.Count} area claims.");
      Puts($"Loaded {Badlands.Count} badlands areas.");

      GenerateMapOverlayImage();
      RebuildMapUi();
    }

    void Unload()
    {
      var objects = Resources.FindObjectsOfTypeAll<Area>();
      Puts($"Unloading {objects.Length} areas.");

      foreach (var area in objects)
      {
        var collider = area.GetComponent<BoxCollider>();
        if (collider != null)
          UnityEngine.Object.Destroy(collider);
        UnityEngine.Object.Destroy(area);
      }

      RemoveInfoPanelForAllPlayers();
    }

    void OnServerInitialized()
    {
      if (Clans == null)
        PrintWarning("RustFactions requires the Rust:IO Clans plugin, but it was not found!");

      CreateLandClaimAreas();
      CacheTaxChests();

      permission.RegisterPermission(PERM_CHANGE_BADLANDS, this);
      permission.RegisterPermission(PERM_CHANGE_CLAIMS, this);
    }

    void OnServerSave()
    {
      SaveData(DataFile);
    }

    // Game Event Hooks ------------------------------------------------------------------------------------

    void OnHammerHit(BasePlayer player, HitInfo hit)
    {
      PlayerInteractionState playerState = PlayerInteractionStates.Get(player);

      switch (playerState)
      {
        case PlayerInteractionState.AddingClaim:
          if (TryAddClaim(player, hit))
            PlayerInteractionStates.Reset(player);
          break;
        case PlayerInteractionState.RemovingClaim:
          if (TryRemoveClaim(player, hit))
            PlayerInteractionStates.Reset(player);
          break;
        case PlayerInteractionState.SelectingHeadquarters:
          if (TrySetHeadquarters(player, hit))
            PlayerInteractionStates.Reset(player);
          break;
        case PlayerInteractionState.SelectingTaxChest:
          if (TrySetTaxChest(player, hit))
            PlayerInteractionStates.Reset(player);
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
        Area area;
        if (PlayersInAreas.TryGetValue(player.userID, out area))
        {
          area.Players.Remove(player);
          PlayersInAreas.Remove(player.userID);
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
          var policy = TaxPolicies.Get(container);
          if (policy != null)
          {
            Puts($"[{policy.FactionId}] has lost their ability to tax because their tax chest was destroyed.");
            TaxPolicies.RemoveTaxChest(policy);
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
      Area previousArea;
      Claim previousClaim = null;
      if (PlayersInAreas.TryGetValue(player.userID, out previousArea))
        previousClaim = Claims.Get(previousArea);

      Claim claim = Claims.Get(area);

      PlayersInAreas[player.userID] = area;
      UpdateInfoPanel(player, area, claim);

      if (Badlands.Contains(area.Id) && (previousArea == null || !Badlands.Contains(previousArea)))
      {
        // The player has crossed into the badlands.
        SendMessage(player, Messages.EnteredBadlands);
      }
      else if (claim == null && previousClaim != null)
      {
        // The player has crossed a border between the land of a faction and unclaimed land.
        SendMessage(player, Messages.EnteredUnclaimedArea);
      }
      else if (claim != null && previousClaim == null)
      {
        // The player has crosed a border between unclaimed land and the land of a faction.
        SendMessage(player, Messages.EnteredClaimedArea, claim.FactionId);
      }
      else if ((claim != null && previousClaim != null) && (claim.FactionId != previousClaim.FactionId))
      {
        // The player has crossed a border between two factions.
        SendMessage(player, Messages.EnteredClaimedArea, claim.FactionId);
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
        TaxPolicies.Remove(factionId);
    }

    void OnTaxPoliciesChanged()
    {
      UpdateInfoPanelForAllPlayers();
    }

    void OnClaimsChanged()
    {
      GenerateMapOverlayImage();
      RebuildMapUi();
      UpdateInfoPanelForAllPlayers();
    }

    void OnBadlandsChanged()
    {
      GenerateMapOverlayImage();
      RebuildMapUi();
      UpdateInfoPanelForAllPlayers();
    }
    
    // Set Up Functions ------------------------------------------------------------------------------------

    /*
     * The (X, Z) coordinate system works like this (on a map of size 3000):
     *
     * (-3000, 3000) +-------+ (3000, 3000)
     *               |       |
     *               |   +--------- (0,0)
     *               |       |
     * (-3000, 3000) +-------+ (3000, -3000)
     *
     * No matter the map size, grid cells are always 150 x 150.
     */

    void CreateLandClaimAreas()
    {
      var worldSize = ConVar.Server.worldsize;
      const int step = 150;
      var offset = worldSize / 2;

      var prefix = "";
      char letter = 'A';

      Puts("Creating land claim areas...");

      for (var z = (offset - step); z > -(offset + step); z -= step)
      {
        var number = 0;
        for (var x = -offset; x < offset; x += step)
        {
          var area = new GameObject().AddComponent<Area>();

          var gridCell = $"{prefix}{letter}{number}";
          var location = new Vector3(x + (step / 2), 0, z + (step / 2));
          var size = new Vector3(step, 500, step); // TODO: Chose an arbitrary height. Is something else better?

          area.Setup(this, gridCell, location, size);

          Areas[gridCell] = area;

          number++;
        }

        if (letter == 'Z')
        {
          letter = 'A';
          prefix = "A";
        }
        else
        {
          letter++;
        }
      }

      Puts($"Created {Areas.Values.Count} land claim areas.");
    }

    void CacheTaxChests()
    {
      if (!Options.EnableTaxation) return;

      // Find and cache references to the StorageContainers that act as tax chests.
      Puts("Caching references to tax chests...");

      foreach (var policy in TaxPolicies.GetAllActiveTaxPolicies())
      {
        var containerId = (uint)policy.TaxChestId;
        var container = BaseNetworkable.serverEntities.Find(containerId) as StorageContainer;
        TaxChests[containerId] = container;
      }

      Puts($"Cached references to {TaxChests.Values.Count} tax chests.");
    }

  }

}
