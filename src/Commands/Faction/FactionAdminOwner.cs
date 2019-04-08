namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionAdminOwnerCommand(User user, string[] args)
    {
      if (args.Length != 2)
      {
        user.SendChatMessage(Messages.Usage, "/faction admin owner FACTION \"PLAYER\"");
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
        user.SendChatMessage(Messages.UserIsAlreadyOwnerOfFaction, member.UserName, faction.Id);
        return;
      }

      user.SendChatMessage(Messages.FactionOwnerChanged, faction.Id, member.UserName);
      Log($"{Util.Format(user)} forcibly set {Util.Format(member)} as the owner of faction {faction.Id}");

      faction.SetOwner(member);
    }
  }
}