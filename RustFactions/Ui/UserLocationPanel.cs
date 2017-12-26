namespace Oxide.Plugins
{
  using Oxide.Game.Rust.Cui;
  using UnityEngine;

  public partial class RustFactions
  {
    static class UserLocationPanelColor
    {
      public const string BackgroundNormal = "1 0.95 0.875 0.025";
      public const string BackgroundDanger = "0.77 0.25 0.17 1";
      public const string BackgroundSafe = "0.31 0.37 0.20 1";
      public const string Text = "0.85 0.85 0.85 0.5";
    }

    class UserLocationPanel
    {
      RustFactions Core;
      User User;

      public UserLocationPanel(RustFactions core, User user)
      {
        Core = core;
        User = user;
      }

      public void Refresh()
      {
        Hide();
        if (User.CurrentArea != null)
          CuiHelper.AddUi(User.Player, Build());
      }

      public void Hide()
      {
        CuiHelper.DestroyUi(User.Player, UiElements.LocationPanel);
      }

      CuiElementContainer Build()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = GetBackgroundColor(User.CurrentArea) },
          RectTransform = {
            AnchorMin = "0 0",
            AnchorMax = "0.124 0.036",
            OffsetMin = "16 44",
            OffsetMax = "16 44"
          }
        }, UiElements.Hud, UiElements.LocationPanel);

        container.Add(new CuiLabel {
          Text = { Text = GetLabelText(User.CurrentArea), Color = UserLocationPanelColor.Text, FontSize = 14, Align = TextAnchor.MiddleLeft },
          RectTransform = {
            AnchorMin = "0 0",
            AnchorMax = "1 1",
            OffsetMin = "6 0",
            OffsetMax = "6 0"
          }
        }, UiElements.LocationPanel, UiElements.LocationPanelText);

        return container;
      }

      string GetLabelText(Area area)
      {
        switch (area.Type)
        {
          case AreaType.Badlands:
            return $"{area.Id}: Badlands (+{Core.Options.BadlandsGatherBonus}% bonus)";

          case AreaType.Claimed:
          case AreaType.Headquarters:
            Faction faction = Core.Factions.Get(area.FactionId);
            if (faction.CanCollectTaxes)
              return $"{area.Id}: {faction.Id} ({faction.TaxRate}% tax)";
            else
              return $"{area.Id}: {faction.Id}";

          case AreaType.Town:
            return $"{area.Id}: {area.Name} ({area.FactionId})";

          default:
            return $"{area.Id}: Unclaimed";
        }
      }

      string GetBackgroundColor(Area area)
      {
        switch (area.Type)
        {
          case AreaType.Badlands:
            return UserLocationPanelColor.BackgroundDanger;
          case AreaType.Town:
            return UserLocationPanelColor.BackgroundSafe;
          default:
            return UserLocationPanelColor.BackgroundNormal;
        }
      }

    }
  }
}
