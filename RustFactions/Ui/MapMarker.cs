namespace Oxide.Plugins
{
  using System;

  public partial class RustFactions
  {
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
          IconUrl = UiMapIcon.Player,
          X = TranslatePosition(user.Player.transform.position.x),
          Z = TranslatePosition(user.Player.transform.position.z)
        };
      }

      public static MapMarker ForHeadquarters(Area area, Faction faction)
      {
        return new MapMarker {
          IconUrl = UiMapIcon.Headquarters,
          Label = RemoveSpecialCharacters(faction.Description),
          X = TranslatePosition(area.ClaimCupboard.transform.position.x),
          Z = TranslatePosition(area.ClaimCupboard.transform.position.z)
        };
      }

      public static MapMarker ForTown(Area area)
      {
        return new MapMarker {
          IconUrl = UiMapIcon.Town,
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
          Label = (iconUrl == UiMapIcon.Unknown) ? monument.name : null,
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
        if (monument.Type == MonumentType.Cave) return UiMapIcon.Cave;
        if (monument.name.Contains("lighthouse")) return UiMapIcon.Lighthouse;
        if (monument.name.Contains("harbor")) return UiMapIcon.Harbor;
        if (monument.name.Contains("powerplant")) return UiMapIcon.PowerPlant;
        if (monument.name.Contains("military_tunnel")) return UiMapIcon.MilitaryTunnel;
        if (monument.name.Contains("airfield")) return UiMapIcon.Airfield;
        if (monument.name.Contains("trainyard")) return UiMapIcon.Trainyard;
        if (monument.name.Contains("water_treatment_plant")) return UiMapIcon.WaterTreatmentPlant;
        if (monument.name.Contains("warehouse")) return UiMapIcon.MiningOutpost;
        if (monument.name.Contains("satellite_dish")) return UiMapIcon.SatelliteDish;
        if (monument.name.Contains("sphere_tank")) return UiMapIcon.Dome;
        if (monument.name.Contains("launch_site")) return UiMapIcon.LaunchSite;
        if (monument.name.Contains("gas_station")) return UiMapIcon.GasStation;
        if (monument.name.Contains("supermarket")) return UiMapIcon.Supermarket;
        if (monument.name.Contains("power_sub")) return UiMapIcon.Substation;
        if (monument.name.Contains("quarry")) return UiMapIcon.Quarry;
        if (monument.name.Contains("radtown_small_3")) return UiMapIcon.SewerBranch;
        return UiMapIcon.Unknown;
      }
    }
  }
}
