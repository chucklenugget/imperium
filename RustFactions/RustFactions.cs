// Reference: System.Drawing
// Requires: Clans

using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UnityEngine;

using Color = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;

namespace Oxide.Plugins
{
  [Info("RustFactions", "chucklenugget", "0.6.0")]
  public class RustFactions : RustPlugin
  {

    // Definitions -----------------------------------------------------------------------------------------

    [PluginReference] Plugin Clans;

    DynamicConfigFile DataFile;
    CuiElementContainer MapUi;
    uint CurrentMapOverlayImageId;

    public Dictionary<string, Claim> Claims = new Dictionary<string, Claim>();
    public Dictionary<string, TaxPolicy> TaxPolicies = new Dictionary<string, TaxPolicy>();
    public HashSet<string> BadlandsAreas = new HashSet<string>();

    PlayerStateTracker<PlayerInteractionState> PlayerInteractionStates = new PlayerStateTracker<PlayerInteractionState>();
    PlayerStateTracker<PlayerMapState> PlayerMapStates = new PlayerStateTracker<PlayerMapState>();

    Dictionary<string, Area> Areas = new Dictionary<string, Area>();
    Dictionary<ulong, Area> PlayersInAreas = new Dictionary<ulong, Area>();
    Dictionary<uint, StorageContainer> TaxChests = new Dictionary<uint, StorageContainer>();

    const string UI_CLAIM_PANEL = "RustFactionsClaimPanel";
    const string UI_CLAIM_PANEL_TEXT = "RustFactionsClaimPanelText";
    const string UI_CLAIM_PANEL_BGCOLOR_NORMAL = "1 0.95 0.875 0.025";
    const string UI_CLAIM_PANEL_BGCOLOR_RED = "0.77 0.25 0.17 1";
    const string UI_MAP_PANEL = "RustFactionsMapPanel";
    const string UI_MAP_CLOSE_BUTTON = "RustFactionsMapCloseButton";
    const string UI_MAP_BACKGROUND_IMAGE = "RustFactionsMapImage";
    const string UI_MAP_OVERLAY_IMAGE = "RustFactionsMapOverlay";
    const string UI_TRANSPARENT_TEXTURE = "assets/content/textures/generic/fulltransparent.tga";

    const string PERM_CHANGE_CLAIMS = "rustfactions.claims";
    const string PERM_CHANGE_BADLANDS = "rustfactions.badlands";

    string MapImageUrl;
    int MapImageSize;
    int MinFactionMembers;
    int DefaultTaxRate;
    int MaxTaxRate;
    int BadlandsGatherBonus;
    bool EnableAreaClaims;
    bool EnableTaxation;
    bool EnableBadlands;

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
    }

    void Loaded()
    {
      DataFile = Interface.Oxide.DataFileSystem.GetFile("RustFactions");
      lang.RegisterMessages(MESSAGES, this);

      MapImageUrl = Convert.ToString(Config["MapImageUrl"]);
      MapImageSize = Convert.ToInt32(Config["MapImageSize"]);
      MinFactionMembers = Convert.ToInt32(Config["MinFactionMembers"]);
      DefaultTaxRate = Convert.ToInt32(Config["DefaultTaxRate"]);
      MaxTaxRate = Convert.ToInt32(Config["MaxTaxRate"]);
      BadlandsGatherBonus = Convert.ToInt32(Config["BadlandsGatherBonus"]);
      EnableAreaClaims = Convert.ToBoolean(Config["EnableAreaClaims"]);
      EnableTaxation = Convert.ToBoolean(Config["EnableTaxation"]);
      EnableBadlands = Convert.ToBoolean(Config["EnableBadlands"]);

      Puts("Area Claims are " + (EnableAreaClaims ? "enabled" : "disabled"));
      Puts("Taxation is " + (EnableTaxation ? "enabled" : "disabled"));
      Puts("Badlands are " + (EnableBadlands ? "enabled" : "disabled"));

      try
      {
        var data = DataFile.ReadObject<PersistentData>();
        if (data.Claims != null)
        {
          Claims = data.Claims.ToDictionary(claim => claim.AreaId);
          Puts($"Loaded {Claims.Values.Count} area claims.");
        }
        if (data.TaxPolicies != null)
        {
          TaxPolicies = data.TaxPolicies.ToDictionary(policy => policy.FactionId);
          Puts($"Loaded {TaxPolicies.Values.Count} area claims.");
        }
        if (data.BadlandsAreas != null)
        {
          BadlandsAreas = new HashSet<string>(data.BadlandsAreas);
          Puts($"Loaded {BadlandsAreas.Count} badlands areas.");
        }
      }
      catch (Exception err)
      {
        Puts(err.ToString());
        PrintWarning("Couldn't load claim and tax policies, defaulting to an empty map.");
      }

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
      var data = new PersistentData {
        Claims = Claims.Values.ToArray(),
        TaxPolicies = TaxPolicies.Values.ToArray(),
        BadlandsAreas = BadlandsAreas.ToArray()
      };

      DataFile.WriteObject(data, true);
    }

    protected override void LoadDefaultConfig()
    {
      PrintWarning("Loading default configuration.");
      Config.Clear();
      Config["MapImageUrl"] = "";
      Config["MapImageSize"] = 1024;
      Config["MinFactionMembers"] = 3;
      Config["DefaultTaxRate"] = 10;
      Config["MaxTaxRate"] = 20;
      Config["BadlandsGatherBonus"] = 10;
      Config["EnableAreaClaims"] = true;
      Config["EnableTaxation"] = true;
      Config["EnableBadlands"] = true;
      SaveConfig();
    }

