namespace Oxide.Plugins
{
  using UnityEngine;

  public partial class Imperium
  {
    class FactionEntityMonitor : MonoBehaviour
    {
      void Awake()
      {
        InvokeRepeating(nameof(EnsureAllTaxChestsStillExist), 60f, 60f);
      }

      void OnDestroy()
      {
        if (IsInvoking(nameof(EnsureAllTaxChestsStillExist))) CancelInvoke(nameof(EnsureAllTaxChestsStillExist));
      }

      void EnsureAllTaxChestsStillExist()
      {
        foreach (Faction faction in Instance.Factions.GetAll())
          EnsureTaxChestExists(faction);
      }

      void EnsureTaxChestExists(Faction faction)
      {
        if (faction.TaxChest == null || !faction.TaxChest.IsDestroyed)
          return;

        Instance.Log($"{faction.Id}'s tax chest was destroyed (periodic check)");
        faction.TaxChest = null;
      }
    }
  }
}
