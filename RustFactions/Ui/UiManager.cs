namespace Oxide.Plugins
{
  using Oxide.Core.Configuration;
  using Oxide.Game.Rust.Cui;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using UnityEngine;

  public partial class RustFactions
  {
    public const string UiImageBaseUrl = "http://images.rustfactions.io.s3.amazonaws.com/";

    static class UiElement
    {
      public const string Hud = "Hud";
      public const string HudPanel = "RustFactions.HudPanel";
      public const string HudPanelTop = "RustFactions.HudPanel.Top";
      public const string HudPanelMiddle = "RustFactions.HudPanel.Middle";
      public const string HudPanelBottom = "RustFactions.HudPanel.Bottom";
      public const string HudPanelText = "RustFactions.HudPanel.Text";
      public const string HudPanelIcon = "RustFactions.HudPanel.Icon";
      public const string Map = "RustFactions.Map";
      public const string MapCloseButton = "RustFactions.Map.CloseButton";
      public const string MapBackgroundImage = "RustFactions.Map.BackgroundImage";
      public const string MapClaimsImage = "RustFactions.Map.ClaimsImage";
      public const string MapOverlay = "RustFactions.Map.Overlay";
      public const string MapIcon = "RustFactions.Map.Icon";
      public const string MapLabel = "RustFactions.Map.Label";
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

    class UiManager : RustFactionsManager
    {
      Dictionary<string, Image> Images;
      ImageDownloader Downloader;

      public UiManager(RustFactions core)
        : base(core)
      {
        Images = new Dictionary<string, Image>();
        Downloader = new GameObject().AddComponent<ImageDownloader>();
        Downloader.Init(core);
      }

      public void RegisterImage(string url, string id = null)
      {
        if (Images.ContainsKey(url))
          return;

        var image = new Image(url, id);
        Images[url] = image;

        if (id == null)
          Downloader.Download(image);
      }

      public CuiRawImageComponent CreateImageComponent(string imageName)
      {
        Image image;

        if (!Images.TryGetValue(imageName, out image))
        {
          Core.PrintWarning($"Tried to create CuiRawImageComponent for image with name {imageName}, but it wasn't declared.");
          return null;
        }

        if (image.Id != null)
          return new CuiRawImageComponent { Png = image.Id, Sprite = UI_TRANSPARENT_TEXTURE };
        else
          return new CuiRawImageComponent { Url = image.Url, Sprite = UI_TRANSPARENT_TEXTURE };
      }

      public void Load(DynamicConfigFile file)
      {
        try
        {
          Images = file.ReadObject<Image[]>().ToDictionary(image => image.Url);
          Core.Puts($"Loaded {Images.Values.Count} cached images.");
        }
        catch (Exception err)
        {
          Core.PrintWarning($"Error loading cached images: {err}");
        }

        if (!String.IsNullOrEmpty(Core.Options.MapImageUrl))
          RegisterImage(Core.Options.MapImageUrl);

        RegisterDefaultImages(typeof(UiHudIcon));
        RegisterDefaultImages(typeof(UiMapIcon));
      }

      public void Save(DynamicConfigFile file)
      {
        file.WriteObject(Images.Values.ToArray());
      }

      public void Destroy()
      {
        UnityEngine.Object.DestroyImmediate(Downloader);
      }

      void RegisterDefaultImages(Type type)
      {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
          RegisterImage((string)field.GetRawConstantValue());
      }
    }
  }
}
 
 