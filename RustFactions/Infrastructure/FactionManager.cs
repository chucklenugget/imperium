namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json.Linq;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  public partial class RustFactions
  {
    class FactionManager : RustFactionsManager
    {
      Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();

      public FactionManager(RustFactions core)
        : base(core)
      {
      }

      public Faction[] GetAll()
      {
        return Factions.Values.ToArray();
      }

      public Faction Get(string id)
      {
        Faction faction;
        if (Factions.TryGetValue(id, out faction))
          return faction;
        else
          return null;
      }

      public Faction GetByUser(User user)
      {
        return GetByUser(user.Id);
      }

      public Faction GetByUser(ulong userId)
      {
        var factionId = Core.Clans.Call<string>("GetClanOf", userId);

        if (String.IsNullOrEmpty(factionId))
          return null;

        return Get(factionId);
      }

      public Faction GetByTaxChest(StorageContainer container)
      {
        return GetByTaxChest(container.net.ID);
      }

      public Faction GetByTaxChest(uint containerId)
      {
        return Factions.Values.SingleOrDefault(f => f.TaxChest != null && f.TaxChest.net.ID == containerId);
      }

      public void SetTaxRate(Faction faction, int taxRate)
      {
        faction.TaxRate = taxRate;
        Core.OnFactionsChanged();
      }

      public void SetTaxChest(Faction faction, StorageContainer taxChest)
      {
        faction.TaxChest = taxChest;
        Core.OnFactionsChanged();
      }

      public void Init(FactionInfo[] factionInfos)
      {
        var clanIds = Core.Clans.Call<JArray>("GetAllClans");

        Dictionary<string, FactionInfo> lookup;
        if (factionInfos != null)
          lookup = factionInfos.ToDictionary(f => f.FactionId);
        else
          lookup = new Dictionary<string, FactionInfo>();

        Puts($"Creating faction objects for {clanIds.Count} factions...");

        foreach (var clanId in clanIds)
        {
          var id = clanId.ToString();
          var data = Core.Clans.Call<JObject>("GetClan", id);
          Faction faction = new GameObject().AddComponent<Faction>();

          FactionInfo info;
          lookup.TryGetValue(id, out info);

          faction.Init(Core, id, data, info);
          Factions.Add(faction.Id, faction);
        }

        Puts("Faction objects created.");
      }

      public void HandleFactionCreated(string factionId)
      {
        var data = Core.Clans.Call<JObject>("GetClan", factionId);

        Faction faction = new GameObject().AddComponent<Faction>();
        faction.Init(Core, factionId, data);

        Factions.Add(faction.Id, faction);
      }

      public void HandleFactionChanged(string factionId)
      {
        var data = Core.Clans.Call<JObject>("GetClan", factionId);

        Faction faction;
        if (!Factions.TryGetValue(factionId, out faction))
        {
          faction = new GameObject().AddComponent<Faction>();
          faction.Init(Core, factionId, data);
          Factions.Add(factionId, faction);
        }
        else
        {
          faction.LoadClanData(data);
        }
      }

      public void HandleFactionDestroyed(string factionId)
      {
        Faction faction;

        if (Factions.TryGetValue(factionId, out faction))
          UnityEngine.Object.Destroy(faction);
        
        Factions.Remove(factionId);
      }

      public void Destroy()
      {
        var factionObjects = Resources.FindObjectsOfTypeAll<Faction>();
        Puts($"Destroying {factionObjects.Length} faction objects...");

        foreach (var faction in factionObjects)
          UnityEngine.Object.Destroy(faction);

        Factions.Clear();
        Puts("Faction objects destroyed.");
      }

      public FactionInfo[] SerializeState()
      {
        return Factions.Values.Select(faction => faction.Serialize()).ToArray();
      }
    }
  }
}