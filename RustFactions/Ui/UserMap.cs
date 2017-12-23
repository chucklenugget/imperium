namespace Oxide.Plugins
{
  using System;
  using Oxide.Game.Rust.Cui;
  using UnityEngine;

  public partial class RustFactions
  {
    class UserMap
    {
      public bool IsVisible { get; private set; }

      RustFactions Plugin;
      User User;

      public UserMap(RustFactions plugin, User user)
      {
        Plugin = plugin;
        User = user;
      }

      public void Show()
      {
        CuiHelper.AddUi(User.Player, Build());
        IsVisible = true;
      }

      public void Hide()
      {
        CuiHelper.DestroyUi(User.Player, UiElements.Map);
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
          CuiHelper.DestroyUi(User.Player, UiElements.Map);
          CuiHelper.AddUi(User.Player, Build());
        }
      }

      CuiElementContainer Build()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 1" },
          RectTransform = { AnchorMin = "0.2271875 0.015", AnchorMax = "0.7728125 0.985" },
          CursorEnabled = true
        }, UiElements.Hud, UiElements.Map);

        container.Add(new CuiElement {
          Name = UiElements.MapBackgroundImage,
          Parent = UiElements.Map,
          Components = {
            new CuiRawImageComponent { Url = Plugin.Options.MapImageUrl, Sprite = UI_TRANSPARENT_TEXTURE },
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });

        container.Add(new CuiElement {
          Name = UiElements.MapClaimsImage,
          Parent = UiElements.Map,
          Components = {
            new CuiRawImageComponent { Png = Plugin.CurrentMapOverlayImageId.ToString(), Sprite = UI_TRANSPARENT_TEXTURE },
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });

        AddMarker(container, new MapMarker(User.Player, "You"));

        foreach (Claim claim in Plugin.Claims.GetAllHeadquarters())
        {
          var faction = Plugin.GetFaction(claim.FactionId);
          var cupboard = BaseNetworkable.serverEntities.Find(claim.CupboardId) as BuildingPrivlidge;
          if (cupboard != null)
            AddMarker(container, new MapMarker(faction, claim, cupboard));
        }

        container.Add(new CuiButton {
          Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = "0 0 0 1", Command = "rustfactions.map.toggle", FadeIn = 0 },
          RectTransform = { AnchorMin = "0.95 0.961", AnchorMax = "0.999 0.999" }
        }, UiElements.Map, UiElements.MapCloseButton);

        return container;
      }

      void AddMarker(CuiElementContainer container, MapMarker marker, float iconSize = 0.01f)
      {
        container.Add(new CuiElement {
          Name = UiElements.MapIcon + Guid.NewGuid().ToString(),
          Parent = UiElements.Map,
          Components = {
            new CuiRawImageComponent { Url = marker.IconUrl, Sprite = UI_TRANSPARENT_TEXTURE },
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
          }, UiElements.Map, UiElements.MapLabel + Guid.NewGuid().ToString());
        }
      }

    }
  }
}
