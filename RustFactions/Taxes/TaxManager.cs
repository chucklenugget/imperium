namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class RustFactions
  {
    public class TaxManager : RustFactionsManager
    {
      Dictionary<string, TaxPolicy> TaxPolicies = new Dictionary<string, TaxPolicy>();

      public int Count
      {
        get { return TaxPolicies.Values.Count; }
      }

      public TaxManager(RustFactions plugin)
        : base(plugin)
      {
      }

      public TaxManager(RustFactions plugin, IEnumerable<TaxPolicy> policies)
        : this(plugin)
      {
        TaxPolicies = policies.ToDictionary(p => p.FactionId);
      }

      public TaxPolicy SetTaxChest(string factionId, StorageContainer container)
      {
        TaxPolicy policy = Get(factionId);

        if (policy != null)
          policy.TaxChestId = container.net.ID;
        else
          policy = TaxPolicies[factionId] = new TaxPolicy(factionId, Plugin.Options.DefaultTaxRate, container.net.ID);

        Plugin.OnTaxPoliciesChanged();
        return policy;
      }

      public TaxPolicy SetTaxRate(string factionId, int taxRate)
      {
        TaxPolicy policy = Get(factionId);

        if (policy != null)
          policy.TaxRate = taxRate;
        else
          policy = TaxPolicies[factionId] = new TaxPolicy(factionId, taxRate, null);

        Plugin.OnTaxPoliciesChanged();
        return policy;
      }

      public void Remove(TaxPolicy policy)
      {
        TaxPolicies.Remove(policy.FactionId);
        Plugin.OnTaxPoliciesChanged();
      }

      public void Remove(string factionId)
      {
        TaxPolicies.Remove(factionId);
        Plugin.OnTaxPoliciesChanged();
      }

      public void RemoveTaxChest(TaxPolicy policy)
      {
        RemoveTaxChest(policy.FactionId);
      }

      public void RemoveTaxChest(string factionId)
      {
        TaxPolicies[factionId].TaxChestId = null;
        Plugin.OnTaxPoliciesChanged();
      }

      public TaxPolicy Get(Claim claim)
      {
        return Get(claim.FactionId);
      }

      public TaxPolicy Get(Faction faction)
      {
        return Get(faction.Id);
      }

      public TaxPolicy Get(string factionId)
      {
        TaxPolicy policy;
        if (TaxPolicies.TryGetValue(factionId, out policy))
          return policy;
        else
          return null;
      }

      public TaxPolicy Get(StorageContainer container)
      {
        return TaxPolicies.Values.FirstOrDefault(p => p.TaxChestId == container.net.ID);
      }

      public TaxPolicy[] GetAllActiveTaxPolicies()
      {
        return TaxPolicies.Values.Where(p => p.TaxChestId != null).ToArray();
      }

      public TaxPolicy[] Serialize()
      {
        return TaxPolicies.Values.ToArray();
      }
    }
  }
}
