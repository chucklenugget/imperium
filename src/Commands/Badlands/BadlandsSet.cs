namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    void OnSetBadlandsCommand(User user, string[] args)
    {
      var areas = new List<Area>();

      foreach (string arg in args)
      {
        Area area = Areas.Get(Util.NormalizeAreaId(arg));

        if (area == null)
        {
          user.SendChatMessage(Messages.UnknownArea, arg);
          return;
        }

        if (area.Type != AreaType.Wilderness)
        {
          user.SendChatMessage(Messages.AreaNotWilderness, area.Id);
          return;
        }

        areas.Add(area);
      }

      Areas.Unclaim(Areas.GetAllByType(AreaType.Badlands));
      Areas.AddBadlands(areas);

      user.SendChatMessage(Messages.BadlandsSet, Util.Format(Areas.GetAllByType(AreaType.Badlands)));
      Log($"{Util.Format(user)} set badlands to {Util.Format(areas)}");
    }
  }
}
