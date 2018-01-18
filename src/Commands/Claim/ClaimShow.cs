namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimShowCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendMessage(Messages.CannotShowClaimBadUsage);
        return;
      }

      Area area = Areas.Get(NormalizeAreaId(args[0]));

      switch (area.Type)
      {
        case AreaType.Badlands:
          user.SendMessage(Messages.AreaIsBadlands, area.Id);
          return;
        case AreaType.Claimed:
          user.SendMessage(Messages.AreaIsClaimed, area.Id, area.FactionId);
          return;
        case AreaType.Headquarters:
          user.SendMessage(Messages.AreaIsHeadquarters, area.Id, area.FactionId);
          return;
        case AreaType.Town:
          user.SendMessage(Messages.AreaIsTown, area.Id, area.Name, area.FactionId);
          return;
        default:
          user.SendMessage(Messages.AreaIsWilderness, area.Id);
          return;
      }
    }
  }
}