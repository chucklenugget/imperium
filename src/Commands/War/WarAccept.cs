namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnWarAcceptCommand(User user, string[] args)
    {
      Faction defender = Factions.GetByMember(user);

      if (!EnsureUserAndFactionCanEngageInDiplomacy(user, defender))
        return;

      if (args.Length < 1)
      {
        user.SendChatMessage(Messages.Usage, "/war accept FACTION");
        return;
      }

      Faction attacker = Factions.Get(Util.NormalizeFactionId(args[0]));

      if (attacker == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      if (defender.Id == attacker.Id)
      {
        user.SendChatMessage(Messages.CannotDeclareWarAgainstYourself);
        return;
      }

      War war = Wars.GetWarBetween(defender, attacker);

      if (war == null)
      {
        user.SendChatMessage(Messages.CannotAcceptWarNotAtWar, attacker.Id);
        return;
      }

      Wars.AcceptWar(war);
      PrintToChat(Messages.WarAcceptedAnnouncement, war.DefenderId, war.AttackerId);
      Log($"{Util.Format(user)} accepted war from faction {war.AttackerId} on behalf of {war.DefenderId}");
    }
  }
}
