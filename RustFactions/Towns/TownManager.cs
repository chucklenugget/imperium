namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class RustFactions
  {
    public class TownManager : RustFactionsManager
    {
      Dictionary<string, Town> Towns = new Dictionary<string, Town>();

      public int Count
      {
        get { return Towns.Values.Count; }
      }

      public TownManager(RustFactions plugin)
        : base(plugin)
      {
      }

      public void Load(IEnumerable<Town> towns)
      {
        Towns = towns.ToDictionary(c => c.AreaId);
      }

      public void Add(Town town)
      {
        Towns[town.AreaId] = town;
        Plugin.OnTownsChanged();
      }

      public void Remove(Town town)
      {
        Remove(town.AreaId);
      }

      public void Remove(string areaId)
      {
        Towns.Remove(areaId);
        Plugin.OnTownsChanged();
      }

      public bool Contains(Area area)
      {
        return Contains(area.Id);
      }

      public bool Contains(string areaId)
      {
        return Towns.ContainsKey(areaId);
      }

      public Town Get(Area area)
      {
        return Get(area.Id);
      }

      public Town Get(string areaId)
      {
        Town town;
        if (Towns.TryGetValue(areaId, out town))
          return town;
        else
          return null;
      }

      public Town GetByCupboard(BuildingPrivlidge cupboard)
      {
        return GetByCupboard(cupboard.net.ID);
      }

      public Town GetByCupboard(uint cupboardId)
      {
        return Towns.Values.FirstOrDefault(c => c.CupboardId == cupboardId);
      }

      public Town GetByMayor(BasePlayer player)
      {
        return GetByMayor(player.userID);
      }

      public Town GetByMayor(ulong playerId)
      {
        return Towns.Values.FirstOrDefault(t => t.MayorId == playerId);
      }

      public Town[] GetAll()
      {
        return Towns.Values.ToArray();
      }

      public Town[] Serialize()
      {
        return Towns.Values.ToArray();
      }
    }
  }
}
