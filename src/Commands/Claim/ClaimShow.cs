namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimShowCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/claim show XY");
        return;
      }

      Area area = Areas.Get(Util.NormalizeAreaId(args[0]));

      switch (area.Type)
      {
        case AreaType.Badlands:
          user.SendChatMessage(Messages.AreaIsBadlands, area.Id);
          return;
        case AreaType.Claimed:
          user.SendChatMessage(Messages.AreaIsClaimed, area.Id, area.FactionId);
          return;
        case AreaType.Headquarters:
          user.SendChatMessage(Messages.AreaIsHeadquarters, area.Id, area.FactionId);
          return;
        case AreaType.Town:
          user.SendChatMessage(Messages.AreaIsTown, area.Id, area.Name, area.FactionId);
          return;
        default:
          user.SendChatMessage(Messages.AreaIsWilderness, area.Id);
          return;
      }
    }
  }
}