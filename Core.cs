// Reference: System.Drawing
// Requires: Clans

namespace Oxide.Plugins
{
  using System;
  using System.Text;
  using Oxide.Core;
  using Oxide.Core.Configuration;
  using Oxide.Core.Plugins;

  [Info("RustFactions", "chucklenugget", "1.1.0")]
  public partial class RustFactions : RustPlugin
  {
    [PluginReference] Plugin Clans;

    DynamicConfigFile DataFile;
    DynamicConfigFile HistoryFile;
    DynamicConfigFile ImagesFile;
    RustFactionsOptions Options;
    Timer UpkeepCollectionTimer;

    AreaManager Areas;
    FactionManager Factions;
    DiplomacyManager Diplomacy;
    UserManager Users;
    HistoryManager History;
    UiManager Ui;

    const string PERM_CHANGE_CLAIMS = "rustfactions.claims";
    const string PERM_CHANGE_BADLANDS = "rustfactions.badlands";
    const string PERM_CHANGE_TOWNS = "rustfactions.towns";

    const string UI_TRANSPARENT_TEXTURE = "assets/content/textures/generic/fulltransparent.tga";

    void Init()
    {
      PrintToChat($"{Title} v{Version} initialized.");

      DataFile = Interface.Oxide.DataFileSystem.GetFile("RustFactions");
      HistoryFile = Interface.Oxide.DataFileSystem.GetDatafile("RustFactionsHistory");
      ImagesFile = Interface.Oxide.DataFileSystem.GetDatafile("RustFactionsImages");

      Factions = new FactionManager(this);
      Areas = new AreaManager(this);
      Diplomacy = new DiplomacyManager(this);
      Users = new UserManager(this);
      History = new HistoryManager(this);
      Ui = new UiManager(this);
    }

    void Loaded()
    {
      InitLang();
      Options = LoadOptions(Config);
      Puts("Area claims are " + (Options.EnableAreaClaims ? "enabled" : "disabled"));
      Puts("Taxation is " + (Options.EnableTaxation ? "enabled" : "disabled"));
      Puts("Badlands are " + (Options.EnableBadlands ? "enabled" : "disabled"));
      Puts("Towns are " + (Options.EnableTowns ? "enabled" : "disabled"));
      Puts("Defensive bonuses are " + (Options.EnableDefensiveBonuses ? "enabled" : "disabled"));
      Puts("Claim upkeep is " + (Options.EnableUpkeep ? "enabled" : "disabled"));
    }

    void Unload()
    {
      Ui.Destroy();
      Users.Destroy();
      Diplomacy.Destroy();
      Areas.Destroy();
      Factions.Destroy();

      if (UpkeepCollectionTimer != null && !UpkeepCollectionTimer.Destroyed)
        UpkeepCollectionTimer.Destroy();
    }

    void OnServerInitialized()
    {
      if (Clans == null)
        PrintWarning("RustFactions requires the Rust:IO Clans plugin, but it was not found!");

      permission.RegisterPermission(PERM_CHANGE_BADLANDS, this);
      permission.RegisterPermission(PERM_CHANGE_CLAIMS, this);
      permission.RegisterPermission(PERM_CHANGE_TOWNS, this);

      RustFactionsData data = LoadData(this, DataFile);
      History.Load(HistoryFile);
      Ui.Load(ImagesFile);

      Factions.Init(data.Factions);
      Areas.Init(data.Areas);
      Diplomacy.Init(data.Wars);
      Users.Init();

      Ui.GenerateMapOverlayImage();

      if (Options.EnableUpkeep)
        UpkeepCollectionTimer = timer.Every(Options.UpkeepCheckIntervalMinutes * 60, CollectUpkeepForAllFactions);
    }

    void OnServerSave()
    {
      SaveData(DataFile);
      History.Save(HistoryFile);
      Ui.Save(ImagesFile);
    }

    void OnPlayerInit(BasePlayer player)
    {
      if (player == null) return;

      // If the player hasn't fully connected yet, try again in 2 seconds.
      if (player.IsReceivingSnapshot)
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

    [ChatCommand("help")]
    void OnHelpCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      var sb = new StringBuilder();

      sb.AppendLine("<size=18>Welcome to Rust Factions!</size>");
      sb.AppendLine(String.Format("v{0} by <color=#ffd479>chucklenugget</color>", Version));
      sb.AppendLine();

      if (!String.IsNullOrEmpty(Options.RulesUrl))
      {
        sb.AppendLine(String.Format("First, please read the rules at <color=#ffd479>{0}</color>!", Options.RulesUrl));
        sb.AppendLine();
      }

      sb.Append("The following commands are available. To learn more about each command, do <color=#ffd479>/command help</color>. ");
      sb.AppendLine("For example, to learn more about how to claim land, do <color=#ffd479>/claim help</color>.");
      sb.AppendLine();

      sb.AppendLine("<color=#ffd479>/clan</color> Create a faction");
      sb.AppendLine("<color=#ffd479>/claim</color> Claim areas of land");
      sb.AppendLine("<color=#ffd479>/tax</color> Manage taxation of your land");
      sb.AppendLine("<color=#ffd479>/war</color> See active wars, declare war, or offer peace");

      if (user.HasPermission(PERM_CHANGE_TOWNS))
        sb.AppendLine("<color=#ffd479>/town</color> Find nearby towns, or create one yourself");
      else
        sb.AppendLine("<color=#ffd479>/town</color> Find nearby towns");

      if (user.HasPermission(PERM_CHANGE_BADLANDS))
        sb.AppendLine("<color=#ffd479>/badlands</color> Find or change badlands areas");
      else
        sb.AppendLine("<color=#ffd479>/badlands</color> Find badlands (PVP) areas");

      user.SendMessage(sb);
    }

