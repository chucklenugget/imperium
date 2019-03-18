namespace Oxide.Plugins
{
  using Rust;
  using System;
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Imperium
  {
    class Zone : MonoBehaviour
    {
      const string SpherePrefab = "assets/prefabs/visualization/sphere.prefab";

      List<BaseEntity> Spheres = new List<BaseEntity>();

      public ZoneType Type { get; private set; }
      public string Name { get; private set; }
      public MonoBehaviour Owner { get; private set; }
      public DateTime? EndTime { get; set; }

      public void Init(ZoneType type, string name, MonoBehaviour owner, float radius, int darkness, DateTime? endTime)
      {
        Type = type;
        Name = name;
        Owner = owner;
        EndTime = endTime;

        Vector3 position = GetGroundPosition(owner.transform.position);

        gameObject.layer = (int)Layer.Reserved1;
        gameObject.name = $"imperium_zone_{name.ToLowerInvariant()}";
        transform.position = position;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));

        for (var idx = 0; idx < darkness; idx++)
        {
          var sphere = GameManager.server.CreateEntity(SpherePrefab, position);

          SphereEntity entity = sphere.GetComponent<SphereEntity>();
          entity.lerpRadius = radius * 2;
          entity.currentRadius = radius * 2;
          entity.lerpSpeed = 0f;

          sphere.Spawn();
          Spheres.Add(sphere);
        }

        var collider = gameObject.AddComponent<SphereCollider>();
        collider.radius = radius;
        collider.isTrigger = true;
        collider.enabled = true;

        if (endTime != null)
          InvokeRepeating(nameof(CheckIfShouldDestroy), 10f, 5f);
      }

      void OnDestroy()
      {
        var collider = GetComponent<SphereCollider>();

        if (collider != null)
          Destroy(collider);

        foreach (BaseEntity sphere in Spheres)
          sphere.KillMessage();

        if (IsInvoking(nameof(CheckIfShouldDestroy)))
          CancelInvoke(nameof(CheckIfShouldDestroy));
      }

      void OnTriggerEnter(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null && !user.CurrentZones.Contains(this))
          Events.OnUserEnteredZone(user, this);
      }

      void OnTriggerExit(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null && user.CurrentZones.Contains(this))
          Events.OnUserLeftZone(user, this);
      }

      void CheckIfShouldDestroy()
      {
        if (DateTime.UtcNow >= EndTime)
          Instance.Zones.Remove(this);
      }

      Vector3 GetGroundPosition(Vector3 pos)
      {
        return new Vector3(pos.x, TerrainMeta.HeightMap.GetHeight(pos), pos.z);
      }
    }
  }
}
