namespace Oxide.Plugins
{
  using System;
  using System.Linq;

  public partial class Imperium
  {
    static class Pvp
    {
      public static object HandleDamageBetweenPlayers(User attacker, User defender, HitInfo hit)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        // Allow players to take the easy way out.
        if (hit.damageTypes.Has(Rust.DamageType.Suicide))
          return null;

        if (attacker.CurrentArea == null || defender.CurrentArea == null)
        {
          Instance.PrintWarning("A player dealt or received damage while in an unknown area. This shouldn't happen.");
          return null;
        }

        // If the players are both in factions who are currently at war, they can damage each other anywhere.
        if (attacker.Faction != null && defender.Faction != null && Instance.Wars.AreFactionsAtWar(attacker.Faction, defender.Faction))
          return null;

        // If both the attacker and the defender are in a PVP area or zone, they can damage one another.
        if (IsUserInPvpLocation(attacker) && IsUserInPvpLocation(defender))
          return null;

        // Stop the damage.
        return false;
      }

      public static object AlterTrapTrigger(BaseTrap trap, User defender)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        // A player can always trigger their own traps, to prevent exploiting this mechanic.
        if (defender.Player.userID == trap.OwnerID)
          return null;

        Area trapArea = Instance.Areas.GetByEntityPosition(trap);

        if (trapArea == null || defender.CurrentArea == null)
        {
          Instance.PrintWarning("A trap was triggered in an unknown area. This shouldn't happen.");
          return null;
        }

        // If the defender is in a faction, they can trigger traps placed in areas claimed by factions with which they are at war.
        if (defender.Faction != null && trapArea.FactionId != null && Instance.Wars.AreFactionsAtWar(defender.Faction.Id, trapArea.FactionId))
          return null;

        // If both the trap and the defender are in a PVP area, the trap can trigger.
        if (IsPvpArea(trapArea) && IsPvpArea(defender.CurrentArea))
          return null;

        // Stop the trap from triggering.
        return false;
      }

      public static object AlterTurretTrigger(BaseCombatEntity turret, User defender)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        // A player can be always be targeted by their own turrets, to prevent exploiting this mechanic.
        if (defender.Player.userID == turret.OwnerID)
          return null;

        Area turretArea = Instance.Areas.GetByEntityPosition(turret);

        if (turretArea == null || defender.CurrentArea == null)
        {
          Instance.PrintWarning("A turret tried to acquire a target in an unknown area. This shouldn't happen.");
          return null;
        }

        // If the defender is in a faction, they can be targeted by turrets in areas claimed by factions with which they are at war.
        if (defender.Faction != null && turretArea.FactionId != null && Instance.Wars.AreFactionsAtWar(defender.Faction.Id, turretArea.FactionId))
          return null;

        // If both the turret and the defender are in a PVP area, the trap can trigger.
        if (IsPvpArea(turretArea) && IsPvpArea(defender.CurrentArea))
          return null;

        return false;
      }

      static bool IsUserInPvpLocation(User user)
      {
        return IsPvpArea(user.CurrentArea) || user.CurrentZones.Any(IsPvpZone);
      }

      static bool IsPvpZone(Zone zone)
      {
        switch (zone.Type)
        {
          case ZoneType.Debris:
          case ZoneType.SupplyDrop:
            return Instance.Options.Pvp.AllowedInEventZones;
          case ZoneType.Monument:
            return Instance.Options.Pvp.AllowedInMonumentZones;
          default:
            throw new InvalidOperationException($"Unknown zone type {zone.Type}");
        }
      }

      static bool IsPvpArea(Area area)
      {
        switch (area.Type)
        {
          case AreaType.Badlands:
            return Instance.Options.Pvp.AllowedInBadlands;
          case AreaType.Claimed:
          case AreaType.Headquarters:
            return Instance.Options.Pvp.AllowedInClaimedLand;
          case AreaType.Town:
            return Instance.Options.Pvp.AllowedInTowns;
          case AreaType.Wilderness:
            return Instance.Options.Pvp.AllowedInWilderness;
          default:
            throw new InvalidOperationException($"Unknown area type {area.Type}");
        }
      }
    }
  }
}
