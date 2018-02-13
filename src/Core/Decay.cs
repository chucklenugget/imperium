namespace Oxide.Plugins
{
  public partial class Imperium
  {
    static class Decay
    {
      public static object AlterDecayDamage(BaseEntity entity, HitInfo hit)
      {
        if (!Instance.Options.Decay.Enabled)
          return null;

        Area area = GetAreaForDecayCalculation(entity);

        if (area == null)
        {
          Instance.PrintWarning($"An entity decayed in an unknown area. This shouldn't happen.");
          return null;
        }

        float reduction = 0;

        if (area.Type == AreaType.Claimed || area.Type == AreaType.Headquarters)
          reduction = Instance.Options.Decay.ClaimedLandDecayReduction;

        if (area.Type == AreaType.Town)
          reduction = Instance.Options.Decay.TownDecayReduction;

        if (reduction >= 1)
          return false;

        if (reduction > 0)
          hit.damageTypes.Scale(Rust.DamageType.Decay, reduction);

        return null;
      }

      static Area GetAreaForDecayCalculation(BaseEntity entity)
      {
        Area area = null;

        // If the entity is controlled by a claim cupboard, use the area the cupboard controls.
        BuildingPrivlidge cupboard = entity.GetBuildingPrivilege();
        if (cupboard)
          area = Instance.Areas.GetByClaimCupboard(cupboard);

        // Otherwise, determine the area by its position in the world.
        if (area == null)
          area = Instance.Areas.GetByEntityPosition(entity);

        return area;
      }
    }
  }
}
