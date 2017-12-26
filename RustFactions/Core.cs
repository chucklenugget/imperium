// Reference: System.Drawing
// Requires: Clans

namespace Oxide.Plugins
{
  using Oxide.Core;
  using Oxide.Core.Configuration;
  using Oxide.Core.Plugins;

  [Info("RustFactions", "chucklenugget", "1.0.0")]
  public partial class RustFactions : RustPlugin
  {
    [PluginReference] Plugin Clans;

    DynamicConfigFile DataFile;
    DynamicConfigFile HistoryFile;
    RustFactionsOptions Options;

    AreaManager Areas;
    FactionManager Factions;
    UserManager Users;
    HistoryManager History;

    uint CurrentMapOverlayImageId;

    const string PERM_CHANGE_CLAIMS = "rustfactions.claims";
    const string PERM_CHANGE_BADLANDS = "rustfactions.badlands";
    const string PERM_CHANGE_TOWNS = "rustfactions.towns";

    const string UI_TRANSPARENT_TEXTURE = "assets/content/textures/generic/fulltransparent.tga";
    const string UI_IMAGE_BASE_URL = "http://images.rustfactions.io.s3.amazonaws.com/";

    public static class UiElements
    {
      public const string Hud = "Hud";
      public const string LocationPanel = "RustFactions.LocationPanel";
      public const string LocationPanelText = "RustFactions.LocationPanel.Text";
      public const string Map = "RustFactions.Map";
      public const string MapCloseButton = "RustFactions.Map.CloseButton";
      public const string MapBackgroundImage = "RustFactions.Map.BackgroundImage";
      public const string MapClaimsImage = "RustFactions.Map.ClaimsImage";
      public const string MapOverlay = "RustFactions.Map.Overlay";
      public const string MapIcon = "RustFactions.Map.Icon";
      public const string MapLabel = "RustFactions.Map.Label";
    }

    void Init()
    {
      PrintToChat($"{this.Title} {this.Version} initialized.");

      DataFile = Interface.Oxide.DataFileSystem.GetFile("RustFactions");
      HistoryFile = Interface.Oxide.DataFileSystem.GetDatafile("RustFactionsHistory");

      Areas = new AreaManager(this);
      Factions = new FactionManager(this);
      Users = new UserManager(this);
      History = new HistoryManager(this);
    }

    void Loaded()
    {
      InitLang();
      Options = LoadOptions(Config);
      Puts("Area Claims are " + (Options.EnableAreaClaims ? "enabled" : "disabled"));
      Puts("Taxation is " + (Options.EnableTaxation ? "enabled" : "disabled"));
      Puts("Badlands are " + (Options.EnableBadlands ? "enabled" : "disabled"));
      Puts("Towns are " + (Options.EnableTowns ? "enabled" : "disabled"));
    }

    void Unload()
    {
      Users.Destroy();
      Factions.Destroy();
      Areas.Destroy();
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

      Areas.Init(data.Areas);
      Factions.Init(data.Factions);
      Users.Init();
      GenerateMapOverlayImage();
    }

    void OnServerSave()
    {
      SaveData(DataFile);
      History.Save(HistoryFile);
    }

    void OnPlayerInit(BasePlayer player)
    {
      if (player == null) return;

      // If the player hasn't fully connected yet, try again in 2 seconds.
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

      if (previousArea != null)
      {
        if (area.Type == AreaType.Badlands && previousArea.Type != AreaType.Badlands)
        {
          // The player has crossed into the badlands.
          user.SendMessage(Messages.EnteredBadlands);
        }
        else if (area.Type == AreaType.Unclaimed && previousArea.Type != AreaType.Unclaimed)
        {
          // The player has crossed a border between the land of a faction and unclaimed land.
          user.SendMessage(Messages.EnteredUnclaimedArea);
        }
        else if (area.Type != AreaType.Unclaimed && previousArea.Type != AreaType.Unclaimed)
        {
          // The player has crosed a border between unclaimed land and the land of a faction.
          user.SendMessage(Messages.EnteredClaimedArea, area.FactionId);
        }
        else if (area.Type != AreaType.Unclaimed && previousArea.Type != AreaType.Unclaimed && area.FactionId != previousArea.FactionId)
        {
          // The player has crossed a border between two factions.
          user.SendMessage(Messages.EnteredClaimedArea, area.FactionId);
        }
      }

      user.LocationPanel.Refresh();
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

      Factions.HandleFactionDestroyed(factionId);
      OnFactionsChanged();
    }

    void OnAreasChanged()
    {
      GenerateMapOverlayImage();
      RefreshUiForAllPlayers();
    }

    void OnFactionsChanged()
    {
      RefreshUiForAllPlayers();
    }

    void RefreshUiForAllPlayers()
    {
      foreach (User user in Users.GetAll())
      {
        user.LocationPanel.Refresh();
        user.Map.Refresh();
      }
    }

  }

}
