namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnWarDeclareCommand(User user, string[] args)
    {
      Faction attacker = Factions.GetByUser(user);

      if (!EnsureCanEngageInDiplomacy(user, attacker))
        return;

      if (args.Length < 2)
      {
        user.SendMessage(Messages.CannotDeclareWarWrongUsage);
        return;
      }

      Faction defender = Factions.Get(NormalizeFactionId(args[0]));

      if (defender == null)
      {
        user.SendMessage(Messages.InteractionFailedUnknownFaction, args[0]);
        return;
      }

      if (attacker.Id == defender.Id)
      {
        user.SendMessage(Messages.CannotDeclareWarAgainstYourself);
        return;
      }

      War existingWar = Wars.GetActiveWarBetween(attacker, defender);

      if (existingWar != null)
      {
        user.SendMessage(Messages.CannotDeclareWarAlreadyAtWar, defender.Id);
        return;
      }

      string cassusBelli = args[1].Trim();

      if (cassusBelli.Length < Options.MinCassusBelliLength)
      {
        user.SendMessage(Messages.CannotDeclareWarInvalidCassusBelli, defender.Id);
        return;
      }

      War war = Wars.DeclareWar(attacker, defender, user, cassusBelli);

      user.SendMessage(Messages.WarDeclared, war.DefenderId);
      PrintToChat(Messages.WarDeclaredAnnouncement, war.AttackerId, war.DefenderId, war.CassusBelli);
    }
  }
}
