namespace Oxide.Plugins
{
  using System.Linq;
  using UnityEngine;

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
      "gates.external"
    };

    object ScaleDamageForDefensiveBonus(BaseCombatEntity entity, HitInfo hit, User attacker)
    {
      if (!ShouldAwardDefensiveBonus(entity))
        return null;

      Area area = Areas.GetByEntityPosition(entity);

      if (area == null)
      {
        PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
        return null;
      }

      Faction attackingFaction = Factions.GetByUser(attacker);
      Faction defendingFaction = Factions.GetByUser(entity.OwnerID);

      if (defendingFaction.Id != area.Id)
        return null;

      /*
      if (attackingFaction.Id == defendingFaction.Id)
        return null;
       */

      int depth = Areas.GetDepthInsideFriendlyTerritory(area);
      int index = Mathf.Clamp(depth, 0, Options.DefensiveBonuses.Count - 1);
      float reduction = Options.DefensiveBonuses[index];

      Puts($"Qualified entity {entity.net.ID} is being attacked in {area.Id}, attacker = {attackingFaction?.Id}, defender = {defendingFaction?.Id}, depth = {depth}, reduction = {reduction}");

      if (reduction >= 1)
        return false;
      else
        hit.damageTypes.ScaleAll(reduction);

      return null;
    }

    bool ShouldAwardDefensiveBonus(BaseEntity entity)
    {
      if (entity is DecayEntity)
        return true;

      if (ProtectedPrefabs.Any(prefab => entity.ShortPrefabName.Contains(prefab)))
        return true;

      return false;
    }

  }
}
