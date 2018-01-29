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
        Factions.Disband(faction);
        PrintToChat(Messages.FactionDisbandedAnnouncement, faction.Id);
        return;
      }

      faction.RemoveMember(user);
      user.SetFaction(null);

      user.SendChatMessage(Messages.YouLeftFaction, faction.Id);
      PrintToChat(Messages.FactionMemberLeftAnnouncement, user.Name, faction.Id);
    }
  }
}