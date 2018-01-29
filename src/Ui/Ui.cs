namespace Oxide.Plugins
{
  public partial class Imperium
  {
    const string UI_TRANSPARENT_TEXTURE = "assets/content/textures/generic/fulltransparent.tga";

    const string UiImageBaseUrl = "http://images.rustfactions.io.s3.amazonaws.com/";
    const string UiMapOverlayImageUrl = "imperium://map-overlay.png";

    static class UiElement
    {
      public const string Hud = "Hud";
      public const string HudPanel = "Imperium.HudPanel";
      public const string HudPanelTop = "Imperium.HudPanel.Top";
      public const string HudPanelMiddle = "Imperium.HudPanel.Middle";
      public const string HudPanelBottom = "Imperium.HudPanel.Bottom";
      public const string HudPanelText = "Imperium.HudPanel.Text";
      public const string HudPanelIcon = "Imperium.HudPanel.Icon";
      public const string Map = "Imperium.Map";
      public const string MapCloseButton = "Imperium.Map.CloseButton";
      public const string MapBackgroundImage = "Imperium.Map.BackgroundImage";
      public const string MapClaimsImage = "Imperium.Map.ClaimsImage";
      public const string MapOverlay = "Imperium.Map.Overlay";
      public const string MapIcon = "Imperium.Map.Icon";
      public const string MapLabel = "Imperium.Map.Label";
    }

    static class UiHudIcon
    {
      public const string Badlands = UiImageBaseUrl + "icons/hud/badlands.png";
      public const string Claimed = UiImageBaseUrl + "icons/hud/claimed.png";
      public const string Clock = UiImageBaseUrl + "icons/hud/clock.png";
      public const string Defense = UiImageBaseUrl + "icons/hud/defense.png";
      public const string Harvest = UiImageBaseUrl + "icons/hud/harvest.png";
      public const string Headquarters = UiImageBaseUrl + "icons/hud/headquarters.png";
      public const string Players = UiImageBaseUrl + "icons/hud/players.png";
      public const string Sleepers = UiImageBaseUrl + "icons/hud/sleepers.png";
      public const string Taxes = UiImageBaseUrl + "icons/hud/taxes.png";
      public const string Town = UiImageBaseUrl + "icons/hud/town.png";
      public const string Warzone = UiImageBaseUrl + "icons/hud/warzone.png";
      public const string Wilderness = UiImageBaseUrl + "icons/hud/wilderness.png";
    }

    static class UiMapIcon
    {
      public const string Airfield = UiImageBaseUrl + "icons/map/airfield.png";
      public const string Cave = UiImageBaseUrl + "icons/map/cave.png";
      public const string Dome = UiImageBaseUrl + "icons/map/dome.png";
      public const string GasStation = UiImageBaseUrl + "icons/map/gas-station.png";
      public const string Harbor = UiImageBaseUrl + "icons/map/harbor.png";
      public const string Headquarters = UiImageBaseUrl + "icons/map/headquarters.png";
      public const string Junkyard = UiImageBaseUrl + "icons/map/junkyard.png";
      public const string LaunchSite = UiImageBaseUrl + "icons/map/launch-site.png";
      public const string Lighthouse = UiImageBaseUrl + "icons/map/lighthouse.png";
      public const string MilitaryTunnel = UiImageBaseUrl + "icons/map/military-tunnel.png";
      public const string MiningOutpost = UiImageBaseUrl + "icons/map/mining-outpost.png";
      public const string Player = UiImageBaseUrl + "icons/map/player.png";
      public const string PowerPlant = UiImageBaseUrl + "icons/map/power-plant.png";
      public const string Quarry = UiImageBaseUrl + "icons/map/quarry.png";
      public const string SatelliteDish = UiImageBaseUrl + "icons/map/satellite-dish.png";
      public const string SewerBranch = UiImageBaseUrl + "icons/map/sewer-branch.png";
      public const string Substation = UiImageBaseUrl + "icons/map/substation.png";
      public const string Supermarket = UiImageBaseUrl + "icons/map/supermarket.png";
      public const string Town = UiImageBaseUrl + "icons/map/town.png";
      public const string Trainyard = UiImageBaseUrl + "icons/map/trainyard.png";
      public const string Unknown = UiImageBaseUrl + "icons/map/unknown.png";
      public const string WaterTreatmentPlant = UiImageBaseUrl + "icons/map/water-treatment-plant.png";
    }

    void RefreshUiForAllPlayers()
    {
      foreach (User user in Users.GetAll())
      {
        user.Map.Refresh();
        user.HudPanel.Refresh();
      }
    }
  }
}

