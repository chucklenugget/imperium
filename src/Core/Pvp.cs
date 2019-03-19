﻿namespace Oxide.Plugins
{
  using System;
  using System.Linq;

  public partial class Imperium
  {
    static class Pvp
    {
      static string[] BlockedPrefabs = new[] {
        "fireball_small",
        "fireball_small_arrow",
        "fireball_small_shotgun",
        "fireexplosion"
      };

      public static object HandleDamageBetweenPlayers(User attacker, User defender, HitInfo hit)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        // Allow players to take the easy way out.
        if (hit.damageTypes.Has(Rust.DamageType.Suicide))
          return null;

        // If the players are both in factions who are currently at war, they can damage each other anywhere.
        if (attacker.Faction != null && defender.Faction != null && Instance.Wars.AreFactionsAtWar(attacker.Faction, defender.Faction))
          return null;

        // If both the attacker and the defender are in PVP mode, or in a PVP area/zone, they can damage one another.
        if (attacker.IsInDanger && defender.IsInDanger)
          return null;

        // Stop the damage.
        return false;
      }

      public static object HandleIncidentalDamage(User defender, HitInfo hit)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        if (hit.Initiator == null)
          return null;

        // If the damage is coming from something other than a blocked prefab, allow it.
        if (!BlockedPrefabs.Contains(hit.Initiator.ShortPrefabName))
          return null;

        // If the player is in a PVP area or in PVP mode, allow the damage.
        if (defender.IsInDanger)
          return null;

        return false;
      }

      public static object HandleTrapTrigger(BaseTrap trap, User defender)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        // A player can always trigger their own traps, to prevent exploiting this mechanic.
        if (defender.Player.userID == trap.OwnerID)
          return null;

        Area trapArea = Instance.Areas.GetByEntityPosition(trap);

        // If the defender is in a faction, they can trigger traps placed in areas claimed by factions with which they are at war.
        if (defender.Faction != null && trapArea.FactionId != null && Instance.Wars.AreFactionsAtWar(defender.Faction.Id, trapArea.FactionId))
          return null;

        // If the defender is in a PVP area or zone, the trap can trigger.
        // TODO: Ensure the trap is also in the PVP zone.
        if (defender.IsInDanger)
          return null;

        // Stop the trap from triggering.
        return false;
      }

      public static object HandleTurretTarget(BaseCombatEntity turret, User defender)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        // A player can be always be targeted by their own turrets, to prevent exploiting this mechanic.
        if (defender.Player.userID == turret.OwnerID)
          return null;

        Area turretArea = Instance.Areas.GetByEntityPosition(turret);

        if (turretArea == null || defender.CurrentArea == null)
          return null;

        // If the defender is in a faction, they can be targeted by turrets in areas claimed by factions with which they are at war.
        if (defender.Faction != null && turretArea.FactionId != null && Instance.Wars.AreFactionsAtWar(defender.Faction.Id, turretArea.FactionId))
          return null;

        // If the defender is in a PVP area or zone, the turret can trigger.
        // TODO: Ensure the turret is also in the PVP zone.
        if (defender.IsInDanger)
          return null;

        return false;
      }
    }
  }
}
