namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  /*
   * The (X, Z) coordinate system works like this (on a map of size 3000):
   *
   * (-3000, 3000) +-------+ (3000, 3000)
   *               |       |
   *               |   +--------- (0,0)
   *               |       |
   * (-3000, 3000) +-------+ (3000, -3000)
   *
   * No matter the map size, grid cells are always 150 x 150.
   */

  public partial class RustFactions
  {
    class AreaManager : RustFactionsManager
    {
      Dictionary<string, Area> Areas = new Dictionary<string, Area>();

      const int GRID_SIZE = 150;

      public int Count
      {
        get { return Areas.Count; }
      }

      public AreaManager(RustFactions plugin)
        : base(plugin)
      {
      }
      
      public Area Get(string areaId)
      {
        Area area;
        if (Areas.TryGetValue(areaId, out area))
          return area;
        else
          return null;
      }

      public void Init()
      {
        var worldSize = ConVar.Server.worldsize;
        var offset = worldSize / 2;

        var prefix = "";
        char letter = 'A';

        Puts("Creating area objects...");

        for (var z = (offset - GRID_SIZE); z > -(offset + GRID_SIZE); z -= GRID_SIZE)
        {
          var number = 0;
          for (var x = -offset; x < offset; x += GRID_SIZE)
          {
            var area = new GameObject().AddComponent<Area>();

            var areaId = $"{prefix}{letter}{number}";
            var location = new Vector3(x + (GRID_SIZE / 2), 0, z + (GRID_SIZE / 2));
            var size = new Vector3(GRID_SIZE, 500, GRID_SIZE); // TODO: Chose an arbitrary height. Is something else better?

            area.Init(Plugin, areaId, location, size);

            Areas[areaId] = area;

            number++;
          }

          if (letter == 'Z')
          {
            letter = 'A';
            prefix = "A";
          }
          else
          {
            letter++;
          }
        }

        Puts($"Created {Areas.Values.Count} area objects.");
      }

      public void Destroy()
      {
        var areaObjects = Resources.FindObjectsOfTypeAll<Area>();
        Puts($"Destroying {areaObjects.Length} area objects...");

        foreach (var area in areaObjects)
        {
          var collider = area.GetComponent<BoxCollider>();
          if (collider != null)
            UnityEngine.Object.Destroy(collider);
          UnityEngine.Object.Destroy(area);
        }

        Areas.Clear();
        Puts("Area objects destroyed.");
      }
    }
  }
}
