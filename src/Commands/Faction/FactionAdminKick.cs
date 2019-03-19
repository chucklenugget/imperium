namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionAdminKickCommand(User user, string[] args)
    {
      if (args.Length != 2)
      {
        user.SendChatMessage(Messages.Usage, "/faction admin kick FACTION \"PLAYER\"");
        return;
      }

      Faction faction = Factions.Get(args[0]);
      User member = Users.Find(args[1]);

      if (faction == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      if (member == null)
      {
        user.SendChatMessage(Messages.InvalidUser, args[1]);
        return;
      }

      if (!faction.HasMember(member))
      {
        user.SendChatMessage(Messages.UserIsNotMemberOfFaction, member.UserName, faction.Id);
        return;
      }

      if (faction.HasLeader(member))
      {
        user.SendChatMessage(Messages.CannotKickLeaderOfFaction, member.UserName, faction.Id);
        return;
      }

      user.SendChatMessage(Messages.MemberRemoved, member.UserName, faction.Id);
      PrintToChat(Messages.FactionMemberLeftAnnouncement, member.UserName, faction.Id);

      Log($"{Util.Format(user)} forcibly kicked {Util.Format(member)} from faction {faction.Id}");

      faction.RemoveMember(member);
      member.SetFaction(null);
    }
  }
}