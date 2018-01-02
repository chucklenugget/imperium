namespace Oxide.Plugins
{
  using System;
  using System.Linq;
  using System.Text;

  public partial class RustFactions
  {
    [ChatCommand("war")]
    void OnWarCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableBadlands)
      {
        user.SendMessage(Messages.BadlandsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        OnWarHelpCommand(user);
        return;
      }

      var restArgs = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "list":
          OnWarListCommand(user);
          break;
        case "declare":
          OnWarDeclareCommand(user, restArgs);
          break;
        case "end":
          OnWarEndCommand(user, restArgs);
          break;
        default:
          OnWarHelpCommand(user);
          break;
      }
    }

    void OnWarListCommand(User user)
    {
      var sb = new StringBuilder();

      War[] wars = Diplomacy.GetAllActiveWars();
      if (wars.Length == 0)
      {
        sb.Append("The island is at peace... for now. No wars have been declared.");
      }
      else
      {
        sb.AppendLine(String.Format("<color=#ffd479>The island is at war! {0} wars have been declared:</color>", wars.Length));
        foreach (War war in wars)
          sb.AppendFormat("  <color=#ff0000>{0}</color> vs <color=#ff0000>{1}</color>: {2}", war.AttackerId, war.DefenderId, war.CassusBelli);
      }

      user.SendMessage(sb);
    }

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

      War existingWar = Diplomacy.GetActiveWarBetween(attacker, defender);

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

      War war = Diplomacy.DeclareWar(attacker, defender, user, cassusBelli);

      user.SendMessage(Messages.WarDeclared, war.DefenderId);
      PrintToChat(Messages.WarDeclaredAnnouncement, war.AttackerId, war.DefenderId, war.CassusBelli);
    }

    void OnWarEndCommand(User user, string[] args)
    {
      Faction attacker = Factions.GetByUser(user);

      if (!EnsureCanEngageInDiplomacy(user, attacker))
        return;

      Faction defender = Factions.Get(NormalizeFactionId(args[0]));

      if (defender == null)
      {
        user.SendMessage(Messages.InteractionFailedUnknownFaction, args[0]);
        return;
      }

      War war = Diplomacy.GetActiveWarBetween(attacker, defender);

      if (war == null)
      {
        user.SendMessage(Messages.CannotEndWarNotAtWar, defender.Id);
        return;
      }

      if (war.AttackerId != attacker.Id)
      {
        user.SendMessage(Messages.CannotEndWarDidNotDeclareWar, defender.Id);
        return;
      }

      Diplomacy.EndWar(war);

      user.SendMessage(Messages.WarEnded, defender.Id);
      PrintToChat(Messages.WarEndedAnnouncement, attacker.Id, defender.Id);
    }

    void OnWarHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/war list</color>: Show all existing conflicts");
      sb.AppendLine("  <color=#ffd479>/war declare FACTION \"REASON\"</color>: Declare war against another faction");
      sb.AppendLine("  <color=#ffd479>/war end FACTION</color>: Ends war with another faction");
      sb.AppendLine("  <color=#ffd479>/war help</color>: Show this message");

      user.SendMessage(sb);
    }

    bool EnsureCanEngageInDiplomacy(User user, Faction faction)
    {
      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMemberOfFaction);
        return false;
      }

      if (faction.MemberSteamIds.Count < Options.MinFactionMembers)
      {
        user.SendMessage(Messages.InteractionFailedFactionTooSmall);
        return false;
      }

      if (Areas.GetAllClaimedByFaction(faction).Length == 0)
      {
        user.SendMessage(Messages.InteractionFailedFactionDoesNotOwnLand);
        return false;
      }

      return true;
    }

  }
}
