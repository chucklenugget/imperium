namespace Oxide.Plugins
{
  using System.Collections.Generic;

  public partial class Imperium
  {
    void OnClaimDeleteCommand(User user, string[] args)
    {
      if (args.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/claim delete XY [XY XY...]");
        return;
      }

      if (!user.HasPermission(PERM_CHANGE_CLAIMS))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

      var areas = new List<Area>();
      foreach (string arg in args)
      {
        Area area = Areas.Get(NormalizeAreaId(arg));

        if (area.Type == AreaType.Badlands)
        {
          user.SendChatMessage(Messages.AreaIsBadlands, area.Id);
          return;
        }

        if (area.Type == AreaType.Wilderness)
        {
          user.SendChatMessage(Messages.AreaIsWilderness, area.Id);
          return;
        }

        areas.Add(area);
      }

      foreach (Area area in areas)
        PrintToChat(Messages.AreaClaimDeletedAnnouncement, area.FactionId, area.Id);

      Areas.Unclaim(areas);
    }
  }
}