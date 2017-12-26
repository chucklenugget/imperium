namespace Oxide.Plugins
{
  using System;

  public partial class RustFactions
  {
    static class MapMarkerIcon
    {
      public const string Player = UI_IMAGE_BASE_URL + "icons/marker.png";
      public const string Headquarters = UI_IMAGE_BASE_URL + "icons/unknown.png";
      public const string Shop = UI_IMAGE_BASE_URL + "icons/unknown.png";
      public const string Town = UI_IMAGE_BASE_URL + "icons/unknown.png";
      public const string Cave = UI_IMAGE_BASE_URL + "icons/cave.png";
      public const string Airfield = UI_IMAGE_BASE_URL + "icons/airfield.png";
      public const string SatelliteDish = UI_IMAGE_BASE_URL + "icons/dish.png";
      public const string GasStation = UI_IMAGE_BASE_URL + "icons/gasstation.png";
      public const string Harbor = UI_IMAGE_BASE_URL + "icons/harbor.png";
      public const string LaunchSite = UI_IMAGE_BASE_URL + "icons/launchsite.png";
      public const string Lighthouse = UI_IMAGE_BASE_URL + "icons/lighthouse.png";
      public const string MilitaryTunnel = UI_IMAGE_BASE_URL + "icons/militarytunnel.png";
      public const string Dome = UI_IMAGE_BASE_URL + "icons/spheretank.png";
      public const string Supermarket = UI_IMAGE_BASE_URL + "icons/supermarket.png";
      public const string Trainyard = UI_IMAGE_BASE_URL + "icons/trainyard.png";
      public const string Unknown = UI_IMAGE_BASE_URL + "icons/unknown.png";
      public const string MiningOutpost = UI_IMAGE_BASE_URL + "icons/warehouse.png";
      public const string PowerPlant = UI_IMAGE_BASE_URL + "icons/powerplant.png";
      public const string WaterTreatmentPlant = UI_IMAGE_BASE_URL + "icons/watertreatment.png";
      public const string SewerBranch = UI_IMAGE_BASE_URL + "icons/sewerbranch.png";
      public const string Substation = UI_IMAGE_BASE_URL + "icons/powersub.png";
      public const string Quarry = UI_IMAGE_BASE_URL + "icons/quarry.png";
    }

    class MapMarker
    {
      public string IconUrl;
      public string Label;
      public float X;
      public float Z;

      public static MapMarker ForUser(User user)
      {
        return new MapMarker
        {
          IconUrl = MapMarkerIcon.Player,
          X = TranslatePosition(user.Player.transform.position.x),
          Z = TranslatePosition(user.Player.transform.position.z)
        };
      }

      public static MapMarker ForHeadquarters(Area area, Faction faction)
      {
        return new MapMarker {
          IconUrl = MapMarkerIcon.Headquarters,
          Label = RemoveSpecialCharacters(faction.Description),
          X = TranslatePosition(area.ClaimCupboard.transform.position.x),
          Z = TranslatePosition(area.ClaimCupboard.transform.position.z)
        };
      }

      public static MapMarker ForTown(Area area)
      {
        return new MapMarker {
          IconUrl = MapMarkerIcon.Town,
          Label = RemoveSpecialCharacters(area.Name),
          X = TranslatePosition(area.ClaimCupboard.transform.position.x),
          Z = TranslatePosition(area.ClaimCupboard.transform.position.z)
        };
      }

      public static MapMarker ForMonument(MonumentInfo monument)
      {
        string iconUrl = GetIconForMonument(monument);
        return new MapMarker {
          IconUrl = iconUrl,
          Label = (iconUrl == MapMarkerIcon.Unknown) ? monument.name : null,
          X = TranslatePosition(monument.transform.position.x),
          Z = TranslatePosition(monument.transform.position.z)
        };
      }

      static float TranslatePosition(float pos)
      {
        var mapSize = TerrainMeta.Size.x; // TODO: Different from ConVar.Server.worldsize?
        return (pos + mapSize / 2f) / mapSize;
      }

      static int TranslateRotation(float rot)
      {
        return ((int)Math.Round(rot / 10)) * 10;
      }
      
      static string GetIconForMonument(MonumentInfo monument)
      {
        if (monument.Type == MonumentType.Cave) return MapMarkerIcon.Cave;
        if (monument.name.Contains("lighthouse")) return MapMarkerIcon.Lighthouse;
        if (monument.name.Contains("harbor")) return MapMarkerIcon.Harbor;
        if (monument.name.Contains("powerplant")) return MapMarkerIcon.PowerPlant;
        if (monument.name.Contains("military_tunnel")) return MapMarkerIcon.MilitaryTunnel;
        if (monument.name.Contains("airfield")) return MapMarkerIcon.Airfield;
        if (monument.name.Contains("trainyard")) return MapMarkerIcon.Trainyard;
        if (monument.name.Contains("water_treatment_plant")) return MapMarkerIcon.WaterTreatmentPlant;
        if (monument.name.Contains("warehouse")) return MapMarkerIcon.MiningOutpost;
        if (monument.name.Contains("satellite_dish")) return MapMarkerIcon.SatelliteDish;
        if (monument.name.Contains("sphere_tank")) return MapMarkerIcon.Dome;
        if (monument.name.Contains("launch_site")) return MapMarkerIcon.LaunchSite;
        if (monument.name.Contains("gas_station")) return MapMarkerIcon.GasStation;
        if (monument.name.Contains("supermarket")) return MapMarkerIcon.Supermarket;
        if (monument.name.Contains("power_sub")) return MapMarkerIcon.Substation;
        if (monument.name.Contains("quarry")) return MapMarkerIcon.Quarry;
        if (monument.name.Contains("radtown_small_3")) return MapMarkerIcon.SewerBranch;
        return MapMarkerIcon.Unknown;
      }
    }
  }
}
