namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Imperium
  {
    class GameEventWatcher : MonoBehaviour
    {
      const float CheckIntervalSeconds = 5f;

      HashSet<BaseHelicopter> Helicopters = new HashSet<BaseHelicopter>();
      HashSet<CargoPlane> CargoPlanes = new HashSet<CargoPlane>();

      public bool IsHelicopterActive
      {
        get { return Helicopters.Count > 0; }
      }

      public bool IsCargoPlaneActive
      {
        get { return CargoPlanes.Count > 0; }
      }

      void Awake()
      {
        foreach (BaseHelicopter heli in FindObjectsOfType<BaseHelicopter>())
          BeginEvent(heli);

        foreach (CargoPlane plane in FindObjectsOfType<CargoPlane>())
          BeginEvent(plane);

        InvokeRepeating("CheckEvents", CheckIntervalSeconds, CheckIntervalSeconds);
      }

      void OnDestroy()
      {
        if (IsInvoking("CheckEvents"))
          CancelInvoke("CheckEvents");
      }

      public void BeginEvent(BaseHelicopter heli)
      {
        Instance.Puts($"Beginning helicopter event, heli at @ {heli.transform.position}");
        Helicopters.Add(heli);
      }

      public void BeginEvent(CargoPlane plane)
      {
        Instance.Puts($"Beginning cargoplane event, plane at @ {plane.transform.position}");
        CargoPlanes.Add(plane);
      }

      void CheckEvents()
      {
        var endedEvents = Helicopters.RemoveWhere(IsEntityGone) + CargoPlanes.RemoveWhere(IsEntityGone);
        if (endedEvents > 0)
          Instance.Hud.RefreshForAllPlayers();
      }

      bool IsEntityGone(BaseEntity entity)
      {
        return !entity.IsValid() || !entity.gameObject.activeInHierarchy;
      }
    }
  }
}
