namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionInviteCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction invite \"PLAYER\"");
        return;
      }

      Faction faction = Factions.GetByMember(user);

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

      if (faction.HasMember(member))
      {
        user.SendChatMessage(Messages.UserIsAlreadyMemberOfFaction, member.UserName, faction.Id);
        return;
      }

      int? maxMembers = Options.Factions.MaxMembers;
      if (maxMembers != null && faction.MemberCount >= maxMembers)
      {
        user.SendChatMessage(Messages.FactionHasTooManyMembers, faction.Id, faction.MemberCount);
        return;
      }

      member.SendChatMessage(Messages.InviteReceived, user.UserName, faction.Id);
      user.SendChatMessage(Messages.InviteAdded, member.UserName, faction.Id);

      Log($"{Util.Format(user)} invited {Util.Format(member)} to faction {faction.Id}");

      faction.AddInvite(member);
    }
  }
}