namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    public class TaxPolicy
    {
      public string FactionId;
      public int TaxRate;
      public uint? TaxChestId;

      public TaxPolicy(string factionId, int taxRate, uint? taxContainerId)
      {
        FactionId = factionId;
        TaxRate = taxRate;
        TaxChestId = taxContainerId;
      }

      public bool IsActive()
      {
        return TaxChestId != null && TaxRate > 0;
      }
    }
  }
}
