namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    public class Town
    {
      public string AreaId;
      public string Name;
      public string FactionId;
      public ulong MayorId;
      public uint CupboardId;

      public Town() { }

      public Town(string areaId, string name, string factionId, ulong mayorId, uint cupboardId)
      {
        AreaId = areaId;
        Name = name;
        FactionId = factionId;
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
