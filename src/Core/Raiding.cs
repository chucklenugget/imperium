namespace Oxide.Plugins
{
  using System;
  using System.Linq;

  public partial class Imperium
  {
    static class Raiding
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

      public static object AlterDamageAgainstStructure(User attacker, BaseCombatEntity entity, HitInfo hit)
      {
        // Players can always damage their own entities.
        if (attacker.Player.userID == entity.OwnerID)
          return null;

        if (!IsProtectedEntity(entity))
          return null;

        Area area = Instance.Areas.GetByEntityPosition(entity, true);

        if (area == null)
        {
          Instance.PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
          return null;
        }

        if (area.FactionId != null && attacker.Faction != null)
        {
          // If a member of a faction is attacking an entity within their own lands, don't alter the damage.
          if (attacker.Faction.Id == area.FactionId)
            return null;

          // If the entity is built on claimed land, it can be damaged during war by enemy faction members.
          if (Instance.Wars.AreFactionsAtWar(attacker.Faction.Id, area.FactionId))
            return ApplyDefensiveBonus(area, hit);
        }

        // If the structure is in a raidable area, it can be damaged.
        if (IsRaidableArea(area))
          return ApplyDefensiveBonus(area, hit);

        // Prevent the damage.
        return false;
      }

      static object ApplyDefensiveBonus(Area area, HitInfo hit)
      {
        float reduction = area.GetDefensiveBonus();

        if (reduction >= 1)
          return false;

        if (reduction > 0)
          hit.damageTypes.ScaleAll(reduction);

        return null;
      }

      static bool IsProtectedEntity(BaseEntity entity)
      {
        var buildingBlock = entity as BuildingBlock;

        if (buildingBlock != null)
          return buildingBlock.grade != BuildingGrade.Enum.Twigs;

        if (ProtectedPrefabs.Any(prefab => entity.ShortPrefabName.Contains(prefab)))
          return true;

        return false;
      }

      static bool IsRaidableArea(Area area)
      {
        switch (area.Type)
        {
          case AreaType.Badlands:
            return Instance.Options.Raiding.AllowedInBadlands;
          case AreaType.Claimed:
          case AreaType.Headquarters:
            return Instance.Options.Raiding.AllowedInClaimedLand;
          case AreaType.Town:
            return Instance.Options.Raiding.AllowedInTowns;
          case AreaType.Wilderness:
            return Instance.Options.Raiding.AllowedInWilderness;
          default:
            throw new InvalidOperationException($"Unknown area type {area.Type}");
        }
      }
    }
  }
}
