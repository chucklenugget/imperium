namespace Oxide.Plugins
{
  using Rust;
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Imperium
  {
    class Zone : MonoBehaviour
    {
      const string SpherePrefab = "assets/prefabs/visualization/sphere.prefab";

      List<BaseEntity> Spheres = new List<BaseEntity>();

      public ZoneType Type { get; private set; }
      public MonoBehaviour Owner { get; private set; }

      public void Init(ZoneType type, MonoBehaviour owner, string name, Vector3 position, float radius, int darkness, float? lifespan = null)
      {
        Type = type;
        Owner = owner;

        gameObject.layer = (int)Layer.Reserved1;
        gameObject.name = $"imperium_zone_{name}";
        transform.position = position;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));

        for (var idx = 0; idx < darkness; idx++)
        {
          var sphere = GameManager.server.CreateEntity(SpherePrefab, position);

          SphereEntity entity = sphere.GetComponent<SphereEntity>();
          entity.currentRadius = radius * 2;
          entity.lerpSpeed = 0f;

          sphere.Spawn();
          Spheres.Add(sphere);
        }

        var collider = gameObject.AddComponent<SphereCollider>();
        collider.radius = radius;
        collider.isTrigger = true;
        collider.enabled = true;

        if (lifespan != null)
          Invoke("DelayedDestroy", (int)lifespan);
      }

      void OnDestroy()
      {
        var collider = GetComponent<SphereCollider>();

        if (collider != null)
          Destroy(collider);

        foreach (BaseEntity sphere in Spheres)
          sphere.KillMessage();
      }

      void OnTriggerEnter(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null && !user.CurrentZones.Contains(this))
          Api.HandleUserEnteredZone(user, this);
      }

      void OnTriggerExit(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null && user.CurrentZones.Contains(this))
          Api.HandleUserLeftZone(user, this);
      }

      void DelayedDestroy()
      {
        Instance.Zones.Remove(this);
      }
    }
  }
}
