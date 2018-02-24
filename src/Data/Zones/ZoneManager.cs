namespace Oxide.Plugins
{
  using System;
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
        if (!Instance.Options.Zones.Enabled || Instance.Options.Zones.MonumentZones == null)
          return;

        MonumentInfo[] monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
        foreach (MonumentInfo monument in monuments)
        {
          float? radius = GetMonumentZoneRadius(monument);
          if (radius != null)
          {
            Vector3 position = monument.transform.position;
            Vector3 size = monument.Bounds.size;
            Create(ZoneType.Monument, monument.displayPhrase.english, monument, (float) radius);
          }
        }
      }

      public Zone GetByOwner(MonoBehaviour owner)
      {
        Zone zone;

        if (Zones.TryGetValue(owner, out zone))
          return zone;

        return null;
      }

      public void CreateForDebrisField(BaseHelicopter helicopter)
      {
        Vector3 position = helicopter.transform.position;
        float radius = Instance.Options.Zones.EventZoneRadius;
        Create(ZoneType.Debris, "Debris Field", helicopter, radius, GetEventEndTime());
      }

      public void CreateForSupplyDrop(SupplyDrop drop)
      {
        Vector3 position = drop.transform.position;
        float radius = Instance.Options.Zones.EventZoneRadius;
        float lifespan = Instance.Options.Zones.EventZoneLifespanSeconds;
        Create(ZoneType.SupplyDrop, "Supply Drop", drop, radius, GetEventEndTime());
      }

      public void CreateForRaid(BuildingPrivlidge cupboard)
      {
        // If the building was already being raided, just extend the lifespan of the existing zone.
        Zone existingZone = GetByOwner(cupboard);
        if (existingZone)
        {
          existingZone.EndTime = GetEventEndTime();
          Instance.Puts($"Extending raid zone end time to {existingZone.EndTime} ({existingZone.EndTime.Value.Subtract(DateTime.UtcNow).ToShortString()} from now)");
          return;
        }

        Vector3 position = cupboard.transform.position;
        float radius = Instance.Options.Zones.EventZoneRadius;

        Create(ZoneType.Raid, "Raid", cupboard, radius, GetEventEndTime());
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

      void Create(ZoneType type, string name, MonoBehaviour owner, float radius, DateTime? endTime = null)
      {
        var zone = new GameObject().AddComponent<Zone>();
        zone.Init(type, name, owner, radius, Instance.Options.Zones.DomeDarkness, endTime);

        Instance.Puts($"Created zone {zone.Name} at {zone.transform.position} with radius {radius}");

        if (endTime != null)
          Instance.Puts($"Zone {zone.Name} will be destroyed at {endTime} ({endTime.Value.Subtract(DateTime.UtcNow).ToShortString()} from now)");

        Zones.Add(owner, zone);
      }

      float? GetMonumentZoneRadius(MonumentInfo monument)
      {
        if (monument.Type == MonumentType.Cave)
          return null;

        foreach (var entry in Instance.Options.Zones.MonumentZones)
        {
          if (monument.name.Contains(entry.Key))
            return entry.Value;
        }

        return null;
      }

      DateTime GetEventEndTime()
      {
        return DateTime.UtcNow.AddSeconds(Instance.Options.Zones.EventZoneLifespanSeconds);
      }
    }
  }
}
