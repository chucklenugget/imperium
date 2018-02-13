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
        "waterbarrel",
        "fridge"
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

        if (attacker.Faction != null)
        {
          // Factions can damage any structure built on their own land.
          if (area.FactionId != null && attacker.Faction.Id == area.FactionId)
            return null;

          // Factions who are at war can damage any structure on enemy land, subject to the defensive bonus.
          if (area.FactionId != null && Instance.Wars.AreFactionsAtWar(attacker.Faction.Id, area.FactionId))
            return ApplyDefensiveBonus(area, hit);

          // Factions who are at war can damage any structure built by a member of an enemy faction, subject
          // to the defensive bonus.
          BasePlayer owner = BasePlayer.FindByID(entity.OwnerID);
          if (owner != null)
          {
            Faction owningFaction = Instance.Factions.GetByMember(owner.UserIDString);
            if (owningFaction != null && Instance.Wars.AreFactionsAtWar(attacker.Faction, owningFaction))
              return ApplyDefensiveBonus(area, hit);
          }
        }

        // If the structure is in a raidable area, it can be damaged subject to the defensive bonus.
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

        // All building blocks except for twig are protected.
        if (buildingBlock != null)
          return buildingBlock.grade != BuildingGrade.Enum.Twigs;

        // Some additional entities (doors, boxes, etc.) are also protected.
        if (ProtectedPrefabs.Any(prefab => entity.ShortPrefabName.Contains(prefab)))
          return true;

        return false;
      }

      static bool IsRaidableArea(Area area)
      {
        if (!Instance.Options.Raiding.RestrictRaiding)
          return true;

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
