namespace Oxide.Plugins
{
  using System;

  public partial class RustFactions
  {
    static class MapMarkerIcon
    {
      public const string Headquarters = UI_IMAGE_BASE_URL + "icons/unknown.png";
      public const string Monument = UI_IMAGE_BASE_URL + "icons/unknown.png";
      public const string Player = UI_IMAGE_BASE_URL + "icons/marker.png";
      public const string Shop = UI_IMAGE_BASE_URL + "icons/unknown.png";
      public const string Town = UI_IMAGE_BASE_URL + "icons/unknown.png";
    }

    class MapMarker
    {
      public string IconUrl;
      public string Label;
      public float X;
      public float Z;

      public MapMarker(BasePlayer player, string label = null)
      {
        IconUrl = MapMarkerIcon.Player;
        Label = label ?? player.displayName.RemoveSpecialCharacters();
        X = TranslatePosition(player.transform.position.x);
        Z = TranslatePosition(player.transform.position.z);
      }

      public MapMarker(Faction faction, Claim claim, BuildingPrivlidge cupboard)
      {
        IconUrl = MapMarkerIcon.Headquarters;
        Label = faction.Description.RemoveSpecialCharacters();
        X = TranslatePosition(cupboard.transform.position.x);
        Z = TranslatePosition(cupboard.transform.position.z);
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

      static string GetIconUrl(string iconName)
      {
        return $"http://images.rustfactions.io.s3.amazonaws.com/icons/{iconName}.png";
      }
    }
  }
}
