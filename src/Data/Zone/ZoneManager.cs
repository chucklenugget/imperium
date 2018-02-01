namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  public partial class Imperium
  {
    class ZoneManager
    {
      Dictionary<MonoBehaviour, Zone> Zones = new Dictionary<MonoBehaviour, Zone>();

      public void Init()
      {
        MonumentInfo[] monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
        foreach (MonumentInfo monument in monuments.Where(IsDangerousMonument))
          Create(monument);

        SupplyDrop[] drops = UnityEngine.Object.FindObjectsOfType<SupplyDrop>();
        foreach (SupplyDrop drop in drops)
          Create(drop);
      }

      public Zone Create(MonumentInfo monument)
      {
        Vector3 position = monument.transform.position;
        float radius = monument.Bounds.size.x / 2;
        return Create(monument, GetMonumentZoneName(monument), position, radius);
      }

      public Zone Create(SupplyDrop drop)
      {
        Vector3 position = GetGroundPosition(drop.transform.position);
        float radius = Instance.Options.EventZoneRadius;
        float lifespan = Instance.Options.EventZoneLifespanSeconds;
        return Create(drop, drop.ShortPrefabName, position, radius, lifespan);
      }

      public Zone Create(BaseHelicopter helicopter)
      {
        Vector3 position = GetGroundPosition(helicopter.transform.position);
        float radius = Instance.Options.EventZoneRadius;
        float lifespan = Instance.Options.EventZoneLifespanSeconds;
        return Create(helicopter, helicopter.ShortPrefabName, position, radius, lifespan);
      }

      public void Remove(Zone zone)
      {
        Instance.Puts($"Destroying zone {zone.name}");
        UnityEngine.Object.Destroy(zone);
        Zones.Remove(zone.Owner);
      }

      public void Destroy()
      {
        Zone[] zones = UnityEngine.Object.FindObjectsOfType<Zone>();

        if (zones != null)
        {
          Instance.Puts($"Destroying {zones.Length} zone objects...");
          foreach (Zone zone in zones)
            UnityEngine.Object.DestroyImmediate(zone);
        }

        Zones.Clear();

        Instance.Puts("Zone objects destroyed.");
      }

      Zone Create(MonoBehaviour owner, string name, Vector3 position, float radius, float? lifespan = null)
      {
        var zone = new GameObject().AddComponent<Zone>();
        zone.Init(owner, name, position, radius, Instance.Options.ZoneDomeDarkness, lifespan);

        Instance.Puts($"Created zone {zone.name} at {position} with radius {radius}");

        if (lifespan != null)
          Instance.Puts($"Zone {zone.name} will be destroyed in {lifespan} seconds");

        Zones[owner] = zone;

        return zone;
      }

      bool IsDangerousMonument(MonumentInfo monument)
      {
        if (monument.Type == MonumentType.Cave)
          return false;

        if (Instance.Options.DangerousMonuments == null)
          return false;

        return Instance.Options.DangerousMonuments.Any(pattern => monument.name.Contains(pattern));
      }

      string GetMonumentZoneName(MonumentInfo monument)
      {
        int begin = monument.name.LastIndexOf('/');
        int end = monument.name.LastIndexOf('.');
        return monument.name.Substring(begin + 1, end - begin - 1);
      }

      Vector3 GetGroundPosition(Vector3 pos)
      {
        return new Vector3(pos.x, TerrainMeta.HeightMap.GetHeight(pos), pos.z);
      }
    }
  }
}
