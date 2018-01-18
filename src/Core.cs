// Reference: System.Drawing
// Requires: Clans

namespace Oxide.Plugins
{
  using System;
  using System.IO;
  using Oxide.Core;
  using Oxide.Core.Configuration;
  using Oxide.Core.Plugins;

  [Info("Imperium", "chucklenugget", "1.1.0")]
  public partial class Imperium : RustPlugin
  {
    [PluginReference] Plugin Clans;

    DynamicConfigFile DataFile;
    DynamicConfigFile HistoryFile;
    DynamicConfigFile ImagesFile;
    ImperiumOptions Options;
    Timer UpkeepCollectionTimer;

    AreaManager Areas;
    FactionManager Factions;
    DiplomacyManager Diplomacy;
    UserManager Users;
    HistoryManager History;
    UiManager Ui;

    const string PERM_CHANGE_CLAIMS = "imperium.claims";
    const string PERM_CHANGE_BADLANDS = "imperium.badlands";
    const string PERM_CHANGE_TOWNS = "imperium.towns";

    const string UI_TRANSPARENT_TEXTURE = "assets/content/textures/generic/fulltransparent.tga";

    void Init()
    {
      PrintToChat($"{Title} v{Version} initialized.");

      DataFile = Interface.Oxide.DataFileSystem.GetFile(Name + Path.DirectorySeparatorChar + "data");
      HistoryFile = Interface.Oxide.DataFileSystem.GetDatafile(Name + Path.DirectorySeparatorChar + "history");
      ImagesFile = Interface.Oxide.DataFileSystem.GetDatafile(Name + Path.DirectorySeparatorChar + "images");

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
        PrintWarning("Imperium requires the Rust:IO Clans plugin, but it was not found!");

      permission.RegisterPermission(PERM_CHANGE_BADLANDS, this);
      permission.RegisterPermission(PERM_CHANGE_CLAIMS, this);
      permission.RegisterPermission(PERM_CHANGE_TOWNS, this);

      ImperiumSavedData data = LoadData(this, DataFile);
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

    bool EnsureCanChangeFactionClaims(User user, Faction faction)
    {
      if (faction == null || !faction.IsLeader(user))
      {
        user.SendMessage(Messages.InteractionFailedNotLeaderOfFaction);
        return false;
      }

      if (faction.MemberSteamIds.Count < Options.MinFactionMembers)
      {
        user.SendMessage(Messages.InteractionFailedFactionTooSmall, Options.MinFactionMembers);
        return false;
      }

      return true;
    }

    bool EnsureCanUseCupboardAsClaim(User user, BuildingPrivlidge cupboard)
    {
      if (cupboard == null)
      {
        user.SendMessage(Messages.SelectingCupboardFailedInvalidTarget);
        return false;
      }

      if (!cupboard.IsAuthed(user.Player))
      {
        user.SendMessage(Messages.SelectingCupboardFailedNotAuthorized);
        return false;
      }

      return true;
    }

    bool EnsureCanManageTowns(User user, Faction faction)
    {
      if (!user.HasPermission(PERM_CHANGE_TOWNS))
      {
        user.SendMessage(Messages.CannotManageTownsNoPermission);
        return false;
      }

      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMemberOfFaction);
        return false;
      }

      return true;
    }

    bool EnsureCanUseCupboardAsTown(User user, BuildingPrivlidge cupboard)
    {
      if (cupboard == null)
      {
        user.SendMessage(Messages.SelectingCupboardFailedInvalidTarget);
        return false;
      }

      if (!cupboard.IsAuthed(user.Player))
      {
        user.SendMessage(Messages.SelectingCupboardFailedNotAuthorized);
        return false;
      }

      return true;
    }

    bool EnsureCanEngageInDiplomacy(User user, Faction faction)
    {
      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMemberOfFaction);
        return false;
      }

      if (faction.MemberSteamIds.Count < Options.MinFactionMembers)
      {
        user.SendMessage(Messages.InteractionFailedFactionTooSmall);
        return false;
      }

      if (Areas.GetAllClaimedByFaction(faction).Length == 0)
      {
        user.SendMessage(Messages.InteractionFailedFactionDoesNotOwnLand);
        return false;
      }

      return true;
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
