namespace Oxide.Plugins
{
  using Oxide.Game.Rust.Cui;
  using System;
  using System.Linq;
  using UnityEngine;

  public partial class Imperium
  {
    class UserHud
    {
      const float IconSize = 0.075f;

      static class PanelColor
      {
        public const string BackgroundNormal = "1 0.95 0.875 0.025";
        public const string BackgroundDanger = "0.77 0.25 0.17 0.5";
        public const string BackgroundSafe = "0.31 0.37 0.20 0.75";
        public const string TextNormal = "0.85 0.85 0.85 0.75";
        public const string TextDanger = "0.85 0.65 0.65 1";
        public const string TextSafe = "0.67 0.89 0.32 1";
      }

      public User User { get; }
      public bool IsDisabled { get; set; }

      public UserHud(User user)
      {
        User = user;
      }

      public void Show()
      {
        if (User.CurrentArea != null)
          CuiHelper.AddUi(User.Player, Build());
      }

      public void Hide()
      {
        CuiHelper.DestroyUi(User.Player, Ui.Element.HudPanel);
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
          Image = { Color = "0 0 0 0", Sprite = Ui.TransparentTexture },
          RectTransform = { AnchorMin = "0.008 0.015", AnchorMax = "0.217 0.113" }
        }, Ui.Element.Hud, Ui.Element.HudPanel);

        Area area = User.CurrentArea;

        if (area.Type != AreaType.Wilderness)
        {
          container.Add(new CuiPanel {
            Image = { Color = PanelColor.BackgroundNormal },
            RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 1" }
          }, Ui.Element.HudPanel, Ui.Element.HudPanelTop);

          if (area.IsClaimed)
          {
            string defensiveBonus = String.Format("{0}%", area.GetDefensiveBonus() * 100);
            AddWidget(container, Ui.Element.HudPanelTop, Ui.HudIcon.Defense, PanelColor.TextNormal, defensiveBonus);
          }

          if (area.IsTaxableClaim)
          {
            string taxRate = String.Format("{0}%", area.GetTaxRate() * 100);
            AddWidget(container, Ui.Element.HudPanelTop, Ui.HudIcon.Taxes, PanelColor.TextNormal, taxRate, 0.33f);
          }

          if (area.Type == AreaType.Badlands)
          {
            string harvestBonus = String.Format("+{0}% Bonus", Instance.Options.Taxes.BadlandsGatherBonus * 100);
            AddWidget(container, Ui.Element.HudPanelTop, Ui.HudIcon.Harvest, PanelColor.TextNormal, harvestBonus);
          }
        }

        container.Add(new CuiPanel {
          Image = { Color = GetLocationBackgroundColor() },
          RectTransform = { AnchorMin = "0 0.35", AnchorMax = "1 0.65" }
        }, Ui.Element.HudPanel, Ui.Element.HudPanelMiddle);

        AddWidget(container, Ui.Element.HudPanelMiddle, GetLocationIcon(), GetLocationTextColor(), GetLocationDescription());

        container.Add(new CuiPanel {
          Image = { Color = PanelColor.BackgroundNormal },
          RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.3" }
        }, Ui.Element.HudPanel, Ui.Element.HudPanelBottom);

        string currentTime = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm");
        AddWidget(container, Ui.Element.HudPanelBottom, Ui.HudIcon.Clock, PanelColor.TextNormal, currentTime);

        string activePlayers = BasePlayer.activePlayerList.Count.ToString();
        AddWidget(container, Ui.Element.HudPanelBottom, Ui.HudIcon.Players, PanelColor.TextNormal, activePlayers, 0.3f);

        string sleepingPlayers = BasePlayer.sleepingPlayerList.Count.ToString();
        AddWidget(container, Ui.Element.HudPanelBottom, Ui.HudIcon.Sleepers, PanelColor.TextNormal, sleepingPlayers, 0.53f);

        string planeIcon = Instance.Hud.GameEvents.IsCargoPlaneActive ? Ui.HudIcon.CargoPlaneIndicatorOn : Ui.HudIcon.CargoPlaneIndicatorOff;
        AddWidget(container, Ui.Element.HudPanelBottom, planeIcon, 0.78f);

        string heliIcon = Instance.Hud.GameEvents.IsHelicopterActive ? Ui.HudIcon.HelicopterIndicatorOn : Ui.HudIcon.HelicopterIndicatorOff;
        AddWidget(container, Ui.Element.HudPanelBottom, heliIcon, 0.88f);

        return container;
      }

      string GetLocationIcon()
      {
        Zone zone = User.CurrentZones.FirstOrDefault();

        if (zone != null)
        {
          switch (zone.Type)
          {
            case ZoneType.SupplyDrop:
              return Ui.HudIcon.SupplyDrop;
            case ZoneType.Debris:
              return Ui.HudIcon.Debris;
            case ZoneType.Monument:
              return Ui.HudIcon.Badlands;
          }
        }

        Area area = User.CurrentArea;

        if (area.IsWarZone)
          return Ui.HudIcon.WarZone;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return Ui.HudIcon.Badlands;
          case AreaType.Claimed:
            return Ui.HudIcon.Claimed;
          case AreaType.Headquarters:
            return Ui.HudIcon.Headquarters;
          case AreaType.Town:
            return Ui.HudIcon.Town;
          default:
            return Ui.HudIcon.Wilderness;
        }
      }

      string GetLocationDescription()
      {
        Area area = User.CurrentArea;
        Zone zone = User.CurrentZones.FirstOrDefault();

        if (zone != null)
          return $"{area.Id}: {zone.Name}";

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

      string GetLocationBackgroundColor()
      {
        if (User.CurrentZones.Count > 0)
          return PanelColor.BackgroundDanger;

        Area area = User.CurrentArea;

        if (area.IsWarZone)
          return PanelColor.BackgroundDanger;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return PanelColor.BackgroundDanger;
          case AreaType.Town:
            return PanelColor.BackgroundSafe;
          default:
            return PanelColor.BackgroundNormal;
        }
      }

      string GetLocationTextColor()
      {
        if (User.CurrentZones.Count > 0)
          return PanelColor.TextDanger;

        Area area = User.CurrentArea;

        if (area.IsWarZone)
          return PanelColor.TextDanger;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return PanelColor.TextDanger;
          case AreaType.Town:
            return PanelColor.TextSafe;
          default:
            return PanelColor.TextNormal;
        }
      }

      void AddWidget(CuiElementContainer container, string parent, string iconName, string textColor, string text, float left = 0f)
      {
        var guid = Guid.NewGuid().ToString();

        container.Add(new CuiElement {
          Name = Ui.Element.HudPanelIcon + guid,
          Parent = parent,
          Components = {
            Instance.Hud.CreateImageComponent(iconName),
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
        }, parent, Ui.Element.HudPanelText + guid);
      }

      void AddWidget(CuiElementContainer container, string parent, string iconName, float left = 0f)
      {
        var guid = Guid.NewGuid().ToString();

        container.Add(new CuiElement {
          Name = Ui.Element.HudPanelIcon + guid,
          Parent = parent,
          Components = {
            Instance.Hud.CreateImageComponent(iconName),
            new CuiRectTransformComponent {
              AnchorMin = $"{left} {IconSize}",
              AnchorMax = $"{left + IconSize} {1 - IconSize}",
              OffsetMin = "6 0",
              OffsetMax = "6 0"
            }
          }
        });
      }
    }
  }
}
