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
        NotProtected,
        Friendly,
        BeingAttacked
      }

      static string[] BlockedPrefabs = new[] {
        "fireball_small",
        "fireball_small_arrow",
        "fireball_small_shotgun",
        "fireexplosion"
      };

      static string[] ProtectedPrefabs = new[]
      {
        "barricade.concrete",
        "barricade.metal",
        "barricade.sandbags",
        "barricade.stone",
        "barricade.wood",
        "barricade.woodwire",
        "bbq",
        "bed",
        "box.wooden.large",
        "ceilinglight",
        "chair",
        "cupboard",
        "door.double.hinged",
        "door.hinged",
        "dropbox",
        "fireplace",
        "floor.frame",
        "floor.ladder.hatch",
        "fridge",
        "furnace",
        "gates.external",
        "jackolantern",
        "lantern",
        "locker",
        "mailbox",
        "planter.large",
        "planter.small",
        "reactivetarget",
        "refinery_small",
        "repairbench",
        "researchtable",
        "rug",
        "searchlight",
        "shelves",
        "shutter",
        "sign.hanging",
        "sign.huge.wood",
        "sign.large.wood",
        "sign.medium.wood",
        "sign.pictureframe",
        "sign.pole.banner.large",
        "sign.post",
        "sign.small.wood",
        "small_stash",
        "spikes.floor",
        "spinner.wheel",
        "survivalfishtrap",
        "table",
        "tunalight",
        "vendingmachine",
        "wall.external",
        "wall.frame",
        "wall.window",
        "water_barrel",
        "water_catcher",
        "water_desalinator",
        "waterbarrel",
        "waterpurifier",
        "window.bars",
        "woodbox",
        "workbench"
      };

      static string[] RaidTriggeringPrefabs = new[]
      {
        "cupboard",
        "door.double.hinged",
        "door.hinged",
        "floor.ladder.hatch",
        "floor.frame",
        "gates.external",
        "vendingmachine",
        "wall.frame",
        "wall.external",
        "wall.window",
        "window.bars"
      };

      public static object HandleDamageAgainstStructure(User attacker, BaseEntity entity, HitInfo hit)
      {
        Area area = Instance.Areas.GetByEntityPosition(entity);

        if (area == null)
        {
          Instance.PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
          return null;
        }

        DamageResult result = DetermineDamageResult(attacker, area, entity);

        if (result == DamageResult.NotProtected || result == DamageResult.Friendly)
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

          if (cupboard != null && IsRaidTriggeringEntity(entity))
          {
            float remainingHealth = entity.Health() - hit.damageTypes.Total();
            if (remainingHealth < 1)
              Instance.Zones.CreateForRaid(cupboard);
          }
        }

        return null;
      }

      static DamageResult DetermineDamageResult(User attacker, Area area, BaseEntity entity)
      {
        // Players can always damage their own entities.
        if (attacker.Player.userID == entity.OwnerID)
          return DamageResult.Friendly;

        if (!IsProtectedEntity(entity))
          return DamageResult.NotProtected;

        if (attacker.Faction != null)
        {
          // Factions can damage any structure built on their own land.
          if (area.FactionId != null && attacker.Faction.Id == area.FactionId)
            return DamageResult.Friendly;

          // Factions who are at war can damage any structure on enemy land, subject to the defensive bonus.
          if (area.FactionId != null && Instance.Wars.AreFactionsAtWar(attacker.Faction.Id, area.FactionId))
            return DamageResult.BeingAttacked;

          // Factions who are at war can damage any structure built by a member of an enemy faction, subject
          // to the defensive bonus.
          BasePlayer owner = BasePlayer.FindByID(entity.OwnerID);
          if (owner != null)
          {
            Faction owningFaction = Instance.Factions.GetByMember(owner.UserIDString);
            if (owningFaction != null && Instance.Wars.AreFactionsAtWar(attacker.Faction, owningFaction))
              return DamageResult.BeingAttacked;
          }
        }

        // If the structure is in a raidable area, it can be damaged subject to the defensive bonus.
        if (IsRaidableArea(area))
          return DamageResult.BeingAttacked;

        // Prevent the damage.
        return DamageResult.Prevent;
      }

      public static object HandleIncidentalDamage(BaseEntity entity, HitInfo hit)
      {
        if (!Instance.Options.Raiding.RestrictRaiding)
          return null;

        Area area = Instance.Areas.GetByEntityPosition(entity);

        if (area == null)
        {
          Instance.PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
          return null;
        }

        if (hit.Initiator == null)
          return null;

        // If the damage is coming from something other than a blocked prefab, allow it.
        if (!BlockedPrefabs.Contains(hit.Initiator.ShortPrefabName))
          return null;

        // If the player is in a PVP area or in PVP mode, allow the damage.
        if (IsRaidableArea(area))
          return null;

        return false;
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

      static bool IsRaidTriggeringEntity(BaseEntity entity)
      {
        var buildingBlock = entity as BuildingBlock;

        // All building blocks except for twig are protected.
        if (buildingBlock != null)
          return buildingBlock.grade != BuildingGrade.Enum.Twigs;

        // Destriction of some additional entities (doors, etc.) will also trigger raids.
        if (RaidTriggeringPrefabs.Any(prefab => entity.ShortPrefabName.Contains(prefab)))
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
          case AreaType.Wilderness:
            return Instance.Options.Raiding.AllowedInWilderness;
          default:
            throw new InvalidOperationException($"Unknown area type {area.Type}");
        }
      }
    }
  }
}
