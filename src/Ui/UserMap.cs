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
      Imperium Core;
      User User;

      public bool IsVisible { get; private set; }

      public UserMap(Imperium core, User user)
      {
        Core = core;
        User = user;
      }

      public void Show()
      {
        CuiHelper.AddUi(User.Player, Build());
        IsVisible = true;
      }

      public void Hide()
      {
        CuiHelper.DestroyUi(User.Player, UiElement.Map);
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
          CuiHelper.DestroyUi(User.Player, UiElement.Map);
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
        }, UiElement.Hud, UiElement.Map);

        container.Add(new CuiElement {
          Name = UiElement.MapBackgroundImage,
          Parent = UiElement.Map,
          Components = {
            Core.Ui.CreateImageComponent(Core.Options.MapImageUrl),
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });

        container.Add(new CuiElement {
          Name = UiElement.MapClaimsImage,
          Parent = UiElement.Map,
          Components = {
            Core.Ui.CreateImageComponent(UiMapOverlayImageUrl),
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });

        var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
        foreach (MonumentInfo monument in monuments.Where(m => m.Type != MonumentType.Cave && !m.name.Contains("power_sub")))
          AddMarker(container, MapMarker.ForMonument(monument));

        foreach (Area area in Core.Areas.GetAllByType(AreaType.Headquarters))
        {
          var faction = Core.Factions.Get(area.FactionId);
          AddMarker(container, MapMarker.ForHeadquarters(area, faction));
        }

        foreach (Area area in Core.Areas.GetAllByType(AreaType.Town))
          AddMarker(container, MapMarker.ForTown(area));

        AddMarker(container, MapMarker.ForUser(User));

        container.Add(new CuiButton {
          Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = "0 0 0 1", Command = "imperium.map.toggle", FadeIn = 0 },
          RectTransform = { AnchorMin = "0.95 0.961", AnchorMax = "0.999 0.999" }
        }, UiElement.Map, UiElement.MapCloseButton);

        return container;
      }

      void AddMarker(CuiElementContainer container, MapMarker marker, float iconSize = 0.01f)
      {
        container.Add(new CuiElement {
          Name = UiElement.MapIcon + Guid.NewGuid().ToString(),
          Parent = UiElement.Map,
          Components = {
            Core.Ui.CreateImageComponent(marker.IconUrl),
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
          }, UiElement.Map, UiElement.MapLabel + Guid.NewGuid().ToString());
        }
      }

    }
  }
}
