namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  public partial class Imperium
  {
    class AreaManager
    {
      Dictionary<string, Area> Areas;
      Area[,] Layout;

      public MapGrid MapGrid { get; }

      public int Count
      {
        get { return Areas.Count; }
      }

      public AreaManager()
      {
        MapGrid = new MapGrid();
        Areas = new Dictionary<string, Area>();
        Layout = new Area[MapGrid.NumberOfCells, MapGrid.NumberOfCells];
      }
      
      public Area Get(string areaId)
      {
        Area area;
        if (Areas.TryGetValue(areaId, out area))
          return area;
        else
          return null;
      }

      public Area Get(int row, int col)
      {
        return Layout[row, col];
      }

      public Area[] GetAll()
      {
        return Areas.Values.ToArray();
      }

      public Area[] GetAllByType(AreaType type)
      {
        return Areas.Values.Where(a => a.Type == type).ToArray();
      }

      public Area[] GetAllClaimedByFaction(Faction faction)
      {
        return GetAllClaimedByFaction(faction.Id);
      }

      public Area[] GetAllClaimedByFaction(string factionId)
      {
        return Areas.Values.Where(a => a.FactionId == factionId).ToArray();
      }

      public Area[] GetAllTaxableClaimsByFaction(Faction faction)
      {
        return GetAllTaxableClaimsByFaction(faction.Id);
      }

      public Area[] GetAllTaxableClaimsByFaction(string factionId)
      {
        return Areas.Values.Where(a => a.FactionId == factionId && a.IsTaxableClaim).ToArray();
      }

      public Area GetByClaimCupboard(BuildingPrivlidge cupboard)
      {
        return GetByClaimCupboard(cupboard.net.ID);
      }

      public Area GetByClaimCupboard(uint cupboardId)
      {
        return Areas.Values.FirstOrDefault(a => a.ClaimCupboard != null && a.ClaimCupboard.net.ID == cupboardId);
      }

      public Town GetTown(string name)
      {
        Area[] areas = GetAllByType(AreaType.Town).Where(area => area.Name == name).ToArray();
        if (areas.Length == 0)
          return null;
        else
          return new Town(areas);
      }

      public Town[] GetAllTowns()
      {
        return GetAllByType(AreaType.Town).GroupBy(a => a.Name).Select(group => new Town(group)).ToArray();
      }

      public Town GetTownByMayor(User user)
      {
        return GetAllTowns().FirstOrDefault(town => town.MayorId == user.Id);
      }

      public Area GetByEntityPosition(BaseEntity entity)
      {
        Vector3 position = entity.transform.position;

        int row = (int)(MapGrid.MapSize / 2 - position.z) / MapGrid.CellSize;
        int col = (int)(position.x + MapGrid.MapSize / 2) / MapGrid.CellSize;

        return Layout[row, col];
      }

      public void Claim(Area area, AreaType type, Faction faction, User claimant, BuildingPrivlidge cupboard)
      {
        area.Type = type;
        area.FactionId = faction.Id;
        area.ClaimantId = claimant.Id;
        area.ClaimCupboard = cupboard;

        Api.HandleAreaChanged(area);
      }

      public void SetHeadquarters(Area area, Faction faction)
      {
        // Ensure that no other areas are considered headquarters.
        foreach (Area otherArea in GetAllClaimedByFaction(faction).Where(a => a.Type == AreaType.Headquarters))
        {
          otherArea.Type = AreaType.Claimed;
          Api.HandleAreaChanged(otherArea);
        }

        area.Type = AreaType.Headquarters;
        Api.HandleAreaChanged(area);
      }

      public void AddToTown(string name, User mayor, Area area)
      {
        area.Type = AreaType.Town;
        area.Name = name;
        area.ClaimantId = mayor.Id;

        Api.HandleAreaChanged(area);
      }

      public void RemoveFromTown(Area area)
      {
        area.Type = AreaType.Claimed;
        area.Name = null;

        Api.HandleAreaChanged(area);
      }

      public void Unclaim(IEnumerable<Area> areas)
      {
        Unclaim(areas.ToArray());
      }

      public void Unclaim(params Area[] areas)
      {
        foreach (Area area in areas)
        {
          area.Type = AreaType.Wilderness;
          area.FactionId = null;
          area.ClaimantId = null;
          area.ClaimCupboard = null;

          Api.HandleAreaChanged(area);
        }
      }

      public void AddBadlands(params Area[] areas)
      {
        foreach (Area area in areas)
        {
          area.Type = AreaType.Badlands;
          area.FactionId = null;
          area.ClaimantId = null;
          area.ClaimCupboard = null;

          Api.HandleAreaChanged(area);
        }
      }

      public void AddBadlands(IEnumerable<Area> areas)
      {
        AddBadlands(areas.ToArray());
      }

      public int GetNumberOfContiguousClaimedAreas(Area area, Faction owner)
      {
        int count = 0;

        // North
        if (area.Row > 0 && Layout[area.Row - 1, area.Col].FactionId == owner.Id)
          count++;

        // South
        if (area.Row < MapGrid.NumberOfCells - 1 && Layout[area.Row + 1, area.Col].FactionId == owner.Id)
          count++;

        // West
        if (area.Col > 0 && Layout[area.Row, area.Col - 1].FactionId == owner.Id)
          count++;

        // East
        if (area.Col < MapGrid.NumberOfCells - 1 && Layout[area.Row, area.Col + 1].FactionId == owner.Id)
          count++;

        return count;
      }

      public int GetDepthInsideFriendlyTerritory(Area area)
      {
        if (!area.IsClaimed)
          return 0;

        var depth = new int[4];

        // North
        for (var row = area.Row; row >= 0; row--)
        {
          if (Layout[row, area.Col].FactionId != area.FactionId)
            break;

          depth[0]++;
        }

        // South
        for (var row = area.Row; row < MapGrid.NumberOfCells; row++)
        {
          if (Layout[row, area.Col].FactionId != area.FactionId)
            break;

          depth[1]++;
        }

        // West
        for (var col = area.Col; col >= 0; col--)
        {
          if (Layout[area.Row, col].FactionId != area.FactionId)
            break;

          depth[2]++;
        }

        // East
        for (var col = area.Col; col < MapGrid.NumberOfCells; col++)
        {
          if (Layout[area.Row, col].FactionId != area.FactionId)
            break;

          depth[3]++;
        }

        return depth.Min() - 1;
      }

      public void Init(IEnumerable<AreaInfo> areaInfos)
      {
        Instance.Puts("Creating area objects...");

        Dictionary<string, AreaInfo> lookup;
        if (areaInfos != null)
          lookup = areaInfos.ToDictionary(a => a.Id);
        else
          lookup = new Dictionary<string, AreaInfo>();

        for (var row = 0; row < MapGrid.NumberOfCells; row++)
        {
          for (var col = 0; col < MapGrid.NumberOfCells; col++)
          {
            string areaId = MapGrid.GetAreaId(row, col);
            Vector3 position = MapGrid.GetPosition(row, col);
            Vector3 size = new Vector3(MapGrid.CellSize, 500, MapGrid.CellSize);

            AreaInfo info = null;
            lookup.TryGetValue(areaId, out info);

            var area = new GameObject().AddComponent<Area>();
            area.Init(areaId, row, col, position, size, info);

            Areas[areaId] = area;
            Layout[row, col] = area;
          }
        }

        Instance.Puts($"Created {Areas.Values.Count} area objects.");
      }

      public void Destroy()
      {
        Area[] areas = UnityEngine.Object.FindObjectsOfType<Area>();

        if (areas != null)
        {
          Instance.Puts($"Destroying {areas.Length} area objects...");
          foreach (Area area in areas)
            UnityEngine.Object.Destroy(area);
        }

        Areas.Clear();
        Array.Clear(Layout, 0, Layout.Length);

        Instance.Puts("Area objects destroyed.");
      }

      public AreaInfo[] Serialize()
      {
        return Areas.Values.Select(area => area.Serialize()).ToArray();
      }
    }
  }
}
