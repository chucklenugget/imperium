// Reference: System.Drawing
// Requires: Clans

namespace Oxide.Plugins
{
  using Oxide.Core;
  using Oxide.Core.Configuration;
  using Oxide.Core.Plugins;
  using Rust;
  using System;
  using System.Collections.Generic;
  using System.Linq;
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

    Dictionary<string, string> MESSAGES = new Dictionary<string, string>
    {
      { "AreaClaimsDisabled", "Area claims are currently disabled." },
      { "TaxationDisabled", "Taxation is currently disabled." },
      { "BadlandsDisabled", "Badlands are currently disabled." },

      { "CannotClaimAreaNotMemberOfFaction", "You cannot claim an area without being a member of a faction!" },
      { "CannotClaimAreaFactionTooSmall", "You cannot claim an area because your faction does not have at least {0} members." },
      { "CannotClaimAreaNotFactionLeader", "You cannot change area claims because you aren't an owner or a moderator of your faction." },
      { "CannotClaimAreaBadlands", "You cannot claim this area because it is part of the badlands." },
      { "SelectClaimCupboardToAdd", "Use the hammer to select a tool cupboard to represent the claim. Say /claim again to cancel." },
      { "SelectClaimCupboardToRemove", "Use the hammer to select the tool cupboard representing the claim you want to remove. Say /claim again to cancel." },
      { "SelectClaimCupboardForHeadquarters", "Use the hammer to select the tool cupboard to represent your faction's headquarters. Say /claim again to cancel." },
      { "SelectingClaimCupboardCanceled", "Claim command canceled." },
      { "SelectingClaimCupboardFailedInvalidTarget", "You must select a tool cupboard to make a claim." },
      { "SelectingClaimCupboardFailedNeedAuth", "You must be authorized on the tool cupboard in order to use it to claim an area." },
      { "SelectingClaimCupboardFailedNotClaimCupboard", "That tool cabinet doesn't represent an area claim!" },

      { "ClaimCupboardMoved", "You have moved the claim on area {0} to a new tool cupboard." },
      { "HeadquartersSet", "You have declared {0} to be your faction's headquarters." },
      { "ClaimCaptured", "You have captured the area {0} from [{1}]!" },
      { "ClaimAdded", "You have claimed the area {0} for your faction." },
      { "ClaimRemove", "You have removed your faction's claim on {0}." },
      { "ClaimFailedAlreadyClaimed", "You cannot claim the area {0}, because it is already claimed by [{1}]!" },

      { "CannotShowClaimBadUsage", "Usage: /claim show XY" },
      { "CannotListClaimsBadUsage", "Usage: /claim list FACTION" },
      { "CannotListClaimsUnknownFaction", "Unknown faction [{0}]." },
      { "AreaIsBadlands", "{0} is part of the badlands and cannot be claimed." },
      { "AreaIsClaimed", "{0} is owned by [{1}]." },
      { "AreaIsHeadquarters", "{0} is the headquarters of [{1}]." },
      { "AreaIsUnclaimed", "{0} has not been claimed by a faction." },
      { "ClaimsList", "[{0}] has claimed: {1}" },

      { "CannotDeleteClaimsBadUsage", "Usage: /claims delete XY [XY XY...]" },
      { "CannotDeleteClaimsNoPermission", "You don't have permission to delete claims you don't own. Did you mean /claim remove?" },

      { "CannotSelectTaxChestNotMemberOfFaction", "You cannot select a tax chest without being a member of a faction!" },
      { "CannotSelectTaxChestNotFactionLeader", "You cannot select a tax chest because you aren't an owner or a moderator of your faction." },
      { "SelectTaxChest", "Use the hammer to select the container to receive your faction's tribute. Say /taxchest again to cancel." },
      { "SelectingTaxChestCanceled", "Tax chest selection canceled." },
      { "SelectingTaxChestFailedInvalidTarget", "That can't be used as a tax chest." },
      { "SelectingTaxChestSucceeded", "You have selected a new tax chest that will receive {0}% of the materials harvested within land owned by [{1}]. To change the tax rate, say /taxrate PERCENT."},

      { "CannotSetTaxRateNotMemberOfFaction", "You cannot set a tax rate without being a member of a faction!" },
      { "CannotSetTaxRateNotFactionLeader", "You cannot set a tax rate because you aren't an owner or a moderator of your faction." },
      { "CannotSetTaxRateInvalidValue", "You must specify a valid percentage between 0-{0}% as a tax rate." },
      { "SetTaxRateSuccessful", "You have set the tax rate on the land holdings of [{0}] to {1}%." },

      { "CannotSetBadlandsNoPermission", "You don't have permission to alter badlands." },
      { "CannotSetBadlandsWrongUsage", "Usage: /badlands <add|remove|set|clear> [XY XY XY...]" },
      { "CannotSetBadlandsAreaClaimed", "Cannot set {0} as badlands, since it has already been claimed by [{1}]." },
      { "BadlandsAdded", "Added {0} to badlands. Badlands areas are now: {1}" },
      { "BadlandsRemoved", "Removed {0} from badlands. Badlands areas are now: {1}" },
      { "BadlandsSet", "Badlands areas are now: {0}" },
      { "BadlandsList", "Badlands areas are: {0}. Gather bonus is {1}%." },

      { "EnteredBadlands", "<color=#ff0000>BORDER:</color> You have entered the badlands! Player violence is allowed here." },
      { "EnteredUnclaimedArea", "<color=#ffd479>BORDER:</color> You have entered unclaimed land." },
      { "EnteredClaimedArea", "<color=#ffd479>BORDER:</color> You have entered land claimed by [{0}]." }
    };

    // Lifecycle Hooks -------------------------------------------------------------------------------------

    void Init()
    {
      Announce($"{this.Title} {this.Version} initialized.");

      Claims = new ClaimCollection(this);
      TaxPolicies = new TaxPolicyCollection(this);
      Badlands = new BadlandsCollection(this);
    }

    void Loaded()
    {
      lang.RegisterMessages(MESSAGES, this);
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
          Announce("<color=#ff0000ff>AREA CLAIM LOST:</color> [{0}] has lost its claim on {1}, because the tool cupboard was destroyed!", claim.FactionId, claim.AreaId);
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
        SendMessage(player, "EnteredBadlands");
      }
      else if (claim == null && previousClaim != null)
      {
        // The player has crossed a border between the land of a faction and unclaimed land.
        SendMessage(player, "EnteredUnclaimedArea");
      }
      else if (claim != null && previousClaim == null)
      {
        // The player has crosed a border between unclaimed land and the land of a faction.
        SendMessage(player, "EnteredClaimedArea", claim.FactionId);
      }
      else if ((claim != null && previousClaim != null) && (claim.FactionId != previousClaim.FactionId))
      {
        // The player has crossed a border between two factions.
        SendMessage(player, "EnteredClaimedArea", claim.FactionId);
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
          Announce("<color=#ff0000ff>AREA CLAIM LOST:</color> [{0}] has been disbanded, losing its claim on {1}!", claim.FactionId, claim.AreaId);
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
