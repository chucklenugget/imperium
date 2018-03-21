namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    class UserPreferences
    {
      public UserMapLayer VisibleMapLayers { get; private set; }

      public void ShowMapLayer(UserMapLayer layer)
      {
        VisibleMapLayers |= layer;
      }

      public void HideMapLayer(UserMapLayer layer)
      {
        VisibleMapLayers &= ~layer;
      }

      public void ToggleMapLayer(UserMapLayer layer)
      {
        if (IsMapLayerVisible(layer))
          HideMapLayer(layer);
        else
          ShowMapLayer(layer);
      }

      public bool IsMapLayerVisible(UserMapLayer layer)
      {
        return (VisibleMapLayers & layer) == layer;
      }

      public static UserPreferences Default = new UserPreferences {
        VisibleMapLayers = UserMapLayer.Claims | UserMapLayer.Headquarters | UserMapLayer.Monuments | UserMapLayer.Pins
      };
    }

    [Flags]
    enum UserMapLayer
    {
      Claims = 1,
      Headquarters = 2,
      Monuments = 4,
      Pins = 8
    }
  }
}
