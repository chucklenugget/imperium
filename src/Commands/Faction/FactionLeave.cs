namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionLeaveCommand(User user, string[] args)
    {
      if (args.Length != 0)
      {
        user.SendChatMessage(Messages.Usage, "/faction leave");
        return;
      }

      Faction faction = user.Faction;

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      if (faction.MemberIds.Count == 1)
      {
        PrintToChat(Messages.FactionDisbandedAnnouncement, faction.Id);
        Log($"{Util.Format(user)} disbanded faction {faction.Id} by leaving as its only member");
        Factions.Disband(faction);
        return;
      }

      user.SendChatMessage(Messages.YouLeftFaction, faction.Id);
      PrintToChat(Messages.FactionMemberLeftAnnouncement, user.UserName, faction.Id);

      Log($"{Util.Format(user)} left faction {faction.Id}");

      faction.RemoveMember(user);
      user.SetFaction(null);
    }
  }
}