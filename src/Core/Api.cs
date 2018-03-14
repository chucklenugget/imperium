namespace Oxide.Plugins
{
  using Oxide.Core;

  public partial class Imperium : RustPlugin
  {
    static class Api
    {
      public static void OnAreaChanged(Area area)
      {
        Interface.Call(nameof(OnAreaChanged), area);
      }

      public static void OnUserEnteredArea(User user, Area area)
      {
        Interface.Call(nameof(OnUserEnteredArea), user, area);
      }

      public static void OnUserLeftArea(User user, Area area)
      {
        Interface.Call(nameof(OnUserLeftArea), user, area);
      }

      public static void OnUserEnteredZone(User user, Zone zone)
      {
        Interface.Call(nameof(OnUserEnteredZone), user, zone);
      }

      public static void OnUserLeftZone(User user, Zone zone)
      {
        Interface.Call(nameof(OnUserLeftZone), user, zone);
      }

      public static void OnFactionCreated(Faction faction)
      {
        Interface.Call(nameof(OnFactionCreated), faction);
      }

      public static void OnFactionDisbanded(Faction faction)
      {
        Interface.Call(nameof(OnFactionDisbanded), faction);
      }

      public static void OnFactionTaxesChanged(Faction faction)
      {
        Interface.Call(nameof(OnFactionTaxesChanged), faction);
      }

      public static void OnPlayerJoinedFaction(Faction faction, User user)
      {
        Interface.Call(nameof(OnPlayerJoinedFaction), faction, user);
      }

      public static void OnPlayerLeftFaction(Faction faction, User user)
      {
        Interface.Call(nameof(OnPlayerLeftFaction), faction, user);
      }

      public static void OnPlayerInvitedToFaction(Faction faction, User user)
      {
        Interface.Call(nameof(OnPlayerInvitedToFaction), faction, user);
      }

      public static void OnPlayerUninvitedFromFaction(Faction faction, User user)
      {
        Interface.Call(nameof(OnPlayerUninvitedFromFaction), faction, user);
      }

      public static void OnPlayerPromoted(Faction faction, User user)
      {
        Interface.Call(nameof(OnPlayerPromoted), faction, user);
      }

      public static void OnPlayerDemoted(Faction faction, User user)
      {
        Interface.Call(nameof(OnPlayerDemoted), faction, user);
      }

      public static void OnPinCreated(Pin pin)
      {
        Interface.Call(nameof(OnPinCreated), pin);
      }

      public static void OnPinRemoved(Pin pin)
      {
        Interface.Call(nameof(OnPinRemoved), pin);
      }
    }
  }
}
