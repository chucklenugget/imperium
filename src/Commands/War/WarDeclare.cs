namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnWarDeclareCommand(User user, string[] args)
    {
      Faction attacker = Factions.GetByMember(user);

      if (!EnsureUserAndFactionCanEngageInDiplomacy(user, attacker))
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

      War existingWar = Wars.GetWarBetween(attacker, defender);

      if (existingWar != null)
      {
        user.SendChatMessage(Messages.CannotDeclareWarAlreadyAtWar, defender.Id);
        return;
      }

      if (Options.War.DiplomacyHours > 0)
      {
        War war = Wars.DeclareWar(attacker, defender, user, false);
        PrintToChat(Messages.WarDeclaredWithDiplomacyTimerAnnouncement, war.AttackerId, war.DefenderId, Options.War.DiplomacyHours);
        Log($"{Util.Format(user)} declared war on faction {war.DefenderId} on behalf of {war.AttackerId} ({Options.War.DiplomacyHours}h wait)");
      }
      else
      {
        War war = Wars.DeclareWar(attacker, defender, user, true);
        PrintToChat(Messages.WarDeclaredAnnouncement, war.AttackerId, war.DefenderId);
        Log($"{Util.Format(user)} declared war on faction {war.DefenderId} on behalf of {war.AttackerId} (no diplomacy timer)");
      }
    }
  }
}
