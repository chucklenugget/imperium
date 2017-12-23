namespace Oxide.Plugins
{
  using Oxide.Game.Rust.Cui;
  using System;
  using UnityEngine;

  public partial class RustFactions
  {
    class User : MonoBehaviour
    {
      public RustFactions Plugin { get; private set; }
      public BasePlayer Player { get; private set; }
      public MapUi Map { get; private set; }

      public Area CurrentArea { get; set; }
      public Interaction PendingInteraction { get; set; }

      public void Init(RustFactions plugin, BasePlayer player)
      {
        Plugin = plugin;
        Player = player;
        Map = new MapUi(plugin, this);
      }

      void Awake()
      {
      }

      void OnDestroy()
      {
        Map.Hide();
      }
    }
  }
}
