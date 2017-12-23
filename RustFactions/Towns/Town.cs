namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    public class Town
    {
      public string AreaId;
      public string Name;
      public ulong MayorId;
      public uint CupboardId;

      public Town() { }

      public Town(string areaId, string name, ulong mayorId, uint cupboardId)
      {
        AreaId = areaId;
        Name = name;
        MayorId = mayorId;
        CupboardId = cupboardId;
      }

      public BuildingPrivlidge GetCupboardEntity()
      {
        return BaseNetworkable.serverEntities.Find(CupboardId) as BuildingPrivlidge;
      }
    }
  }
}
