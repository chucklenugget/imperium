namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    public class Claim
    {
      public string AreaId;
      public string FactionId;
      public ulong ClaimantId;
      public uint CupboardId;
      public bool IsHeadquarters;

      public Claim() { }

      public Claim(string areaId, string factionId, ulong claimantId, uint cupboardId, bool isHeadquarters)
      {
        AreaId = areaId;
        FactionId = factionId;
        ClaimantId = claimantId;
        CupboardId = cupboardId;
        IsHeadquarters = isHeadquarters;
      }
    }
  }
}
