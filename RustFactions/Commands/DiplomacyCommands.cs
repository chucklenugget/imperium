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
        case "status":
          OnWarStatusCommand(user);
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
        for (var idx = 0; idx < wars.Length; idx++)
        {
          War war = wars[idx];
          sb.AppendFormat("{0}. <color=#ffd479>{1}</color> vs <color=#ffd479>{2}</color>: {2}", (idx + 1), war.AttackerId, war.DefenderId, war.CassusBelli);
          sb.AppendLine();
        }
      }

      user.SendMessage(sb);
    }

    void OnWarStatusCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMemberOfFaction);
        return;
      }

      var sb = new StringBuilder();
      War[] wars = Diplomacy.GetAllActiveWarsByFaction(faction);

      if (wars.Length == 0)
      {
        sb.AppendLine("Your faction is not involved in any wars.");
      }
      else
      {
        sb.AppendLine(String.Format("<color=#ffd479>Your faction is involved in {0} wars:</color>", wars.Length));
        for (var idx = 0; idx < wars.Length; idx++)
        {
          War war = wars[idx];
          sb.AppendFormat("{0}. <color=#ffd479>{1}</color> vs <color=#ffd479>{2}</color>", (idx + 1), war.AttackerId, war.DefenderId);
          if (war.IsAttackerOfferingPeace) sb.AppendFormat(": <color=#ffd479>{0}</color> is offering peace!", war.AttackerId);
          if (war.IsDefenderOfferingPeace) sb.AppendFormat(": <color=#ffd479>{0}</color> is offering peace!", war.DefenderId);
          sb.AppendLine();
        }
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

      if (attacker.Id == defender.Id)
      {
        user.SendMessage(Messages.CannotDeclareWarAgainstYourself);
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
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanEngageInDiplomacy(user, faction))
        return;

      Faction enemy = Factions.Get(NormalizeFactionId(args[0]));

      if (enemy == null)
      {
        user.SendMessage(Messages.InteractionFailedUnknownFaction, args[0]);
        return;
      }

      War war = Diplomacy.GetActiveWarBetween(faction, enemy);

      if (war == null)
      {
        user.SendMessage(Messages.InteractionFailedNotAtWar, enemy.Id);
        return;
      }

      if (war.IsOfferingPeace(faction))
      {
        user.SendMessage(Messages.CannotOfferPeaceAlreadyOfferedPeace, enemy.Id);
        return;
      }

      war.OfferPeace(faction);

      if (war.IsAttackerOfferingPeace && war.IsDefenderOfferingPeace)
      {
        Diplomacy.EndWar(war, WarEndReason.Treaty);
        user.SendMessage(Messages.WarEnded, enemy.Id);
        PrintToChat(Messages.WarEndedTreatyAcceptedAnnouncement, faction.Id, enemy.Id);
        OnDiplomacyChanged();
      }
      else
      {
        user.SendMessage(Messages.PeaceOffered, enemy.Id);
      }
    }

    void OnWarHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/war list</color>: Show all active wars");
      sb.AppendLine("  <color=#ffd479>/war status</color>: Show all active wars your faction is involved in");
      sb.AppendLine("  <color=#ffd479>/war declare FACTION \"REASON\"</color>: Declare war against another faction");
      sb.AppendLine("  <color=#ffd479>/war end FACTION</color>: Offer to end a war, or accept an offer made to you");
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
