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
        CuiHelper.AddUi(User.Player, Build());
        IsVisible = true;
      }

      public void Hide()
      {
        CuiHelper.DestroyUi(User.Player, Ui.Element.Map);
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
          CuiHelper.DestroyUi(User.Player, Ui.Element.Map);
          CuiHelper.AddUi(User.Player, Build());
        }
      }

      CuiElementContainer Build()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 1" },
          RectTransform = { AnchorMin = "0.188 0.037", AnchorMax = "0.813 0.963" },
          CursorEnabled = true
        }, Ui.Element.Hud, Ui.Element.Map);

        container.Add(new CuiElement {
          Name = Ui.Element.MapBackgroundImage,
          Parent = Ui.Element.Map,
          Components = {
            Instance.Hud.CreateImageComponent(Instance.Options.Map.ImageUrl),
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });

        container.Add(new CuiElement {
          Name = Ui.Element.MapClaimsImage,
          Parent = Ui.Element.Map,
          Components = {
            Instance.Hud.CreateImageComponent(Ui.MapOverlayImageUrl),
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });

        var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
        foreach (MonumentInfo monument in monuments.Where(ShowMonumentOnMap))
          AddMarker(container, MapMarker.ForMonument(monument));

        foreach (Area area in Instance.Areas.GetAllByType(AreaType.Headquarters))
        {
          var faction = Instance.Factions.Get(area.FactionId);
          AddMarker(container, MapMarker.ForHeadquarters(area, faction));
        }

        AddMarker(container, MapMarker.ForUser(User));

        container.Add(new CuiButton {
          Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = "0 0 0 1", Command = "imperium.map.toggle", FadeIn = 0 },
          RectTransform = { AnchorMin = "0.95 0.961", AnchorMax = "0.999 0.999" }
        }, Ui.Element.Map, Ui.Element.MapCloseButton);

        return container;
      }

      void AddMarker(CuiElementContainer container, MapMarker marker, float iconSize = 0.01f)
      {
        container.Add(new CuiElement {
          Name = Ui.Element.MapIcon + Guid.NewGuid().ToString(),
          Parent = Ui.Element.Map,
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
            Text = { Text = marker.Label, FontSize = 10, Align = TextAnchor.MiddleCenter, FadeIn = 0 },
            RectTransform = {
              AnchorMin = $"{marker.X - 0.1} {marker.Z - iconSize - 0.025}",
              AnchorMax = $"{marker.X + 0.1} {marker.Z - iconSize}"
            }
          }, Ui.Element.Map, Ui.Element.MapLabel + Guid.NewGuid().ToString());
        }
      }

      bool ShowMonumentOnMap(MonumentInfo monument)
      {
        return monument.Type != MonumentType.Cave
          && !monument.name.Contains("power_sub")
          && !monument.name.Contains("water_well");
      }
    }
  }
}
