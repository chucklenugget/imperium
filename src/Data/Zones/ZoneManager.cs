namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Imperium
  {
    class ZoneManager
    {
      Dictionary<MonoBehaviour, Zone> Zones = new Dictionary<MonoBehaviour, Zone>();

      public void Init()
      {
        if (Instance.Options.EnableMonumentZones && Instance.Options.MonumentZones != null)
        {
          MonumentInfo[] monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
          foreach (MonumentInfo monument in monuments)
          {
            float? radius = GetMonumentZoneRadius(monument);
            if (radius != null)
              Create(monument, (float)radius);
          }
        }

        if (Instance.Options.EnableEventZones)
        {
          SupplyDrop[] drops = UnityEngine.Object.FindObjectsOfType<SupplyDrop>();
          foreach (SupplyDrop drop in drops)
            Create(drop);
        }
      }

      public Zone Create(MonumentInfo monument, float radius)
      {
        Vector3 position = monument.transform.position;
        Vector3 size = monument.Bounds.size;
        return Create(ZoneType.Monument, monument.displayPhrase.english, monument, position, radius);
      }

      public Zone Create(SupplyDrop drop)
      {
        Vector3 position = GetGroundPosition(drop.transform.position);
        float radius = Instance.Options.EventZoneRadius;
        float lifespan = Instance.Options.EventZoneLifespanSeconds;
        return Create(ZoneType.SupplyDrop, "Supply Drop", drop, position, radius, lifespan);
      }

      public Zone Create(BaseHelicopter helicopter)
      {
        Vector3 position = GetGroundPosition(helicopter.transform.position);
        float radius = Instance.Options.EventZoneRadius;
        float lifespan = Instance.Options.EventZoneLifespanSeconds;
        return Create(ZoneType.Debris, "Debris Field", helicopter, position, radius, lifespan);
      }

      public void Remove(Zone zone)
      {
        Instance.Puts($"Destroying zone {zone.name}");

        foreach (User user in Instance.Users.GetAll())
          user.CurrentZones.Remove(zone);

        Zones.Remove(zone.Owner);

        UnityEngine.Object.Destroy(zone);
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

      Zone Create(ZoneType type, string name, MonoBehaviour owner, Vector3 position, float radius, float? lifespan = null)
      {
        var zone = new GameObject().AddComponent<Zone>();
        zone.Init(type, name, owner, position, radius, Instance.Options.ZoneDomeDarkness, lifespan);

        Instance.Puts($"Created zone {zone.Name} at {position} with radius {radius}");

        if (lifespan != null)
          Instance.Puts($"Zone {zone.Name} will be destroyed in {lifespan} seconds");

        Zones[owner] = zone;

        return zone;
      }

      float? GetMonumentZoneRadius(MonumentInfo monument)
      {
        if (monument.Type == MonumentType.Cave)
          return null;

        foreach (var entry in Instance.Options.MonumentZones)
        {
          if (monument.name.Contains(entry.Key))
            return entry.Value;
        }

        return null;
      }

      Vector3 GetGroundPosition(Vector3 pos)
      {
        return new Vector3(pos.x, TerrainMeta.HeightMap.GetHeight(pos), pos.z);
      }
    }
  }
}
