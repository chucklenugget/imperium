namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionAdminPromoteCommand(User user, string[] args)
    {
      if (args.Length != 2)
      {
        user.SendChatMessage(Messages.Usage, "/faction admin promote FACTION \"PLAYER\"");
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

      if (faction.HasOwner(member))
      {
        user.SendChatMessage(Messages.CannotPromoteOrDemoteOwnerOfFaction, member.UserName, faction.Id);
        return;
      }

      if (faction.HasManager(member))
      {
        user.SendChatMessage(Messages.UserIsAlreadyManagerOfFaction, member.UserName, faction.Id);
        return;
      }

      user.SendChatMessage(Messages.ManagerAdded, member.UserName, faction.Id);
      Log($"{Util.Format(user)} forcibly promoted {Util.Format(member)} in faction {faction.Id}");

      faction.Promote(member);
    }
  }
}