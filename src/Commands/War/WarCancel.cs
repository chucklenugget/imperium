namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnWarCancelCommand(User user, string[] args)
    {
      if (!user.HasPermission(Permission.AdminWars))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

      if (args.Length < 2)
      {
        user.SendChatMessage(Messages.Usage, "/war cancel FACTION1 FACTION2");
        return;
      }

      Faction faction1 = Factions.Get(Util.NormalizeFactionId(args[0]));
      Faction faction2 = Factions.Get(Util.NormalizeFactionId(args[1]));

      if (faction1 == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      if (faction2 == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[1]);
        return;
      }

      War war = Wars.GetActiveWarBetween(faction1, faction2);

      if (war == null)
      {
        user.SendChatMessage(Messages.FactionsAreNotAtWar, faction1.Id, faction2.Id);
        return;
      }

      Wars.EndWar(war, WarEndReason.CanceledByAdmin);
      PrintToChat(Messages.WarEndedByAdminAnnouncement, war.AttackerId, war.DefenderId);
      Log($"{Util.Format(user)} canceled the war between {war.DefenderId} and {war.AttackerId}");
    }
  }
}
