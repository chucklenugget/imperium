namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionKickCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction kick \"PLAYER\"");
        return;
      }

      Faction faction = user.Faction;

      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return;
      }

      User member = Users.Find(args[0]);

      if (member == null)
      {
        user.SendChatMessage(Messages.InvalidUser, args[0]);
        return;
      }

      if (faction.HasLeader(member))
      {
        user.SendChatMessage(Messages.CannotKickLeaderOfFaction, member.Name, faction.Id);
        return;
      }

      faction.RemoveMember(member);
      member.SetFaction(null);

      user.SendChatMessage(Messages.MemberRemoved, member.Name, faction.Id);
      PrintToChat(Messages.FactionMemberLeftAnnouncement, member.Name, faction.Id);
    }
  }
}