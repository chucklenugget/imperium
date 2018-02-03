namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    static class Logistics
    {
      static string[] ProtectedPrefabs = new[]
      {
        "door.hinged",
        "door.double.hinged",
        "window.bars",
        "wall.window",
        "floor.ladder.hatch",
        "floor.frame",
        "wall.frame",
        "shutter",
        "wall.external",
        "gates.external",
        "cupboard",
        "waterbarrel"
      };

      public static object AlterDamage(BaseCombatEntity entity, HitInfo hit)
      {
        if (hit.damageTypes.Has(Rust.DamageType.Decay))
          return AlterDecayDamage(entity, hit);

        User attacker = Instance.Users.Get(hit.InitiatorPlayer);
        var defendingPlayer = entity as BasePlayer;

        if (attacker == null)
          return null;

        if (defendingPlayer != null)
        {
          // One player is damaging another.
          User defender = Instance.Users.Get(defendingPlayer);

          if (defender == null)
            return null;

          return AlterDamageBetweenPlayers(attacker, defender, hit);
        }

        // A player is damaging a structure.
        return AlterDamageAgainstStructure(attacker, entity, hit);
      }

      public static object AlterTrapTrigger(BaseTrap trap, User defender)
      {
        if (!Instance.Options.EnableRestrictedPVP)
          return null;

        // A player can trigger their own traps, to prevent exploiting this mechanic.
        if (defender.Player.userID == trap.OwnerID)
          return null;

        Area trapArea = Instance.Areas.GetByEntityPosition(trap, true);
        Area defenderArea = Instance.Areas.GetByEntityPosition(defender.Player);

        // A player can trigger a trap if both are in a danger zone.
        if (trapArea.IsDangerous && defenderArea.IsDangerous)
          return null;

        return false;
      }

      public static object AlterTurretTrigger(BaseCombatEntity turret, User defender)
      {
        if (!Instance.Options.EnableRestrictedPVP)
          return null;

        // A player can be targeted by their own turrets, to prevent exploiting this mechanic.
        if (defender.Player.userID == turret.OwnerID)
          return null;

        Area turretArea = Instance.Areas.GetByEntityPosition(turret, true);
        Area defenderArea = Instance.Areas.GetByEntityPosition(defender.Player);

        // A player can be targeted by a turret if both are in a danger zone.
        if (turretArea.IsDangerous && defenderArea.IsDangerous)
          return null;

        return false;
      }

      static object AlterDamageBetweenPlayers(User attacker, User defender, HitInfo hit)
      {
        if (!Instance.Options.EnableRestrictedPVP)
          return null;

        // Allow players to take the easy way out.
        if (hit.damageTypes.Has(Rust.DamageType.Suicide))
          return null;

        if (attacker.CurrentArea == null)
        {
          Instance.PrintWarning("A player damaged another player from an unknown area. This shouldn't happen.");
          return null;
        }

        if (defender.CurrentArea == null)
        {
          Instance.PrintWarning("A player was damaged in an unknown area. This shouldn't happen.");
          return null;
        }

        // If both the attacker and the defender are in a danger zone, they can damage one another.
        if (attacker.CurrentArea.IsDangerous && defender.CurrentArea.IsDangerous)
          return null;

        // If both the attacker and defender are in an event zone, they can damage one another.
        if (attacker.CurrentZones.Count > 0 && defender.CurrentZones.Count > 0)
          return null;

        // Stop the damage.
        return false;
      }

      static object AlterDamageAgainstStructure(User attacker, BaseCombatEntity entity, HitInfo hit)
      {
        if (!Instance.Options.EnableDefensiveBonuses || !ShouldAwardDefensiveBonus(entity))
          return null;

        // If someone is damaging their own entity, don't alter the damage.
        if (attacker.Player.userID == entity.OwnerID)
          return null;

        Area area = Instance.Areas.GetByEntityPosition(entity, true);

        if (area == null)
        {
          Instance.PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
          return null;
        }

        // If the area isn't owned by a faction, it conveys no defensive bonuses.
        if (!area.IsClaimed)
          return null;

        // If a member of a faction is attacking an entity within their own lands, don't alter the damage.
        if (attacker.Faction != null && attacker.Faction.Id == area.FactionId)
          return null;

        // Structures cannot be damaged, except during war.
        if (!area.IsWarZone)
          return false;

        float reduction = area.GetDefensiveBonus();

        if (reduction >= 1)
          return false;

        if (reduction > 0)
          hit.damageTypes.ScaleAll(reduction);

        return null;
      }

      static object AlterDecayDamage(BaseEntity entity, HitInfo hit)
      {
        if (!Instance.Options.EnableDecayReduction)
          return null;

        Area area = Instance.Areas.GetByEntityPosition(entity, true);
        float reduction = 0;

        if (area.Type == AreaType.Claimed || area.Type == AreaType.Headquarters)
          reduction = Instance.Options.ClaimedLandDecayReduction;

        if (area.Type == AreaType.Town)
          reduction = Instance.Options.TownDecayReduction;

        if (reduction >= 1)
          return false;

        if (reduction > 0)
          hit.damageTypes.Scale(Rust.DamageType.Decay, reduction);

        return null;
      }

      static bool ShouldAwardDefensiveBonus(BaseEntity entity)
      {
        var buildingBlock = entity as BuildingBlock;

        if (buildingBlock != null)
          return buildingBlock.grade != BuildingGrade.Enum.Twigs;

        if (ProtectedPrefabs.Any(prefab => entity.ShortPrefabName.Contains(prefab)))
          return true;

        return false;
      }
    }
  }
}
