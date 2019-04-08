namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionAdminDisbandCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction admin disband FACTION");
        return;
      }

      Faction faction = Factions.Get(args[0]);

      if (faction == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      PrintToChat(Messages.FactionDisbandedByAdminAnnouncement, faction.Id);
      Log($"{Util.Format(user)} forcibly disbanded faction {faction.Id}");

      Factions.Disband(faction);
    }
  }
}