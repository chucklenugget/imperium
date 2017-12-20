namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class RustFactions
  {
    public class ClaimCollection : RustFactionsComponent
    {
      Dictionary<string, Claim> Claims = new Dictionary<string, Claim>();

      public int Count
      {
        get { return Claims.Values.Count; }
      }

      public ClaimCollection(RustFactions plugin)
        : base(plugin)
      {
      }

      public ClaimCollection(RustFactions plugin, IEnumerable<Claim> claims)
        : this(plugin)
      {
        Claims = claims.ToDictionary(c => c.AreaId);
      }

      public void Add(Claim claim)
      {
        Claims[claim.AreaId] = claim;
        Plugin.OnClaimsChanged();
      }

      public void Remove(Claim claim)
      {
        Remove(claim.AreaId);
      }

      public void Remove(string areaId)
      {
        Claims.Remove(areaId);
        Plugin.OnClaimsChanged();
      }

      public void Remove(IEnumerable<Claim> claims)
      {
        foreach (var claim in claims)
          Claims.Remove(claim.AreaId);
        Plugin.OnClaimsChanged();
      }

      public void SetHeadquarters(Faction faction, Claim headquartersClaim)
      {
        foreach (var claim in GetAllClaimsForFaction(faction.Id))
          claim.IsHeadquarters = false;

        headquartersClaim.IsHeadquarters = true;
        Plugin.OnClaimsChanged();
      }

      public Claim Get(Area area)
      {
        return Get(area.Id);
      }

      public Claim Get(string areaId)
      {
        Claim claim;
        if (Claims.TryGetValue(areaId, out claim))
          return claim;
        else
          return null;
      }

      public Claim GetByCupboard(BuildingPrivlidge cupboard)
      {
        return GetByCupboard(cupboard.net.ID);
      }

      public Claim GetByCupboard(uint cupboardId)
      {
        return Claims.Values.FirstOrDefault(c => c.CupboardId == cupboardId);
      }

      public Claim[] GetAllClaimsForFaction(Faction faction)
      {
        return GetAllClaimsForFaction(faction.Id);
      }

      public Claim[] GetAllClaimsForFaction(string factionId)
      {
        return Claims.Values.Where(c => c.FactionId == factionId).ToArray();
      }

      public Claim[] Serialize()
      {
        return Claims.Values.ToArray();
      }
    }
  }
}
