namespace Oxide.Plugins
{
  using UnityEngine;

  public partial class Imperium
  {
    class Pin
    {
      public Vector3 Position { get; }
      public string AreaId { get; }
      public string CreatorId { get; set; }
      public PinType Type { get; set; }
      public string Name { get; set; }

      public Pin(Vector3 position, Area area, User creator, PinType type, string name)
      {
        Position = position;
        AreaId = area.Id;
        CreatorId = creator.Id;
        Type = type;
        Name = name;
      }

      public Pin(PinInfo info)
      {
        Position = info.Position;
        AreaId = info.AreaId;
        CreatorId = info.CreatorId;
        Type = info.Type;
        Name = info.Name;
      }

      public float GetDistanceFrom(BaseEntity entity)
      {
        return GetDistanceFrom(entity.transform.position);
      }

      public float GetDistanceFrom(Vector3 position)
      {
        return Vector3.Distance(position, Position);
      }

      public PinInfo Serialize()
      {
        return new PinInfo {
          Position = Position,
          AreaId = AreaId,
          CreatorId = CreatorId,
          Type = Type,
          Name = Name
        };
      }
    }
  }
}