    // Claim Commands --------------------------------------------------------------------------------------

    [ChatCommand("claim")]
    void OnClaimCommand(BasePlayer player, string command, string[] args)
    {
      if (!EnableAreaClaims)
      {
        SendMessage(player, "AreaClaimsDisabled");
        return;
      };

      if (args.Length == 0)
      {
        PlayerInteractionState playerState = PlayerInteractionStates.Get(player);

        if (playerState == PlayerInteractionState.AddingClaim || playerState == PlayerInteractionState.RemovingClaim)
        {
          SendMessage(player, "SelectingClaimCupboardCanceled");
          PlayerInteractionStates.Reset(player);
        }
        else
        {
          OnClaimAddCommand(player);
        }

        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          OnClaimAddCommand(player);
          break;
        case "remove":
          OnClaimRemoveCommand(player);
          break;
        case "hq":
          OnClaimHeadquartersCommand(player);
          break;
        case "show":
          OnClaimShowCommand(player, restArguments);
          break;
        case "list":
          OnClaimListCommand(player, restArguments);
          break;
        case "delete":
          OnClaimDeleteCommand(player, restArguments);
          break;
        case "help":
        default:
          OnClaimHelpCommand(player);
          break;
      }

    }

    void OnClaimAddCommand(BasePlayer player)
    {
      if (CanChangeFactionClaims(player))
      {
        SendMessage(player, "SelectClaimCupboardToAdd");
        PlayerInteractionStates.Set(player, PlayerInteractionState.AddingClaim);
      }
    }

    void OnClaimRemoveCommand(BasePlayer player)
    {
      if (CanChangeFactionClaims(player))
      {
        SendMessage(player, "SelectClaimCupboardToRemove");
        PlayerInteractionStates.Set(player, PlayerInteractionState.RemovingClaim);
      }
    }

    void OnClaimHeadquartersCommand(BasePlayer player)
    {
      if (CanChangeFactionClaims(player))
      {
        SendMessage(player, "SelectClaimCupboardForHeadquarters");
        PlayerInteractionStates.Set(player, PlayerInteractionState.SelectingHeadquarters);
      }
    }

