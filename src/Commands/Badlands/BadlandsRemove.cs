namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    void OnRemoveBadlandsCommand(User user, string[] args)
    {
      var areas = new List<Area>();

      foreach (string arg in args)
      {
        Area area = Areas.Get(NormalizeAreaId(arg));

        if (area == null)
        {
          user.SendMessage(Messages.CannotSetBadlandsUnknownArea, arg);
          return;
        }

        if (area.Type != AreaType.Badlands)
        {
          user.SendMessage(Messages.CannotSetBadlandsNotBadlands, area.Id);
          return;
        }

        areas.Add(area);
      }

      Areas.Unclaim(areas);

      var badlands = Areas.GetAllByType(AreaType.Badlands).Select(a => a.Id);
      user.SendMessage(Messages.BadlandsSet, FormatList(badlands));
    }
  }
}
