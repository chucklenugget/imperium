namespace Oxide.Plugins
{
  using Rust;
  using System.Collections.Generic;
  using UnityEngine;

  public partial class RustFactions
  {
    public class Area : MonoBehaviour
    {
      public RustFactions Plugin { get; private set; }
      public string Id { get; private set; }
      public Vector3 Location { get; private set; }
      public Vector3 Size { get; private set; }
      public HashSet<BasePlayer> Players { get; private set; }

      public Area()
      {
        Players = new HashSet<BasePlayer>();
      }

      public void Setup(RustFactions plugin, string id, Vector3 location, Vector3 size)
      {
        Plugin = plugin;
        Id = id;
        Location = location;
        Size = size;

        gameObject.layer = (int)Layer.Reserved1;
        gameObject.name = $"RustFactions Area {id}";
        transform.position = location;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));

        var collider = gameObject.AddComponent<BoxCollider>();
        collider.size = Size;
        collider.isTrigger = true;
        collider.enabled = true;

        gameObject.SetActive(true);
        enabled = true;
      }

      void OnTriggerEnter(Collider collider)
      {
        var player = collider.GetComponentInParent<BasePlayer>();
        if (player != null && Players.Add(player))
          Plugin.OnPlayerEnterArea(this, player);
      }

      void OnTriggerExit(Collider collider)
      {
        var player = collider.GetComponentInParent<BasePlayer>();
        if (player != null && Players.Remove(player))
          Plugin.OnPlayerExitArea(this, player);
      }
    }
  }
}
