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

        Area area = Instance.Areas.GetByEntityPosition(entity, true);
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
    }
  }
}
