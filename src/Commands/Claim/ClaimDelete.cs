namespace Oxide.Plugins
{
  using System.Collections.Generic;

  public partial class Imperium
  {
    void OnClaimDeleteCommand(User user, string[] args)
    {
      if (args.Length == 0)
      {
        user.SendMessage(Messages.CannotDeleteClaimsBadUsage);
        return;
      }

      if (!user.HasPermission(PERM_CHANGE_CLAIMS))
      {
        user.SendMessage(Messages.CannotDeleteClaimsNoPermission);
        return;
      }

      var areas = new List<Area>();
      foreach (string arg in args)
      {
        Area area = Areas.Get(NormalizeAreaId(arg));

        if (area.Type == AreaType.Badlands)
        {
          user.SendMessage(Messages.CannotDeleteClaimsAreaIsBadlands, area.Id);
          return;
        }

        if (area.Type == AreaType.Wilderness)
        {
          user.SendMessage(Messages.CannotDeleteClaimsAreaNotUnclaimed, area.Id);
          return;
        }

        areas.Add(area);
      }

      foreach (Area area in areas)
      {
        PrintToChat(Messages.AreaClaimDeletedAnnouncement, area.FactionId, area.Id);
        History.Record(EventType.AreaClaimDeleted, area, null, user);
      }

      Areas.Unclaim(areas);
    }
  }
}