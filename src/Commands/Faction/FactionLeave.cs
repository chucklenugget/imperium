namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionLeaveCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction leave FACTION");
        return;
      }

      Faction faction = user.Faction;

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      faction.RemoveMember(user);
      user.SetFaction(null);

      user.SendChatMessage(Messages.YouLeftFaction, faction.Id);
      PrintToChat(Messages.FactionMemberLeftAnnouncement, user.Name, faction.Id);
    }
  }
}