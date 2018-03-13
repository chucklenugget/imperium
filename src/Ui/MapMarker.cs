namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;

  public partial class Imperium
  {
    class MapMarker
    {
      public string IconUrl;
      public string Label;
      public float X;
      public float Z;

      public static MapMarker ForUser(User user)
      {
        return new MapMarker {
          IconUrl = Ui.MapIcon.Player,
          X = TranslatePosition(user.Player.transform.position.x),
          Z = TranslatePosition(user.Player.transform.position.z)
        };
      }

      public static MapMarker ForHeadquarters(Area area, Faction faction)
      {
        return new MapMarker {
          IconUrl = Ui.MapIcon.Headquarters,
          Label = Util.RemoveSpecialCharacters(faction.Id),
          X = TranslatePosition(area.ClaimCupboard.transform.position.x),
          Z = TranslatePosition(area.ClaimCupboard.transform.position.z)
        };
      }

      public static MapMarker ForMonument(MonumentInfo monument)
      {
        string iconUrl = GetIconForMonument(monument);
        return new MapMarker {
          IconUrl = iconUrl,
          Label = (iconUrl == Ui.MapIcon.Unknown) ? monument.name : null,
          X = TranslatePosition(monument.transform.position.x),
          Z = TranslatePosition(monument.transform.position.z)
        };
      }

      static float TranslatePosition(float pos)
      {
        var mapSize = TerrainMeta.Size.x;
        return (pos + mapSize / 2f) / mapSize;
      }
      
      static string GetIconForMonument(MonumentInfo monument)
      {
        if (monument.Type == MonumentType.Cave) return Ui.MapIcon.Cave;
        if (monument.name.Contains("airfield")) return Ui.MapIcon.Airfield;
        if (monument.name.Contains("sphere_tank")) return Ui.MapIcon.Dome;
        if (monument.name.Contains("harbor")) return Ui.MapIcon.Harbor;
        if (monument.name.Contains("gas_station")) return Ui.MapIcon.GasStation;
        if (monument.name.Contains("junkyard")) return Ui.MapIcon.Junkyard;
        if (monument.name.Contains("launch_site")) return Ui.MapIcon.LaunchSite;
        if (monument.name.Contains("lighthouse")) return Ui.MapIcon.Lighthouse;
        if (monument.name.Contains("military_tunnel")) return Ui.MapIcon.MilitaryTunnel;
        if (monument.name.Contains("warehouse")) return Ui.MapIcon.MiningOutpost;
        if (monument.name.Contains("powerplant")) return Ui.MapIcon.PowerPlant;
        if (monument.name.Contains("quarry")) return Ui.MapIcon.Quarry;
        if (monument.name.Contains("satellite_dish")) return Ui.MapIcon.SatelliteDish;
        if (monument.name.Contains("radtown_small_3")) return Ui.MapIcon.SewerBranch;
        if (monument.name.Contains("power_sub")) return Ui.MapIcon.Substation;
        if (monument.name.Contains("supermarket")) return Ui.MapIcon.Supermarket;
        if (monument.name.Contains("trainyard")) return Ui.MapIcon.Trainyard;
        if (monument.name.Contains("water_treatment_plant")) return Ui.MapIcon.WaterTreatmentPlant;
        return Ui.MapIcon.Unknown;
      }
    }
  }
}
