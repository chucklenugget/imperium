namespace Oxide.Plugins
{
  using Oxide.Core;

  public partial class Imperium : RustPlugin
  {
    static class Events
    {
      public static void OnAreaChanged(Area area)
      {
        Interface.CallHook(nameof(OnAreaChanged), area);
      }

      public static void OnUserEnteredArea(User user, Area area)
      {
        Interface.CallHook(nameof(OnUserEnteredArea), user, area);
      }

      public static void OnUserLeftArea(User user, Area area)
      {
        Interface.CallHook(nameof(OnUserLeftArea), user, area);
      }

      public static void OnUserEnteredZone(User user, Zone zone)
      {
        Interface.CallHook(nameof(OnUserEnteredZone), user, zone);
      }

      public static void OnUserLeftZone(User user, Zone zone)
      {
        Interface.CallHook(nameof(OnUserLeftZone), user, zone);
      }

      public static void OnFactionCreated(Faction faction)
      {
        Interface.CallHook(nameof(OnFactionCreated), faction);
      }

      public static void OnFactionDisbanded(Faction faction)
      {
        Interface.CallHook(nameof(OnFactionDisbanded), faction);
      }

      public static void OnFactionTaxesChanged(Faction faction)
      {
        Interface.CallHook(nameof(OnFactionTaxesChanged), faction);
      }

      public static void OnPlayerJoinedFaction(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerJoinedFaction), faction, user);
      }

      public static void OnPlayerLeftFaction(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerLeftFaction), faction, user);
      }

      public static void OnPlayerInvitedToFaction(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerInvitedToFaction), faction, user);
      }

      public static void OnPlayerUninvitedFromFaction(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerUninvitedFromFaction), faction, user);
      }

      public static void OnPlayerPromoted(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerPromoted), faction, user);
      }

      public static void OnPlayerDemoted(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerDemoted), faction, user);
      }

      public static void OnPinCreated(Pin pin)
      {
        Interface.CallHook(nameof(OnPinCreated), pin);
      }

      public static void OnPinRemoved(Pin pin)
      {
        Interface.CallHook(nameof(OnPinRemoved), pin);
      }
    }
  }
}
