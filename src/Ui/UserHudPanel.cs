namespace Oxide.Plugins
{
  using Oxide.Game.Rust.Cui;
  using System;
  using UnityEngine;

  public partial class Imperium
  {
    static class UserHudPanelColor
    {
      public const string BackgroundNormal = "1 0.95 0.875 0.025";
      public const string BackgroundDanger = "0.77 0.25 0.17 0.5";
      public const string BackgroundSafe = "0.31 0.37 0.20 0.75";
      public const string TextNormal = "0.85 0.85 0.85 0.75";
      public const string TextDanger = "0.85 0.65 0.65 1";
      public const string TextSafe = "0.67 0.89 0.32 1";
    }

    class UserHudPanel
    {
      const float IconSize = 0.075f;

      Imperium Core;
      User User;

      public bool IsDisabled { get; set; }

      public UserHudPanel(Imperium core, User user)
      {
        Core = core;
        User = user;
      }

      public void Show()
      {
        if (User.CurrentArea != null)
          CuiHelper.AddUi(User.Player, Build());
      }

      public void Hide()
      {
        CuiHelper.DestroyUi(User.Player, UiElement.HudPanel);
      }

      public void Toggle()
      {
        if (IsDisabled)
        {
          IsDisabled = false;
          Show();
        }
        else
        {
          IsDisabled = true;
          Hide();
        }
      }

      public void Refresh()
      {
        if (IsDisabled)
          return;

        Hide();
        Show();
      }

      CuiElementContainer Build()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0", Sprite = UI_TRANSPARENT_TEXTURE },
          RectTransform = { AnchorMin = "0.008 0.015", AnchorMax = "0.217 0.113" }
        }, UiElement.Hud, UiElement.HudPanel);

        Area area = User.CurrentArea;

        if (area.Type != AreaType.Wilderness)
        {
          container.Add(new CuiPanel {
            Image = { Color = UserHudPanelColor.BackgroundNormal },
            RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 1" }
          }, UiElement.HudPanel, UiElement.HudPanelTop);

          if (area.IsClaimed)
          {
            string defensiveBonus = String.Format("{0}%", area.GetDefensiveBonus() * 100);
            AddWidget(container, UiElement.HudPanelTop, UiHudIcon.Defense, UserHudPanelColor.TextNormal, defensiveBonus);
          }

          if (area.IsTaxableClaim)
          {
            string taxRate = String.Format("{0}%", area.GetTaxRate() * 100);
            AddWidget(container, UiElement.HudPanelTop, UiHudIcon.Taxes, UserHudPanelColor.TextNormal, taxRate, 0.33f);
          }

          if (area.Type == AreaType.Badlands)
          {
            string harvestBonus = String.Format("+{0}% Bonus", Core.Options.BadlandsGatherBonus * 100);
            AddWidget(container, UiElement.HudPanelTop, UiHudIcon.Harvest, UserHudPanelColor.TextNormal, harvestBonus);
          }
        }

        container.Add(new CuiPanel {
          Image = { Color = GetBackgroundColor(area) },
          RectTransform = { AnchorMin = "0 0.35", AnchorMax = "1 0.65" }
        }, UiElement.HudPanel, UiElement.HudPanelMiddle);

        string areaIcon = GetAreaIcon(area);
        string areaDescription = GetAreaDescription(area);
        AddWidget(container, UiElement.HudPanelMiddle, areaIcon, GetTextColor(area), areaDescription);

        container.Add(new CuiPanel {
          Image = { Color = UserHudPanelColor.BackgroundNormal },
          RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.3" }
        }, UiElement.HudPanel, UiElement.HudPanelBottom);

        string currentTime = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm");
        AddWidget(container, UiElement.HudPanelBottom, UiHudIcon.Clock, UserHudPanelColor.TextNormal, currentTime);

        string activePlayers = BasePlayer.activePlayerList.Count.ToString();
        AddWidget(container, UiElement.HudPanelBottom, UiHudIcon.Players, UserHudPanelColor.TextNormal, activePlayers, 0.33f);

        string sleepingPlayers = BasePlayer.sleepingPlayerList.Count.ToString();
        AddWidget(container, UiElement.HudPanelBottom, UiHudIcon.Sleepers, UserHudPanelColor.TextNormal, sleepingPlayers, 0.66f);

        return container;
      }

      string GetAreaIcon(Area area)
      {
        if (area.IsWarzone)
          return UiHudIcon.Warzone;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return UiHudIcon.Badlands;
          case AreaType.Claimed:
            return UiHudIcon.Claimed;
          case AreaType.Headquarters:
            return UiHudIcon.Headquarters;
          case AreaType.Town:
            return UiHudIcon.Town;
          default:
            return UiHudIcon.Wilderness;
        }
      }

      string GetAreaDescription(Area area)
      {
        switch (area.Type)
        {
          case AreaType.Badlands:
            return $"{area.Id}: Badlands";
          case AreaType.Claimed:
            if (!String.IsNullOrEmpty(area.Name))
              return $"{area.Id}: {area.Name} ({area.FactionId})";
            else
              return $"{area.Id}: {area.FactionId} Territory";
          case AreaType.Headquarters:
            if (!String.IsNullOrEmpty(area.Name))
              return $"{area.Id}: {area.Name} ({area.FactionId} HQ)";
            else
              return $"{area.Id}: {area.FactionId} Headquarters";
          case AreaType.Town:
            return $"{area.Id}: {area.Name} ({area.FactionId})";
          default:
            return $"{area.Id}: Wilderness";
        }
      }

      string GetBackgroundColor(Area area)
      {
        if (area.IsWarzone)
          return UserHudPanelColor.BackgroundDanger;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return UserHudPanelColor.BackgroundDanger;
          case AreaType.Town:
            return UserHudPanelColor.BackgroundSafe;
          default:
            return UserHudPanelColor.BackgroundNormal;
        }
      }

      string GetTextColor(Area area)
      {
        if (area.IsWarzone)
          return UserHudPanelColor.TextDanger;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return UserHudPanelColor.TextDanger;
          case AreaType.Town:
            return UserHudPanelColor.TextSafe;
          default:
            return UserHudPanelColor.TextNormal;
        }
      }

      void AddWidget(CuiElementContainer container, string parent, string iconName, string textColor, string text, float left = 0f)
      {
        var guid = Guid.NewGuid().ToString();

        container.Add(new CuiElement {
          Name = UiElement.HudPanelIcon + guid,
          Parent = parent,
          Components = {
            Core.Ui.CreateImageComponent(iconName),
            new CuiRectTransformComponent {
              AnchorMin = $"{left} {IconSize}",
              AnchorMax = $"{left + IconSize} {1 - IconSize}",
              OffsetMin = "6 0",
              OffsetMax = "6 0"
            }
          }
        });

        container.Add(new CuiLabel {
          Text = {
            Text = text,
            Color = textColor,
            FontSize = 14,
            Align = TextAnchor.MiddleLeft
          },
          RectTransform = {
            AnchorMin = $"{left + IconSize} 0",
            AnchorMax = "1 1",
            OffsetMin = "12 0",
            OffsetMax = "12 0"
          }
        }, parent, UiElement.HudPanelText + guid);
      }
    }
  }
}
