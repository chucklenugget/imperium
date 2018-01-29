namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  public partial class Imperium
  {
    class FactionEntityMonitor : MonoBehaviour
    {
      void Awake()
      {
        InvokeRepeating("CheckTaxChests", 60f, 60f);
      }

      void OnDestroy()
      {
        if (IsInvoking("CheckTaxChests")) CancelInvoke("CheckTaxChests");
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

        Instance.PrintWarning($"Tax chest entity for faction {faction.Id} was destroyed. Removing from faction.");
        faction.TaxChest = null;
      }
    }
  }
}
