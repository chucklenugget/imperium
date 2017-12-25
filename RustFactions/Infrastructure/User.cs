namespace Oxide.Plugins
{
  using UnityEngine;

  public partial class RustFactions
  {
    class User : MonoBehaviour
    {
      RustFactions Plugin;

      public BasePlayer Player { get; private set; }
      public UserMap Map { get; private set; }
      public UserLocationPanel LocationPanel { get; private set; }

      public Area CurrentArea { get; set; }
      public Interaction PendingInteraction { get; set; }

      public void Init(RustFactions plugin, BasePlayer player)
      {
        Plugin = plugin;
        Player = player;
        Map = new UserMap(plugin, this);
        LocationPanel = new UserLocationPanel(plugin, this);
      }

      void Awake()
      {
      }

      void OnDestroy()
      {
        Map.Hide();
        LocationPanel.Hide();
      }
    }
  }
}
