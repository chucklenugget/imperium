namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnWarDeclareCommand(User user, string[] args)
    {
      Faction attacker = Factions.GetByMember(user);

      if (!EnsureCanEngageInDiplomacy(user, attacker))
        return;

      if (args.Length < 2)
      {
        user.SendChatMessage(Messages.Usage, "/war declare FACTION \"REASON\"");
        return;
      }

      Faction defender = Factions.Get(Util.NormalizeFactionId(args[0]));

      if (defender == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      if (attacker.Id == defender.Id)
      {
        user.SendChatMessage(Messages.CannotDeclareWarAgainstYourself);
        return;
      }

      War existingWar = Wars.GetActiveWarBetween(attacker, defender);

      if (existingWar != null)
      {
        user.SendChatMessage(Messages.CannotDeclareWarAlreadyAtWar, defender.Id);
        return;
      }

      string cassusBelli = args[1].Trim();

      if (cassusBelli.Length < Options.War.MinCassusBelliLength)
      {
        user.SendChatMessage(Messages.CannotDeclareWarInvalidCassusBelli, defender.Id);
        return;
      }

      War war = Wars.DeclareWar(attacker, defender, user, cassusBelli);
      PrintToChat(Messages.WarDeclaredAnnouncement, war.AttackerId, war.DefenderId, war.CassusBelli);
      Log($"{Util.Format(user)} declared war on faction {war.DefenderId} on behalf of {war.AttackerId} for reason: {war.CassusBelli}");
    }
  }
}