    void OnClaimShowCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 1)
      {
        SendMessage(player, "CannotShowClaimBadUsage");
        return;
      }

      string areaId = NormalizeAreaId(args[0]);

      if (BadlandsAreas.Contains(areaId))
      {
        SendMessage(player, "AreaIsBadlands", areaId);
        return;
      }

      Claim claim;
      if (!Claims.TryGetValue(areaId, out claim))
        SendMessage(player, "AreaIsUnclaimed", areaId);
      else if (claim.IsHeadquarters)
        SendMessage(player, "AreaIsHeadquarters", claim.AreaId, claim.FactionId);
      else
        SendMessage(player, "AreaIsClaimed", claim.AreaId, claim.FactionId);
    }

    void OnClaimListCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 1)
      {
        SendMessage(player, "CannotListClaimsBadUsage");
        return;
      }

      string factionId = NormalizeFactionId(args[0]);
      Faction faction = GetFaction(factionId);

      if (faction == null)
      {
        SendMessage(player, "CannotListClaimsUnknownFaction", factionId);
        return;
      }

      Claim[] claims = GetClaimsForFaction(faction.Id);
      Claim headquarters = claims.FirstOrDefault(c => c.IsHeadquarters);

      var sb = new StringBuilder();

      if (claims.Length == 0)
      {
        sb.AppendFormat(String.Format("<color=#ffd479>[{0}]</color> has no land holdings.", factionId));
      }
      else
      {
        float percentageOfMap = (claims.Length / (float)Areas.Values.Count) * 100;
        sb.AppendLine(String.Format("<color=#ffd479>[{0}] owns {1} tiles ({2:F2}% of the known world)</color>", faction.Id, claims.Length, percentageOfMap));
        sb.AppendLine(String.Format("Headquarters: {0}", (headquarters == null) ? "Unknown" : headquarters.AreaId));
        sb.AppendLine(String.Format("Areas claimed: {0}", FormatList(claims.Select(c => c.AreaId))));
      }

      SendMessage(player, sb);
    }

    void OnClaimHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/claim</color>: Add a claim for your faction");
      sb.AppendLine("  <color=#ffd479>/claim hq</color>: Select your faction's headquarters");
      sb.AppendLine("  <color=#ffd479>/claim remove</color>: Remove a claim for your faction (no undo!)");
      sb.AppendLine("  <color=#ffd479>/claim show XY</color>: See which faction owns the specified area");
      sb.AppendLine("  <color=#ffd479>/claim list FACTION</color>: List all areas claimed for a faction");

      if (permission.UserHasPermission(player.UserIDString, PERM_CHANGE_CLAIMS))
      {
        sb.AppendLine("Admin commands:");
        sb.AppendLine("  <color=#ffd479>/claim delete XY [XY XY XY...]</color>: Remove the claim on the specified areas (no undo!)");
      }

      SendMessage(player, sb);
    }

    void OnClaimDeleteCommand(BasePlayer player, string[] args)
    {
      if (args.Length == 0)
      {
        SendMessage(player, "CannotDeleteClaimsBadUsage");
        return;
      }

      if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_CLAIMS))
      {
        SendMessage(player, "CannotDeleteClaimsNoPermission");
        return;
      }

      var claimsToRevoke = new List<Claim>();
      foreach (string arg in args)
      {
        string areaId = NormalizeAreaId(arg);

        Claim claim;
        if (!Claims.TryGetValue(areaId, out claim))
        {
          SendMessage(player, "CannotDeleteClaimsAreaNotClaimed", areaId);
          return;
        }

        claimsToRevoke.Add(claim);
      }

      foreach (Claim claim in claimsToRevoke)
      {
        Announce("<color=#ff0000ff>AREA CLAIM REMOVED:</color> [{0}]'s claim on {1} has been removed by an admin.", claim.FactionId, claim.AreaId);
        Puts($"{player.displayName} deleted [{claim.FactionId}]'s claim on {claim.AreaId}");
        Claims.Remove(claim.AreaId);
      }

      OnClaimsChanged();
    }

    // Tax Commands ----------------------------------------------------------------------------------------

    [ChatCommand("taxchest")]
    void OnTaxChestCommand(BasePlayer player, string command, string[] args)
    {
      if (!EnableTaxation)
      {
        SendMessage(player, "TaxationDisabled");
        return;
      };

      PlayerInteractionState playerState = PlayerInteractionStates.Get(player);
      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        SendMessage(player, "CannotSelectTaxChestNotMemberOfFaction");
        return;
      }

      if (!faction.IsLeader(player))
      {
        SendMessage(player, "CannotSelectTaxChestNotFactionLeader");
        return;
      }

      if (playerState == PlayerInteractionState.SelectingTaxChest)
      {
        SendMessage(player, "SelectingTaxChestCanceled");
        PlayerInteractionStates.Reset(player);
      }
      else
      {
        SendMessage(player, "SelectTaxChest");
        PlayerInteractionStates.Set(player, PlayerInteractionState.SelectingTaxChest);
      }
    }

    [ChatCommand("taxrate")]
    void OnTaxRateCommand(BasePlayer player, string command, string[] args)
    {
      if (!EnableTaxation)
      {
        SendMessage(player, "TaxationDisabled");
        return;
      };

      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        SendMessage(player, "CannotSetTaxRateNotMemberOfFaction");
        return;
      }

      if (!faction.IsLeader(player))
      {
        SendMessage(player, "CannotSetTaxRateNotFactionLeader");
        return;
      }

      int taxRate;
      try
      {
        taxRate = Convert.ToInt32(args[0]);
      }
      catch
      {
        SendMessage(player, "CannotSetTaxRateInvalidValue", MaxTaxRate);
        return;
      }

      if (taxRate < 0 || taxRate > MaxTaxRate)
      {
        SendMessage(player, "CannotSetTaxRateInvalidValue", MaxTaxRate);
        return;
      }

      TaxPolicy policy;
      if (TaxPolicies.TryGetValue(faction.Id, out policy))
        policy.TaxRate = taxRate;
      else
        policy = new TaxPolicy(faction.Id, taxRate, null);

      SendMessage(player, "SetTaxRateSuccessful", faction.Id, taxRate);
      OnTaxPoliciesChanged();
    }

    // Badlands Commands -----------------------------------------------------------------------------------

    [ChatCommand("badlands")]
    void OnBadlandsCommand(BasePlayer player, string command, string[] args)
    {
      if (!EnableBadlands)
      {
        SendMessage(player, "BadlandsDisabled");
        return;
      }

      if (args.Length == 0)
      {
        SendMessage(player, "BadlandsList", FormatList(BadlandsAreas), BadlandsGatherBonus);
        return;
      }

      var areaIds = args.Skip(1).Select(arg => arg.ToUpper()).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
            SendMessage(player, "CannotSetBadlandsNoPermission");      
          else if (args.Length < 2)
            SendMessage(player, "CannotSetBadlandsWrongUsage");
          else
            AddBadlands(player, areaIds);
          break;
        case "remove":
          if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
            SendMessage(player, "CannotSetBadlandsNoPermission");
          else if (args.Length < 2)
            SendMessage(player, "CannotSetBadlandsWrongUsage");
          else
            RemoveBadlands(player, areaIds);
          break;
        case "set":
          if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
            SendMessage(player, "CannotSetBadlandsNoPermission");
          else if (args.Length < 2)
            SendMessage(player, "CannotSetBadlandsWrongUsage");
          else
            SetBadlands(player, areaIds);
          break;
        case "clear":
          if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
            SendMessage(player, "CannotSetBadlandsNoPermission");
          else if (args.Length != 1)
            SendMessage(player, "CannotSetBadlandsWrongUsage");
          else
            SetBadlands(player, new string[0]);
          break;
        default:
          SendMessage(player, "CannotSetBadlandsWrongUsage");
          break;
      }
    }

    // Map Commands ----------------------------------------------------------------------------------------

    [ChatCommand("map")]
    void OnMapCommand(BasePlayer player, string command, string[] args)
    {
      ToggleMap(player);
    }

    [ConsoleCommand("rustfactions.map.toggle")]
    void OnMapConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;
      ToggleMap(player);
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
        var claim = Claims.Values.FirstOrDefault(c => c.CupboardId == cupboard.net.ID);
        if (claim != null)
        {
          Announce("<color=#ff0000ff>AREA CLAIM LOST:</color> [{0}] has lost its claim on {1}, because the tool cupboard was destroyed!", claim.FactionId, claim.AreaId);
          Claims.Remove(claim.AreaId);
          OnClaimsChanged();
        }
      }

      // If a tax container is destroyed, remove it from the tax policy.
      if (EnableTaxation)
      {
        var container = entity as StorageContainer;
        if (container != null)
        {
          var policy = TaxPolicies.Values.FirstOrDefault(p => p.TaxChestId == container.net.ID);
          if (policy != null)
          {
            Puts($"[{policy.FactionId}] has lost their ability to tax because their tax chest was destroyed.");
            policy.TaxChestId = null;
            TaxChests.Remove(entity.net.ID);
            OnTaxPoliciesChanged();
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
        Claims.TryGetValue(previousArea.Id, out previousClaim);

      Claim claim = null;
      Claims.TryGetValue(area.Id, out claim);

      PlayersInAreas[player.userID] = area;
      UpdateInfoPanel(player, area, claim);

      if (BadlandsAreas.Contains(area.Id))
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
      var claims = Claims.Values.Where(c => c.FactionId == factionId).ToArray();

      if (claims.Length > 0)
      {
        foreach (var claim in claims)
        {
          Announce("<color=#ff0000ff>AREA CLAIM LOST:</color> [{0}] has been disbanded, losing its claim on {1}!", claim.FactionId, claim.AreaId);
          Claims.Remove(claim.AreaId);
        }
        OnClaimsChanged();
      }

      if (EnableTaxation)
      {
        TaxPolicies.Remove(factionId);
        OnTaxPoliciesChanged();
      }
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

    // Interaction -----------------------------------------------------------------------------------------

    bool TryAddClaim(BasePlayer player, HitInfo hit)
    {
      Faction faction = GetFactionForPlayer(player);
      var cupboard = hit.HitEntity as BuildingPrivlidge;

      if (!CanChangeFactionClaims(player) || !CanUseCupboardAsClaim(player, cupboard))
        return false;

      Area area;
      if (!PlayersInAreas.TryGetValue(player.userID, out area))
      {
        PrintWarning("Player attempted to add claim but wasn't in an area. This shouldn't happen.");
        return false;
      }

      if (BadlandsAreas.Contains(area.Id))
      {
        SendMessage(player, "CannotClaimAreaBadlands");
        return false;
      }

      bool isHeadquarters = GetClaimsForFaction(faction.Id).Length == 0;

      Claim oldClaim;
      Claim newClaim = new Claim(area.Id, faction.Id, player.userID, cupboard.net.ID, isHeadquarters);

      if (Claims.TryGetValue(area.Id, out oldClaim))
      {
        if (oldClaim.FactionId == newClaim.FactionId)
        {
          // If the same faction claims a new cabinet within the same area, move the claim to the new cabinet.
          SendMessage(player, "ClaimCupboardMoved", area.Id);
        }
        else if (oldClaim.CupboardId == newClaim.CupboardId)
        {
          // If a new faction claims the claim cabinet for an area, they take control of that area.
          SendMessage(player, "ClaimCaptured", area.Id, oldClaim.FactionId);
          Announce("<color=#ff0000ff>AREA CAPTURED:</color> [{0}] has captured {1} from [{2}]!", faction.Id, area.Id, oldClaim.FactionId);
        }
        else
        {
          // A new faction can't make a claim on a new cabinet within an area that is already claimed by another faction.
          SendMessage(player, "ClaimFailedAlreadyClaimed", area.Id, oldClaim.FactionId);
          return false;
        }
      }
      else
      {
        SendMessage(player, "ClaimAdded", area.Id);
        if (isHeadquarters)
          Announce("<color=#00ff00ff>AREA CLAIMED:</color> [{0}] claims {1} as their headquarters!", faction.Id, area.Id);
        else
          Announce("<color=#00ff00ff>AREA CLAIMED:</color> [{0}] claims {1}!", faction.Id, area.Id);
      }

      Claims[newClaim.AreaId] = newClaim;
      OnClaimsChanged();

      Puts($"{newClaim.FactionId} claims {newClaim.AreaId}");

      return true;
    }

    bool TryRemoveClaim(BasePlayer player, HitInfo hit)
    {
      Faction faction = GetFactionForPlayer(player);
      var cupboard = hit.HitEntity as BuildingPrivlidge;

      if (!CanChangeFactionClaims(player) || !CanUseCupboardAsClaim(player, cupboard))
        return false;

      Claim claim = Claims.Values.First(c => c.CupboardId == cupboard.net.ID);
      if (claim == null)
      {
        SendMessage(player, "SelectingClaimCupboardFailedNotClaimCupboard");
        return false;
      }

      SendMessage(player, "ClaimRemoved", claim.AreaId);
      Announce("<color=#ff0000ff>CLAIM REMOVED:</color> [{0}] has relinquished their claim on {1}!", faction.Id, claim.AreaId);

      Claims.Remove(claim.AreaId);
      OnClaimsChanged();

      return true;
    }

    bool TrySetHeadquarters(BasePlayer player, HitInfo hit)
    {
      Faction faction = GetFactionForPlayer(player);
      var cupboard = hit.HitEntity as BuildingPrivlidge;

      if (!CanChangeFactionClaims(player) || !CanUseCupboardAsClaim(player, cupboard))
        return false;

      Claim hqClaim = Claims.Values.FirstOrDefault(c => c.CupboardId == cupboard.net.ID);
      if (hqClaim == null)
      {
        SendMessage(player, "SelectingClaimCupboardFailedNotClaimCupboard");
        return false;
      }

      SendMessage(player, "HeadquartersSet", hqClaim.AreaId);
      Announce("<color=#00ff00ff>HQ CHANGED:</color> [{0}] announces that {1} is their headquarters.", faction.Id, hqClaim.AreaId);

      foreach (var claim in Claims.Values.Where(c => c.FactionId == faction.Id))
        claim.IsHeadquarters = false;

      hqClaim.IsHeadquarters = true;
      OnClaimsChanged();

      Puts($"{faction.Id} designates {hqClaim.AreaId} as their headquarters");

      return true;
    }

    bool TrySetTaxChest(BasePlayer player, HitInfo hit)
    {
      var container = hit.HitEntity as StorageContainer;

      if (container == null)
      {
        SendMessage(player, "SelectingTaxChestFailedInvalidTarget");
        return false;
      }

      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        // This covers the unlikely case the player is removed from the faction before they finish selecting.
        SendMessage(player, "CannotSelectTaxChestNotMemberOfFaction");
        return false;
      }

      TaxPolicy policy;
      if (TaxPolicies.TryGetValue(faction.Id, out policy))
        policy.TaxChestId = container.net.ID;
      else
        policy = new TaxPolicy(faction.Id, DefaultTaxRate, container.net.ID);

      SendMessage(player, "SelectingTaxChestSucceeded", policy.TaxRate, policy.FactionId);

      TaxPolicies[faction.Id] = policy;
      TaxChests[container.net.ID] = container;

      Puts($"Tax chest for {faction.Id} set to {container.net.ID}");

      return true;
    }

    void AddBadlands(BasePlayer player, string[] areaIds)
    {
      foreach (string areaId in areaIds)
      {
        Claim claim;
        if (Claims.TryGetValue(areaId, out claim))
        {
          SendMessage(player, "CannotSetBadlandsAreaClaimed", claim.AreaId, claim.FactionId);
          return;
        }
      }

      BadlandsAreas = new HashSet<string>(BadlandsAreas.Union(areaIds));
      SendMessage(player, "BadlandsAdded", FormatList(areaIds), FormatList(BadlandsAreas));
      OnBadlandsChanged();
    }

    void RemoveBadlands(BasePlayer player, string[] areaIds)
    {
      BadlandsAreas = new HashSet<string>(BadlandsAreas.Except(areaIds));
      SendMessage(player, "BadlandsRemoved", FormatList(areaIds), FormatList(BadlandsAreas));
      OnBadlandsChanged();      
    }

    void SetBadlands(BasePlayer player, string[] areaIds)
    {
      BadlandsAreas = new HashSet<string>(areaIds);
      SendMessage(player, "BadlandsSet", FormatList(BadlandsAreas));
      OnBadlandsChanged();
    }

    // Dispenser Math -------------------------------------------------------------------------------------

    void ChargeTaxIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!EnableTaxation) return;

      var player = entity as BasePlayer;
      if (player == null) return;

      Area area;
      if (!PlayersInAreas.TryGetValue(player.userID, out area))
      {
        PrintWarning("Player gathered outside of a defined area. This shouldn't happen.");
        return;
      }

      Claim claim;
      if (!Claims.TryGetValue(area.Id, out claim))
        return;

      TaxPolicy policy;
      if (TaxPolicies.TryGetValue(claim.FactionId, out policy))
      {
        if (policy.TaxChestId != null && policy.TaxRate > 0)
        {
          StorageContainer container;
          if (TaxChests.TryGetValue((uint)policy.TaxChestId, out container) && !container.inventory.IsFull())
          {
            ItemDefinition itemDef = ItemManager.FindItemDefinition(item.info.itemid);
            if (itemDef != null)
            {
              var tax = (int)( item.amount * (policy.TaxRate / 100f) );
              item.amount -= tax;
              container.inventory.AddItem(itemDef, tax);
            }
          }
        }
      }
    }

    void AwardBadlandsBonusIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!EnableBadlands) return;

      var player = entity as BasePlayer;
      if (player == null) return;

      Area area;
      if (!PlayersInAreas.TryGetValue(player.userID, out area))
      {
        PrintWarning("Player gathered outside of a defined area. This shouldn't happen.");
        return;
      }

      if (BadlandsAreas.Contains(area.Id))
      {
        var bonus = (int)( item.amount * (BadlandsGatherBonus / 100f) );
        item.amount += bonus;
      }
    }

    // Auth Checks ----------------------------------------------------------------------------------------

    bool CanChangeFactionClaims(BasePlayer player)
    {
      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        SendMessage(player, "CannotClaimAreaNotMemberOfFaction");
        return false;
      }

      if (faction.MemberSteamIds.Count < MinFactionMembers)
      {
        SendMessage(player, "CannotClaimAreaFactionTooSmall", MinFactionMembers);
        return false;
      }

      if (!faction.IsLeader(player))
      {
        SendMessage(player, "CannotClaimAreaNotFactionLeader");
        return false;
      }

      return true;
    }

    bool CanUseCupboardAsClaim(BasePlayer player, BuildingPrivlidge cupboard)
    {
      if (cupboard == null)
      {
        SendMessage(player, "SelectingClaimCupboardFailedInvalidTarget");
        return false;
      }

      if (!cupboard.IsAuthed(player))
      {
        SendMessage(player, "SelectingClaimCupboardFailedNeedAuth");
        return false;
      }

      return true;
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
      if (!EnableTaxation) return;

      // Find and cache references to the StorageContainers that act as tax chests.
      Puts("Caching references to tax chests...");

      foreach (var policy in TaxPolicies.Values.Where(p => p.TaxChestId != null))
      {
        var containerId = (uint)policy.TaxChestId;
        var container = BaseNetworkable.serverEntities.Find(containerId) as StorageContainer;
        TaxChests[containerId] = container;
      }

      Puts($"Cached references to {TaxChests.Values.Count} tax chests.");
    }

    string FormatList(IEnumerable<string> items)
    {
      return String.Join(", ", items.ToArray());
    }

    // Utility Functions -----------------------------------------------------------------------------------

    void SendMessage(BasePlayer player, string messageName)
    {
      SendReply(player, lang.GetMessage(messageName, this, player.UserIDString));
    }

    void SendMessage(BasePlayer player, string messageName, params object[] args)
    {
      SendReply(player, String.Format(lang.GetMessage(messageName, this, player.UserIDString), args));
    }

    void SendMessage(BasePlayer player, StringBuilder sb)
    {
      SendReply(player, sb.ToString().TrimEnd());
    }

    void Announce(string message, params object[] args)
    {
      // TODO: Localize text
      PrintToChat(String.Format(message, args));
    }

    Faction GetFactionForPlayer(BasePlayer player)
    {
      var name = Clans?.Call<string>("GetClanOf", player.userID);

      if (String.IsNullOrEmpty(name))
        return null;

      return GetFaction(name);
    }

    Faction GetFaction(string name)
    {
      var clanData = Clans?.Call<JObject>("GetClan", name);
      if (clanData == null)
        return null;
      else
        return new Faction(clanData);
    }

    Claim[] GetClaimsForFaction(string factionId)
    {
      return Claims.Values.Where(claim => claim.FactionId == factionId).ToArray();
    }

    string NormalizeAreaId(string input)
    {
      return input.ToUpper().Trim();
    }

    string NormalizeFactionId(string input)
    {
      string factionId = input.ToUpper().Trim();

      if (factionId.StartsWith("[") && factionId.EndsWith("]"))
        factionId = factionId.Substring(1, factionId.Length - 2);
        
      return factionId;
    }
    
    // ----------------------------------------------------------------------------------------------------

    void UpdateInfoPanel(BasePlayer player, Area area, Claim claim)
    {
      RemoveInfoPanel(player);

      string text;
      string backgroundColor = UI_CLAIM_PANEL_BGCOLOR_NORMAL;

      if (BadlandsAreas.Contains(area.Id))
      {
        text = $"{area.Id}: Badlands (+{BadlandsGatherBonus}% bonus)";
        backgroundColor = UI_CLAIM_PANEL_BGCOLOR_RED;
      }
      else if (claim == null)
      {
        text = $"{area.Id}: Unclaimed";
      }
      else
      {
        Faction faction = GetFaction(claim.FactionId);
        TaxPolicy policy;
        if (TaxPolicies.TryGetValue(claim.FactionId, out policy))
          text = $"{area.Id}: {faction.Id} ({policy.TaxRate}% tax)";
        else
          text = $"{area.Id}: {faction.Id}";
      }

      var label = new CuiLabel {
        Text = {
          Align = TextAnchor.MiddleLeft,
          Color = "0.85 0.85 0.85 0.5",
          FontSize = 14,
          Text = text
        },
        RectTransform = {
          AnchorMin = "0 0",
          AnchorMax = "1 1",
          OffsetMin = "6 0",
          OffsetMax = "6 0"
        }
      };

      var panel = new CuiPanel {
        Image = { Color = backgroundColor },
        RectTransform = {
          AnchorMin = "0 0",
          AnchorMax = "0.124 0.036",
          OffsetMin = "16 44",
          OffsetMax = "16 44"
        }
      };

      var container = new CuiElementContainer();
      container.Add(panel, "Hud", UI_CLAIM_PANEL);
      container.Add(label, UI_CLAIM_PANEL, UI_CLAIM_PANEL_TEXT);

      CuiHelper.AddUi(player, container);
    }

    void UpdateInfoPanelForAllPlayers()
    {
      foreach (var player in BasePlayer.activePlayerList)
      {
        Area area;
        if (!PlayersInAreas.TryGetValue(player.userID, out area))
        {
          Puts($"Couldn't update info panel for player because they were outside of a known area. This shouldn't happen.");
          continue;
        }
        Claim claim;
        Claims.TryGetValue(area.Id, out claim);
        UpdateInfoPanel(player, area, claim);
      }
    }

    void RemoveInfoPanel(BasePlayer player)
    {
      CuiHelper.DestroyUi(player, UI_CLAIM_PANEL);
    }

    void RemoveInfoPanelForAllPlayers()
    {
      foreach (var player in BasePlayer.activePlayerList)
        RemoveInfoPanel(player);
    }

    // ----------------------------------------------------------------------------------------------------

    void ShowMapPanel(BasePlayer player)
    {
      CuiHelper.AddUi(player, MapUi);
    }

    void RemoveMapPanel(BasePlayer player)
    {
      CuiHelper.DestroyUi(player, UI_MAP_PANEL);
    }

    void RemoveMapPanelForAllPlayers()
    {
      foreach (var player in BasePlayer.activePlayerList)
        RemoveInfoPanel(player);
    }

    void ToggleMap(BasePlayer player)
    {
      PlayerMapState mapState = PlayerMapStates.Get(player);

      if (mapState == PlayerMapState.Hidden)
      {
        ShowMapPanel(player);
        PlayerMapStates.Set(player, PlayerMapState.Visible);
      }
      else
      {
        RemoveMapPanel(player);
        PlayerMapStates.Set(player, PlayerMapState.Hidden);
      }
    }

    void RebuildMapUi()
    {
      var panel = new CuiPanel {
        Image = { Color = "0 0 0 1" },
        RectTransform = { AnchorMin = "0.2271875 0.015", AnchorMax = "0.7728125 0.985" },
        CursorEnabled = true
      };

      var container = new CuiElementContainer();
      container.Add(panel, "Hud", UI_MAP_PANEL);

      container.Add(new CuiElement {
        Name = UI_MAP_BACKGROUND_IMAGE,
        Parent = UI_MAP_PANEL,
        Components = {
          new CuiRawImageComponent { Url = MapImageUrl, Sprite = UI_TRANSPARENT_TEXTURE },
          new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
        }
      });

      container.Add(new CuiElement {
        Name = UI_MAP_OVERLAY_IMAGE,
        Parent = UI_MAP_PANEL,
        Components = {
          new CuiRawImageComponent { Png = CurrentMapOverlayImageId.ToString(), Sprite = UI_TRANSPARENT_TEXTURE },
          new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
        }
      });

      container.Add(new CuiButton {
        Button = { Color = "0 0 0 1", Command = "rustfactions.map.toggle", FadeIn = 0 },
        RectTransform = { AnchorMin = "0.95 0.961", AnchorMax = "0.999 0.999" },
        Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter }
      }, UI_MAP_PANEL, UI_MAP_CLOSE_BUTTON);

      MapUi = container;
    }

    void GenerateMapOverlayImage()
    {
      Puts("Generating new map overlay image...");

      using (var image = new Bitmap(MapImageSize, MapImageSize))
      using (var graphics = Graphics.FromImage(image))
      {
        var mapSize = ConVar.Server.worldsize;
        var tileSize = (int)(MapImageSize / (mapSize / 150f));
        var grid = new MapGrid(mapSize);

        var colorPicker = new FactionColorPicker();
        var headquarters = new List<HeadquartersLocation>();
        var textBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

        for (int row = 0; row < grid.Width; row++)
        {
          for (int col = 0; col < grid.Width; col++)
          {
            var x = (col * tileSize);
            var y = (row * tileSize);
            var areaId = grid.GetAreaId(row, col);
            var rect = new Rectangle(x, y, tileSize, tileSize);

            if (BadlandsAreas.Contains(areaId))
            {
              // If the tile is badlands, color it in black.
              var brush = new SolidBrush(Color.FromArgb(192, 0, 0, 0));
              graphics.FillRectangle(brush, rect);
            }
            else
            {
              // If the tile is claimed, fill it with a color indicating the faction.
              Claim claim;
              if (Claims.TryGetValue(areaId, out claim))
              {
                var brush = new SolidBrush(colorPicker.GetColorForFaction(claim.FactionId));
                graphics.FillRectangle(brush, rect);

                if (claim.IsHeadquarters)
                  headquarters.Add(new HeadquartersLocation(claim.FactionId, row, col));
              }
            }
          }
        }

        var gridLabelFont = new Font("Consolas", 14, FontStyle.Bold);
        var gridLabelOffset = 5;
        var gridLinePen = new Pen(Color.FromArgb(192, 0, 0, 0), 2);

        for (int row = 0; row < grid.Width; row++)
        {
          graphics.DrawLine(gridLinePen, 0, (row * tileSize), (grid.Width * tileSize), (row * tileSize));
          graphics.DrawString(grid.GetRowId(row), gridLabelFont, textBrush, gridLabelOffset, (row * tileSize) + gridLabelOffset);
        }

        for (int col = 1; col < grid.Width; col++)
        {
          graphics.DrawLine(gridLinePen, (col * tileSize), 0, (col * tileSize), (grid.Width * tileSize));
          graphics.DrawString(grid.GetColumnId(col), gridLabelFont, textBrush, (col * tileSize) + gridLabelOffset, gridLabelOffset);
        }

        var hqFont = new Font("Arial", 18, FontStyle.Bold);
        foreach (var hq in headquarters)
        {
          var x = (hq.Col == 0 ? 0 : hq.Col - 1) * tileSize;
          var y = (hq.Row == 0 ? 0 : hq.Row - 1) * tileSize;
          var textBoundary = new Rectangle(x, y, tileSize * 3, tileSize * 3);
          var centerTextFormat = new StringFormat
          {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
          };
          graphics.DrawString(hq.FactionId, hqFont, textBrush, textBoundary, centerTextFormat);
        }

        var converter = new ImageConverter();
        var imageData = (byte[]) converter.ConvertTo(image, typeof(byte[]));

        uint previousId = CurrentMapOverlayImageId;
        uint newId = FileStorage.server.Store(imageData, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, 0);

        Puts($"Created new map overlay image {newId}.");
        CurrentMapOverlayImageId = newId;

        FileStorage.server.RemoveEntityNum(previousId, 0);
        Puts($"Removed previous map overlay image {previousId}.");
      }
    }
    
    // ----------------------------------------------------------------------------------------------------

    public class MapGrid
    {
      public int MapSize { get; private set; }
      public int Width { get; private set; }

      string[] RowIds;
      string[] ColumnIds;
      string[,] AreaIds;

      public MapGrid(int mapSize)
      {
        MapSize = mapSize;
        Width = (int)Math.Ceiling(mapSize / 150f);
        RowIds = new string[Width];
        ColumnIds = new string[Width];
        AreaIds = new string[Width, Width];
        Build();
      }

      public string GetRowId(int row)
      {
        return RowIds[row];
      }

      public string GetColumnId(int col)
      {
        return ColumnIds[col];
      }

      public string GetAreaId(int row, int col)
      {
        return AreaIds[row, col];
      }

      void Build()
      {
        string prefix = "";
        char letter = 'A';

        for (int row = 0; row < Width; row++)
        {
          RowIds[row] = prefix + letter;
          if (letter == 'Z')
          {
            prefix = "A";
            letter = 'A';
          }
          else
          {
            letter++;
          }
        }

        for (int col = 0; col < Width; col++)
          ColumnIds[col] = col.ToString();

        for (int row = 0; row < Width; row++)
        {
          for (int col = 0; col < Width; col++)
            AreaIds[row, col] = RowIds[row] + ColumnIds[col];
        }
      }
    }

    public class Faction
    {
      public string Id;
      public string Description;
      public string OwnerSteamId;
      public HashSet<string> ModeratorSteamIds;
      public HashSet<string> MemberSteamIds;

      public Faction(JObject clanData)
      {
        Id = clanData["tag"].ToString();
        Description = clanData["description"].ToString();
        OwnerSteamId = clanData["owner"].ToString();
        ModeratorSteamIds = new HashSet<string>(clanData["moderators"].Select(token => token.ToString()));
        MemberSteamIds = new HashSet<string>(clanData["members"].Select(token => token.ToString()));
      }

      public bool IsLeader(BasePlayer player)
      {
        return (player.UserIDString == OwnerSteamId) || (ModeratorSteamIds.Contains(player.UserIDString));
      }
    }

    public class Claim
    {
      public string AreaId;
      public string FactionId;
      public ulong ClaimantId;
      public uint CupboardId;
      public bool IsHeadquarters;

      public Claim() { }

      public Claim(string areaId, string factionId, ulong claimantId, uint cupboardId, bool isHeadquarters)
      {
        AreaId = areaId;
        FactionId = factionId;
        ClaimantId = claimantId;
        CupboardId = cupboardId;
        IsHeadquarters = isHeadquarters;
      }
    }

    public class TaxPolicy
    {
      public string FactionId;
      public int TaxRate;
      public uint? TaxChestId;

      public TaxPolicy(string factionId, int taxRate, uint? taxContainerId)
      {
        FactionId = factionId;
        TaxRate = taxRate;
        TaxChestId = taxContainerId;
      }

      public bool IsActive()
      {
        return TaxChestId != null && TaxRate > 0;
      }
    }

    public class Area : MonoBehaviour
    {
      public RustFactions Plugin { get; private set; }
      public string Id { get; private set; }
      public Vector3 Location { get; private set; }
      public Vector3 Size { get; private set; }
      public HashSet<BasePlayer> Players { get; private set; }

      public Area()
      {
        Players = new HashSet<BasePlayer>();
      }

      public void Setup(RustFactions plugin, string id, Vector3 location, Vector3 size)
      {
        Plugin = plugin;
        Id = id;
        Location = location;
        Size = size;

        gameObject.layer = (int)Layer.Reserved1;
        gameObject.name = $"RustFactions Area {id}";
        transform.position = location;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));

        var collider = gameObject.AddComponent<BoxCollider>();
        collider.size = Size;
        collider.isTrigger = true;
        collider.enabled = true;

        gameObject.SetActive(true);
        enabled = true;
      }

      void OnTriggerEnter(Collider collider)
      {
        var player = collider.GetComponentInParent<BasePlayer>();
        if (player != null && Players.Add(player))
          Plugin.OnPlayerEnterArea(this, player);
      }

      void OnTriggerExit(Collider collider)
      {
        var player = collider.GetComponentInParent<BasePlayer>();
        if (player != null && Players.Remove(player))
          Plugin.OnPlayerExitArea(this, player);
      }
    }

    public class PersistentData
    {
      public Claim[] Claims;
      public TaxPolicy[] TaxPolicies;
      public string[] BadlandsAreas;
    }

    enum PlayerInteractionState
    {
      None,
      AddingClaim,
      RemovingClaim,
      SelectingHeadquarters,
      SelectingTaxChest
    }

    enum PlayerMapState
    {
      Hidden,
      Visible
    }

    class PlayerStateTracker<T>
    {
      Dictionary<ulong, T> states = new Dictionary<ulong, T>();

      public T Get(BasePlayer player)
      {
        T state;
        if (states.TryGetValue(player.userID, out state))
          return state;
        else
          return default(T);
      }

      public void Set(BasePlayer player, T state)
      {
        states[player.userID] = state;
      }

      public void Reset(BasePlayer player)
      {
        states.Remove(player.userID);
      }
    }

    class HeadquartersLocation
    {
      public string FactionId;
      public int Row;
      public int Col;

      public HeadquartersLocation(string factionId, int row, int col)
      {
        FactionId = factionId;
        Row = row;
        Col = col;
      }
    }

    class FactionColorPicker
    {
      static string[] Colors = new[]
      {
        "#00FF00", "#0000FF", "#FF0000", "#01FFFE", "#FFA6FE",
        "#FFDB66", "#006401", "#010067", "#95003A", "#007DB5",
        "#FF00F6", "#FFEEE8", "#774D00", "#90FB92", "#0076FF",
        "#D5FF00", "#FF937E", "#6A826C", "#FF029D", "#FE8900",
        "#7A4782", "#7E2DD2", "#85A900", "#FF0056", "#A42400",
        "#00AE7E", "#683D3B", "#BDC6FF", "#263400", "#BDD393",
        "#00B917", "#9E008E", "#001544", "#C28C9F", "#FF74A3",
        "#01D0FF", "#004754", "#E56FFE", "#788231", "#0E4CA1",
        "#91D0CB", "#BE9970", "#968AE8", "#BB8800", "#43002C",
        "#DEFF74", "#00FFC6", "#FFE502", "#620E00", "#008F9C",
        "#98FF52", "#7544B1", "#B500FF", "#00FF78", "#FF6E41",
        "#005F39", "#6B6882", "#5FAD4E", "#A75740", "#A5FFD2",
        "#FFB167", "#009BFF", "#E85EBE"
      };

      Dictionary<string, Color> AssignedColors = new Dictionary<string, Color>();
      int NextColor = 0;

      public Color GetColorForFaction(string factionId)
      {
        Color color;

        if (!AssignedColors.TryGetValue(factionId, out color))
        {
          color = Color.FromArgb(96, ColorTranslator.FromHtml(Colors[NextColor]));
          AssignedColors.Add(factionId, color);
          NextColor = (NextColor + 1) % Colors.Length;
        }

        return color;
      }
    }

    // ----------------------------------------------------------------------------------------------------

  }

}
