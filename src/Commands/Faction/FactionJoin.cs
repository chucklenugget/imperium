namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionJoinCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction join FACTION");
        return;
      }

      if (user.Faction != null)
      {
        user.SendChatMessage(Messages.AlreadyMemberOfFaction);
        return;
      }

      Faction faction = Factions.Get(args[0]);

      if (faction == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      if (!faction.HasInvite(user))
      {
        user.SendChatMessage(Messages.CannotJoinFactionNotInvited, faction.Id);
        return;
      }

      user.SendChatMessage(Messages.YouJoinedFaction, faction.Id);
      PrintToChat(Messages.FactionMemberJoinedAnnouncement, user.UserName, faction.Id);
      Log($"{Util.Format(user)} joined faction {faction.Id}");

      faction.AddMember(user);
      user.SetFaction(faction);

    }
  }
}