    [ChatCommand("cancel")]
    void OnCancelCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);

      if (user.CurrentInteraction == null)
      {
        user.SendMessage(Messages.NoInteractionInProgress);
        return;
      }

      user.SendMessage(Messages.InteractionCanceled);
      user.CancelInteraction();
    }

    void OnHammerHit(BasePlayer player, HitInfo hit)
    {
      User user = Users.Get(player);

      if (user != null && user.CurrentInteraction != null)
        user.CompleteInteraction(hit);
    }

    object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hit)
    {
      if (!Options.EnableDefensiveBonuses)
        return null;

      if (entity == null || hit == null)
        return null;

      User user = Users.Get(hit.InitiatorPlayer);
      if (user == null)
        return null;

      return ScaleDamageForDefensiveBonus(entity, hit, user);
    }

    void OnEntityKill(BaseNetworkable entity)
    {
      // If a player dies in an area, remove them from the area.
      var player = entity as BasePlayer;
      if (player != null)
      {
        User user = Users.Get(player);
        if (user != null && user.CurrentArea != null)
          user.CurrentArea = null;
      }

      // If a claim TC is destroyed, remove the claim from the area.
      var cupboard = entity as BuildingPrivlidge;
      if (cupboard != null)
      {
        var area = Areas.GetByClaimCupboard(cupboard);
        if (area != null)
        {
          PrintToChat(Messages.AreaClaimLostCupboardDestroyedAnnouncement, area.FactionId, area.Id);
          Areas.Unclaim(area);
        }
      }

      // If a tax chest is destroyed, remove it from the faction data.
      if (Options.EnableTaxation)
      {
        var container = entity as StorageContainer;
        if (container != null)
        {
          Faction faction = Factions.GetByTaxChest(container);
          if (faction != null)
            faction.TaxChest = null;
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

    void OnUserEnterArea(Area area, User user)
    {
      Area previousArea = user.CurrentArea;

      user.CurrentArea = area;
      user.HudPanel.Refresh();

      if (previousArea == null)
        return;

      if (area.Type == AreaType.Badlands && previousArea.Type != AreaType.Badlands)
      {
        // The player has entered the badlands.
        user.SendMessage(Messages.EnteredBadlands);
      }
      else if (area.Type == AreaType.Wilderness && previousArea.Type != AreaType.Wilderness)
      {
        // The player has entered the wilderness.
        user.SendMessage(Messages.EnteredWilderness);
      }
      else if (area.Type == AreaType.Town && previousArea.Type != AreaType.Town)
      {
        // The player has entered a town.
        user.SendMessage(Messages.EnteredTown, area.Name, area.FactionId);
      }
      else if (area.IsClaimed && !previousArea.IsClaimed)
      {
        // The player has entered a faction's territory.
        user.SendMessage(Messages.EnteredClaimedArea, area.FactionId);
      }
      else if (area.IsClaimed && previousArea.IsClaimed && area.FactionId != previousArea.FactionId)
      {
        // The player has crossed a border between the territory of two factions.
        user.SendMessage(Messages.EnteredClaimedArea, area.FactionId);
      }
    }

    void OnUserExitArea(Area area, User user)
    {
      // TODO: If we don't need this hook, we should remove it.
    }

    void OnClanCreate(string factionId)
    {
      Factions.HandleFactionCreated(factionId);
    }

    void OnClanUpdate(string factionId)
    {
      Factions.HandleFactionChanged(factionId);
    }

    void OnClanDestroy(string factionId)
    {
      Area[] areas = Areas.GetAllClaimedByFaction(factionId);

      if (areas.Length > 0)
      {
        foreach (Area area in areas)
          PrintToChat(Messages.AreaClaimLostFactionDisbandedAnnouncement, area.FactionId, area.Id);

        Areas.Unclaim(areas);
      }

      Diplomacy.EndAllWarsForEliminatedFactions();
      Factions.HandleFactionDestroyed(factionId);

      OnFactionsChanged();
    }

    void OnAreasChanged()
    {
      Diplomacy.EndAllWarsForEliminatedFactions();
      Ui.GenerateMapOverlayImage();
      Ui.RefreshUiForAllPlayers();
    }

    void OnFactionsChanged()
    {
      Ui.RefreshUiForAllPlayers();
    }

    void OnDiplomacyChanged()
    {
      Ui.RefreshUiForAllPlayers();
    }

    bool EnforceCommandCooldown(User user)
    {
      int waitSeconds = user.GetSecondsUntilNextCommand();

      if (waitSeconds > 0)
      {
        user.SendMessage(Messages.CommandIsOnCooldown, waitSeconds);
        return false;
      }

      user.CommandCooldownExpirationTime = DateTime.UtcNow.AddSeconds(Options.CommandCooldownSeconds);
      return true;
    }

  }
}
