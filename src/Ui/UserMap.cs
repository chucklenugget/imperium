namespace Oxide.Plugins
{
  using System;
  using Oxide.Game.Rust.Cui;
  using UnityEngine;
  using System.Linq;

  public partial class Imperium
  {
    class UserMap
    {
      public User User { get; }
      public bool IsVisible { get; private set; }

      public UserMap(User user)
      {
        User = user;
      }

      public void Show()
      {
        CuiHelper.AddUi(User.Player, BuildDialog());
        CuiHelper.AddUi(User.Player, BuildSidebar());
        CuiHelper.AddUi(User.Player, BuildMapLayers());
        IsVisible = true;
      }

      public void Hide()
      {
        CuiHelper.DestroyUi(User.Player, Ui.Element.MapDialog);
        IsVisible = false;
      }

      public void Toggle()
      {
        if (IsVisible)
          Hide();
        else
          Show();
      }

      public void Refresh()
      {
        if (IsVisible)
        {
          CuiHelper.DestroyUi(User.Player, Ui.Element.MapSidebar);
          CuiHelper.DestroyUi(User.Player, Ui.Element.MapLayers);
          CuiHelper.AddUi(User.Player, BuildSidebar());
          CuiHelper.AddUi(User.Player, BuildMapLayers());
        }
      }

      // --- Dialog ---

      CuiElementContainer BuildDialog()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0.75" },
          RectTransform = { AnchorMin = "0.164 0.014", AnchorMax = "0.836 0.986" },
          CursorEnabled = true
        }, Ui.Element.Overlay, Ui.Element.MapDialog);

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0" },
          RectTransform = { AnchorMin = "0.012 0.014", AnchorMax = "0.774 0.951" }
        }, Ui.Element.MapDialog, Ui.Element.MapContainer);

        AddDialogHeader(container);
        AddMapTerrainImage(container);

        return container;
      }

      void AddDialogHeader(CuiElementContainer container)
      {
        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 1" },
          RectTransform = { AnchorMin = "0 0.966", AnchorMax = "0.999 0.999" }
        }, Ui.Element.MapDialog, Ui.Element.MapHeader);

        container.Add(new CuiLabel {
          Text = { Text = ConVar.Server.hostname, FontSize = 13, Align = TextAnchor.MiddleLeft, FadeIn = 0 },
          RectTransform = { AnchorMin = "0.012 0.025", AnchorMax = "0.099 0.917" }
        }, Ui.Element.MapHeader, Ui.Element.MapHeaderTitle);

        container.Add(new CuiButton {
          Text = { Text = "X", FontSize = 13, Align = TextAnchor.MiddleCenter },
          Button = { Color = "0 0 0 0", Command = "imperium.map.toggle", FadeIn = 0 },
          RectTransform = { AnchorMin = "0.972 0.083", AnchorMax = "0.995 0.917" }
        }, Ui.Element.MapHeader, Ui.Element.MapHeaderCloseButton);
      }

      void AddMapTerrainImage(CuiElementContainer container)
      {
        CuiRawImageComponent image = Instance.Hud.CreateImageComponent(Instance.Options.Map.ImageUrl);

        // If the image hasn't been loaded, just display a black box so we don't cause an RPC AddUI crash.
        if (image == null)
          image = new CuiRawImageComponent { Color = "0 0 0 1" };

        container.Add(new CuiElement {
          Name = Ui.Element.MapTerrainImage,
          Parent = Ui.Element.MapContainer,
          Components = {
            image,
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });
      }

      // --- Sidebar ---

      CuiElementContainer BuildSidebar()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0" },
          RectTransform = { AnchorMin = "0.786 0.014", AnchorMax = "0.988 0.951" }
        }, Ui.Element.MapDialog, Ui.Element.MapSidebar);

        AddLayerToggleButtons(container);
        AddServerLogo(container);

        return container;
      }

      void AddLayerToggleButtons(CuiElementContainer container)
      {
        container.Add(new CuiButton {
          Text = { Text = "Land Claims", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = GetButtonColor(UserMapLayer.Claims), Command = "imperium.map.togglelayer claims", FadeIn = 0 },
          RectTransform = { AnchorMin = "0 0.924", AnchorMax = "1 1" }
        }, Ui.Element.MapSidebar, Ui.Element.MapButton + Guid.NewGuid().ToString());

        container.Add(new CuiButton {
          Text = { Text = "Faction Headquarters", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = GetButtonColor(UserMapLayer.Headquarters), Command = "imperium.map.togglelayer headquarters", FadeIn = 0 },
          RectTransform = { AnchorMin = "0 0.832", AnchorMax = "1 0.909" }
        }, Ui.Element.MapSidebar, Ui.Element.MapButton + Guid.NewGuid().ToString());

        container.Add(new CuiButton {
          Text = { Text = "Monuments", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = GetButtonColor(UserMapLayer.Monuments), Command = "imperium.map.togglelayer monuments", FadeIn = 0 },
          RectTransform = { AnchorMin = "0 0.741", AnchorMax = "1 0.817" }
        }, Ui.Element.MapSidebar, Ui.Element.MapButton + Guid.NewGuid().ToString());

        container.Add(new CuiButton {
          Text = { Text = "Pins", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = GetButtonColor(UserMapLayer.Pins), Command = "imperium.map.togglelayer pins", FadeIn = 0 },
          RectTransform = { AnchorMin = "0 0.649", AnchorMax = "1 0.726" }
        }, Ui.Element.MapSidebar, Ui.Element.MapButton + Guid.NewGuid().ToString());
      }

      void AddServerLogo(CuiElementContainer container)
      {
        CuiRawImageComponent image = Instance.Hud.CreateImageComponent(Instance.Options.Map.ServerLogoUrl);

        // If the image hasn't been loaded, just display a black box so we don't cause an RPC AddUI crash.
        if (image == null)
          image = new CuiRawImageComponent { Color = "0 0 0 1" };

        container.Add(new CuiElement {
          Name = Ui.Element.MapServerLogoImage,
          Parent = Ui.Element.MapSidebar,
          Components = {
            image,
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 0.133" }
          }
        });
      }

      // --- Map Layers ---

      CuiElementContainer BuildMapLayers()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0" },
          RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
        }, Ui.Element.MapContainer, Ui.Element.MapLayers);

        if (User.Preferences.IsMapLayerVisible(UserMapLayer.Claims))
          AddClaimsLayer(container);

        if (User.Preferences.IsMapLayerVisible(UserMapLayer.Monuments))
          AddMonumentsLayer(container);

        if (User.Preferences.IsMapLayerVisible(UserMapLayer.Headquarters))
          AddHeadquartersLayer(container);

        if (User.Preferences.IsMapLayerVisible(UserMapLayer.Pins))
          AddPinsLayer(container);

        AddMarker(container, MapMarker.ForUser(User));

        return container;
      }

      void AddClaimsLayer(CuiElementContainer container)
      {
        CuiRawImageComponent image = Instance.Hud.CreateImageComponent(Ui.MapOverlayImageUrl);

        // If the claims overlay hasn't been generated yet, just display a black box so we don't cause an RPC AddUI crash.
        if (image == null)
          image = new CuiRawImageComponent { Color = "0 0 0 1" };

        container.Add(new CuiElement {
          Name = Ui.Element.MapClaimsImage,
          Parent = Ui.Element.MapLayers,
          Components = {
            image,
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });
      }

      void AddMonumentsLayer(CuiElementContainer container)
      {
        var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
        foreach (MonumentInfo monument in monuments.Where(ShowMonumentOnMap))
          AddMarker(container, MapMarker.ForMonument(monument));
      }

      void AddHeadquartersLayer(CuiElementContainer container)
      {
        foreach (Area area in Instance.Areas.GetAllByType(AreaType.Headquarters))
        {
          var faction = Instance.Factions.Get(area.FactionId);
          AddMarker(container, MapMarker.ForHeadquarters(area, faction));
        }
      }

      void AddPinsLayer(CuiElementContainer container)
      {
        foreach (Pin pin in Instance.Pins.GetAll())
          AddMarker(container, MapMarker.ForPin(pin));
      }

      void AddMarker(CuiElementContainer container, MapMarker marker, float iconSize = 0.01f)
      {
        container.Add(new CuiElement {
          Name = Ui.Element.MapMarkerIcon + Guid.NewGuid().ToString(),
          Parent = Ui.Element.MapLayers,
          Components = {
            Instance.Hud.CreateImageComponent(marker.IconUrl),
            new CuiRectTransformComponent {
              AnchorMin = $"{marker.X - iconSize} {marker.Z - iconSize}",
              AnchorMax = $"{marker.X + iconSize} {marker.Z + iconSize}"
            }
          }
        });

        if (!String.IsNullOrEmpty(marker.Label))
        {
          container.Add(new CuiLabel {
            Text = { Text = marker.Label, FontSize = 8, Align = TextAnchor.MiddleCenter, FadeIn = 0 },
            RectTransform = {
              AnchorMin = $"{marker.X - 0.1} {marker.Z - iconSize - 0.0175}",
              AnchorMax = $"{marker.X + 0.1} {marker.Z - iconSize}"
            }
          }, Ui.Element.MapLayers, Ui.Element.MapMarkerLabel + Guid.NewGuid().ToString());
        }
      }

      bool ShowMonumentOnMap(MonumentInfo monument)
      {
        return monument.Type != MonumentType.Cave
          && !monument.name.Contains("power_sub")
          && !monument.name.Contains("water_well");
      }

      string GetButtonColor(UserMapLayer layer)
      {
        if (User.Preferences.IsMapLayerVisible(layer))
          return "0 0 0 1";
        else
          return "0.33 0.33 0.33 1";
      }
    }
  }
}
