namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionDemoteCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction demote \"PLAYER\"");
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

      if (faction.HasOwner(member))
      {
        user.SendChatMessage(Messages.CannotPromoteOrDemoteOwnerOfFaction, member.Name, faction.Id);
        return;
      }

      if (!faction.HasManager(member))
      {
        user.SendChatMessage(Messages.UserIsNotManagerOfFaction, member.Name, faction.Id);
        return;
      }

      user.SendChatMessage(Messages.ManagerRemoved, member.Name, faction.Id);
      Log($"{Util.Format(user)} demoted {member} in faction {faction.Id}");

      faction.Demote(member);
    }
  }
}