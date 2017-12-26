namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;
  using Newtonsoft.Json.Linq;
  using UnityEngine;

  public partial class RustFactions
  {
    class Faction : MonoBehaviour
    {
      RustFactions Core;

      public string Id;
      public string Description;
      public string OwnerSteamId;
      public HashSet<string> ModeratorSteamIds;
      public HashSet<string> MemberSteamIds;
      public int TaxRate;
      public StorageContainer TaxChest;

      public bool CanCollectTaxes
      {
        get { return TaxRate != 0 && TaxChest != null; }
      }

      public bool IsLeader(User user)
      {
        return (user.Player.UserIDString == OwnerSteamId) || (ModeratorSteamIds.Contains(user.Player.UserIDString));
      }

      public void Init(RustFactions core, string id, JObject data, FactionInfo info = null)
      {
        Core = core;
        Id = id;
        TaxRate = Core.Options.DefaultTaxRate;

        LoadClanData(data);

        if (info != null)
          TryLoadInfo(info);

        gameObject.name = $"RustFactions Faction ${Id}";
        gameObject.SetActive(true);
        enabled = true;
      }

      void Awake()
      {
        InvokeRepeating("CheckTaxChest", 60f, 60f);
      }

      void OnDestroy()
      {
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
          TaxChestId = TaxChest?.net.ID
        };
      }
    }
  }
}
