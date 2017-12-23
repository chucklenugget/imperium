namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class RustFactions
  {
    public class BadlandsManager : RustFactionsManager
    {
      HashSet<string> AreaIds = new HashSet<string>();

      public int Count
      {
        get { return AreaIds.Count; }
      }

      public BadlandsManager(RustFactions plugin)
        : base(plugin)
      {
      }

      public BadlandsManager(RustFactions plugin, IEnumerable<string> areaIds)
        : this(plugin)
      {
        AreaIds = new HashSet<string>(areaIds);
      }

      public void Add(string areaId)
      {
        AreaIds.Add(areaId);
        Plugin.OnBadlandsChanged();
      }

      public void Add(IEnumerable<string> areaIds)
      {
        foreach (var areaId in areaIds)
          AreaIds.Add(areaId);
        Plugin.OnBadlandsChanged();
      }

      public void Remove(string areaId)
      {
        AreaIds.Remove(areaId);
        Plugin.OnBadlandsChanged();
      }

      public void Remove(IEnumerable<string> areaIds)
      {
        foreach (var areaId in areaIds)
          AreaIds.Remove(areaId);
        Plugin.OnBadlandsChanged();
      }

      public void Set(IEnumerable<string> areaIds)
      {
        AreaIds.Clear();
        Add(areaIds);
      }

      public IEnumerable<string> GetAll()
      {
        return AreaIds;
      }

      public bool Contains(Area area)
      {
        return Contains(area.Id);
      }

      public bool Contains(string areaId)
      {
        return AreaIds.Contains(areaId);
      }

      public string[] Serialize()
      {
        return AreaIds.ToArray();
      }
    }
  }
}
