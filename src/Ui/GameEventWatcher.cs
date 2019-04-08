﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  public partial class Imperium
  {
    class GameEventWatcher : MonoBehaviour
    {
      const float CheckIntervalSeconds = 5f;

      HashSet<CargoPlane> CargoPlanes = new HashSet<CargoPlane>();
      HashSet<BaseHelicopter> PatrolHelicopters = new HashSet<BaseHelicopter>();
      HashSet<CH47Helicopter> ChinookHelicopters = new HashSet<CH47Helicopter>();
      HashSet<HackableLockedCrate> LockedCrates = new HashSet<HackableLockedCrate>();
      HashSet<CargoShip> CargoShips = new HashSet<CargoShip>();

      public bool IsCargoPlaneActive
      {
        get { return CargoPlanes.Count > 0; }
      }

      public bool IsHelicopterActive
      {
        get { return PatrolHelicopters.Count > 0; }
      }

      public bool IsChinookOrLockedCrateActive
      {
        get { return ChinookHelicopters.Count > 0 || LockedCrates.Count > 0; }
      }

      public bool IsCargoShipActive
      {
        get { return CargoShips.Count > 0; }
      }

      void Awake()
      {
        foreach (CargoPlane plane in FindObjectsOfType<CargoPlane>())
          BeginEvent(plane);

        foreach (BaseHelicopter heli in FindObjectsOfType<BaseHelicopter>())
          BeginEvent(heli);

        foreach (CH47Helicopter chinook in FindObjectsOfType<CH47Helicopter>())
          BeginEvent(chinook);

        foreach (HackableLockedCrate crate in FindObjectsOfType<HackableLockedCrate>().Where(IsChinookCrate))
          BeginEvent(crate);

        foreach (CargoShip ship in FindObjectsOfType<CargoShip>())
          BeginEvent(ship);

        InvokeRepeating(nameof(CheckEvents), CheckIntervalSeconds, CheckIntervalSeconds);
      }

      void OnDestroy()
      {
        CancelInvoke();
      }

      public void BeginEvent(CargoPlane plane)
      {
        Instance.Puts($"Beginning cargoplane event, plane at @ {plane.transform.position}");
        CargoPlanes.Add(plane);
      }

      public void BeginEvent(BaseHelicopter heli)
      {
        Instance.Puts($"Beginning patrol helicopter event, heli at @ {heli.transform.position}");
        PatrolHelicopters.Add(heli);
      }

      public void BeginEvent(CH47Helicopter chinook)
      {
        Instance.Puts($"Beginning chinook event, heli at @ {chinook.transform.position}");
        ChinookHelicopters.Add(chinook);
      }

      public void BeginEvent(HackableLockedCrate crate)
      {
        Instance.Puts($"Beginning locked crate event, crate at @ {crate.transform.position}");
        LockedCrates.Add(crate);
      }

      public void BeginEvent(CargoShip ship)
      {
        Instance.Puts($"Beginning cargo ship event, ship at @ {ship.transform.position}");
        CargoShips.Add(ship);
      }

      void CheckEvents()
      {
        var endedEvents = CargoPlanes.RemoveWhere(IsEntityGone)
          + PatrolHelicopters.RemoveWhere(IsEntityGone)
          + ChinookHelicopters.RemoveWhere(IsEntityGone)
          + LockedCrates.RemoveWhere(IsEntityGone)
          + CargoShips.RemoveWhere(IsEntityGone);

        if (endedEvents > 0)
          Instance.Hud.RefreshForAllPlayers();
      }

      bool IsEntityGone(BaseEntity entity)
      {
        return !entity.IsValid() || !entity.gameObject.activeInHierarchy;
      }

      bool IsChinookCrate(HackableLockedCrate crate)
      {
        BaseEntity parent = crate.GetParentEntity();
        return parent == null || !(parent is CargoShip);
      }
    }
  }
}
