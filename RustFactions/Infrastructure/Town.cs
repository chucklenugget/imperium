namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class RustFactions
  {
    class Town
    {
      public string Name { get; private set; }
      public Area[] Areas { get; private set; }
      public string FactionId { get; private set; }
      public ulong MayorId { get; private set; }

      public Town(IEnumerable<Area> areas)
      {
        Areas = areas.ToArray();
        Name = Areas[0].Name;
        FactionId = Areas[0].FactionId;
        MayorId = (ulong)Areas[0].ClaimantId;
      }

      public float GetDistanceFromEntity(BaseEntity entity)
      {
        return Areas.Min(area => GetDistanceFromEntity(entity));
      }

      public int GetPopulation()
      {
        return Areas.SelectMany(area => area.ClaimCupboard.authorizedPlayers.Select(p => p.userid)).Distinct().Count();
      }
    }
  }
}
