namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionDisbandCommand(User user, string[] args)
    {
      if (args.Length != 1 || args[0].ToLowerInvariant() != "forever")
      {
        user.SendChatMessage(Messages.Usage, "/faction disband forever");
        return;
      }

      Faction faction = user.Faction;

      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return;
      }

      PrintToChat(Messages.FactionDisbandedAnnouncement, faction.Id);
      Log($"{Util.Format(user)} disbanded faction {faction.Id}");

      Factions.Disband(faction);
    }
  }
}