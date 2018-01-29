namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
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

    object ScaleDamageForDecay(BaseEntity entity, HitInfo hit)
    {
      Area area = Areas.GetByEntityPosition(entity);
      float reduction = 0;

      if (area.Type == AreaType.Claimed || area.Type == AreaType.Headquarters)
        reduction = Options.ClaimedLandDecayReduction;

      if (area.Type == AreaType.Town)
        reduction = Options.TownDecayReduction;

      if (reduction >= 1)
        return false;

      if (reduction > 0)
      {
        Puts($"Reducing decay on entity {entity.net.ID} by {reduction*100}%");
        hit.damageTypes.Scale(Rust.DamageType.Decay, reduction);
      }

      return null;
    }

    object ScaleDamageForDefensiveBonus(BaseEntity entity, HitInfo hit, User attacker)
    {
      if (!ShouldAwardDefensiveBonus(entity))
        return null;

      // If someone is damaging their own entity, don't alter the damage.
      if (attacker.Player.userID == entity.OwnerID)
        return null;

      Area area = Areas.GetByEntityPosition(entity);

      if (area == null)
      {
        PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
        return null;
      }

      // If the area isn't owned by a faction, it conveys no defensive bonuses.
      if (!area.IsClaimed)
        return null;

      Faction faction = Factions.GetByMember(attacker);

      // If a member of a faction is attacking an entity within their own lands, don't alter the damage.
      if (faction != null && faction.Id == area.FactionId)
        return null;

      // Structures cannot be damaged, except during war.
      if (!area.IsWarzone)
        return false;

      float reduction = area.GetDefensiveBonus();

      if (reduction >= 1)
        return false;

      if (reduction > 0)
        hit.damageTypes.ScaleAll(reduction);

      return null;
    }

    bool ShouldAwardDefensiveBonus(BaseEntity entity)
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
