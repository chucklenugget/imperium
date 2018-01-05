namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Newtonsoft.Json.Linq;
  using UnityEngine;

  public partial class RustFactions
  {
    class Faction : MonoBehaviour
    {
      RustFactions Core;

      public string Id { get; private set; }
      public string Description { get; private set; }
      public string OwnerSteamId { get; private set; }
      public HashSet<string> ModeratorSteamIds { get; private set; }
      public HashSet<string> MemberSteamIds { get; private set; }

      public float TaxRate { get; set; }
      public StorageContainer TaxChest { get; set; }
      public DateTime NextUpkeepPaymentTime { get; set; }

      public bool CanCollectTaxes
      {
        get { return TaxRate != 0 && TaxChest != null; }
      }

      public bool IsUpkeepPaid
      {
        get { return DateTime.UtcNow < NextUpkeepPaymentTime; }
      }

      public bool IsLeader(User user)
      {
        return (user.Player.UserIDString == OwnerSteamId) || (ModeratorSteamIds.Contains(user.Player.UserIDString));
      }

      public int GetUpkeepPerPeriod()
      {
        var costs = Core.Options.UpkeepCosts;

        int totalCost = 0;
        for (var num = 0; num < Core.Areas.GetAllClaimedByFaction(this).Length; num++)
        {
          var index = Mathf.Clamp(num, 0, costs.Count - 1);
          totalCost += costs[index];
        }

        return totalCost;
      }

      public void Init(RustFactions core, string id, JObject data, FactionInfo info = null)
      {
        Core = core;
        Id = id;
        TaxRate = Core.Options.DefaultTaxRate;
        NextUpkeepPaymentTime = DateTime.UtcNow.AddHours(Core.Options.UpkeepCollectionPeriodHours);

        LoadClanData(data);

        if (info != null)
          TryLoadInfo(info);

        InvokeRepeating("CheckTaxChest", 60f, 60f);
      }

      void OnDestroy()
      {
        if (IsInvoking("CheckTaxChest"))
          CancelInvoke("CheckTaxChest");
      }

      public void LoadClanData(JObject data)
      {
        Description = data["description"].ToString();
        OwnerSteamId = data["owner"].ToString();
        ModeratorSteamIds = new HashSet<string>(data["moderators"].Select(token => token.ToString()));
        MemberSteamIds = new HashSet<string>(data["members"].Select(token => token.ToString()));
      }

      void TryLoadInfo(FactionInfo info)
      {
        if (info.TaxChestId != null)
        {
          var taxChest = BaseNetworkable.serverEntities.Find((uint)info.TaxChestId) as StorageContainer;

          if (taxChest == null || taxChest.IsDestroyed)
            Core.PrintWarning($"Couldn't find entity {info.TaxChestId} for faction {Id}'s tax chest. Ignoring.");
          else
            TaxChest = taxChest;
        }

        TaxRate = info.TaxRate;
        NextUpkeepPaymentTime = info.NextUpkeepPaymentTime;
      }

      void CheckTaxChest()
      {
        if (TaxChest == null || !TaxChest.IsDestroyed)
          return;

        Core.PrintWarning($"Tax chest entity {TaxChest.net.ID} was destroyed. Removing from faction.");
        TaxChest = null;
      }

      public FactionInfo Serialize()
      {
        return new FactionInfo {
          FactionId = Id,
          TaxRate = TaxRate,
          TaxChestId = TaxChest?.net.ID,
          NextUpkeepPaymentTime = NextUpkeepPaymentTime
        };
      }
    }
  }
}
