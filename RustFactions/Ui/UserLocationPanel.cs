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
      RustFactions Plugin;
      User User;

      public UserLocationPanel(RustFactions plugin, User user)
      {
        Plugin = plugin;
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
        if (Plugin.Badlands.Contains(area))
          return $"{area.Id}: Badlands (+{Plugin.Options.BadlandsGatherBonus}% bonus)";

        Town town = Plugin.Towns.Get(area);
        if (town != null)
        {
          Faction faction = Plugin.GetFaction(town.FactionId);
          return $"{area.Id}: {town.Name} ({faction.Id})";
        }

        Claim claim = Plugin.Claims.Get(area);
        if (claim != null)
        {
          Faction faction = Plugin.GetFaction(claim.FactionId);
          TaxPolicy policy = Plugin.Taxes.Get(claim);
          if (policy != null)
            return $"{area.Id}: {faction.Id} ({policy.TaxRate}% tax)";
          else
            return $"{area.Id}: {faction.Id}";
        }

        return $"{area.Id}: Unclaimed";
      }

      string GetBackgroundColor(Area area)
      {
        if (Plugin.Badlands.Contains(area))
          return UserLocationPanelColor.BackgroundDanger;

        if (Plugin.Towns.Contains(area))
          return UserLocationPanelColor.BackgroundSafe;

        return UserLocationPanelColor.BackgroundNormal;
      }

    }
  }
}
