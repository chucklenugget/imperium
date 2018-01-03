namespace Oxide.Plugins
{
  using System.Linq;

  public partial class RustFactions
  {
    public static string[] ProtectedPrefabs = new[]
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
      "cupboard"
    };

    object ScaleDamageForDefensiveBonus(BaseCombatEntity entity, HitInfo hit, User attacker)
    {
      if (!ShouldAwardDefensiveBonus(entity))
        return null;

      // If someone is damaging their own entity, don't alter the damage.
      if (attacker.Id == entity.OwnerID)
        return null;

      Area area = Areas.GetByEntityPosition(entity);

      if (area == null)
      {
        PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
        return null;
      }

      if (!area.IsClaimed)
        return null;

      Faction faction = Factions.GetByUser(attacker);

      // If a member of a faction is attacking an entity within their own lands, don't alter the damage.
      if (faction != null && faction.Id == area.FactionId)
        return null;

      float reduction = area.GetDefensiveBonus();

      if (reduction >= 1)
        return false;

      hit.damageTypes.ScaleAll(reduction);
      return null;
    }

    bool ShouldAwardDefensiveBonus(BaseEntity entity)
    {
      if (entity is BuildingBlock)
        return true;

      if (ProtectedPrefabs.Any(prefab => entity.ShortPrefabName.Contains(prefab)))
        return true;

      return false;
    }

  }
}
