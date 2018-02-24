namespace Oxide.Plugins
{
  using System;
  using System.Linq;

  public partial class Imperium
  {
    static class Raiding
    {
      enum DamageResult
      {
        Prevent,
        Ignore,
        TreatAsRaid
      }

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
        "fridge",
        "woodbox_deployed",
        "mailbox.deployed",
        "dropbox.deployed",
        "box.wooden.large"
      };

      public static object HandleDamageAgainstStructure(User attacker, BaseCombatEntity entity, HitInfo hit)
      {
        Area area = Instance.Areas.GetByEntityPosition(entity);

        if (area == null)
        {
          Instance.PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
          return null;
        }

        DamageResult result = DetermineDamageResult(attacker, area, entity);

        if (result == DamageResult.Ignore)
          return null;

        if (result == DamageResult.Prevent)
          return false;

        float reduction = area.GetDefensiveBonus();

        if (reduction >= 1)
          return false;

        if (reduction > 0)
          hit.damageTypes.ScaleAll(reduction);

        if (Instance.Options.Zones.Enabled)
        {
          BuildingPrivlidge cupboard = entity.GetBuildingPrivilege();

          if (cupboard != null)
          {
            float remainingHealth = entity.Health() - hit.damageTypes.Total();
            if (remainingHealth < entity.MaxHealth() * 0.95)
              Instance.Zones.CreateForRaid(cupboard);
          }
        }

        return null;
      }

      static DamageResult DetermineDamageResult(User attacker, Area area, BaseCombatEntity entity)
      {
        // Players can always damage their own entities.
        if (attacker.Player.userID == entity.OwnerID)
          return DamageResult.Ignore;

        if (!IsProtectedEntity(entity))
          return DamageResult.Ignore;

        if (attacker.Faction != null)
        {
          // Factions can damage any structure built on their own land.
          if (area.FactionId != null && attacker.Faction.Id == area.FactionId)
            return DamageResult.Ignore;

          // Factions who are at war can damage any structure on enemy land, subject to the defensive bonus.
          if (area.FactionId != null && Instance.Wars.AreFactionsAtWar(attacker.Faction.Id, area.FactionId))
            return DamageResult.TreatAsRaid;

          // Factions who are at war can damage any structure built by a member of an enemy faction, subject
          // to the defensive bonus.
          BasePlayer owner = BasePlayer.FindByID(entity.OwnerID);
          if (owner != null)
          {
            Faction owningFaction = Instance.Factions.GetByMember(owner.UserIDString);
            if (owningFaction != null && Instance.Wars.AreFactionsAtWar(attacker.Faction, owningFaction))
              return DamageResult.TreatAsRaid;
          }
        }

        // If the structure is in a raidable area, it can be damaged subject to the defensive bonus.
        if (IsRaidableArea(area))
          return DamageResult.TreatAsRaid;

        // Prevent the damage.
        return DamageResult.Prevent;
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
