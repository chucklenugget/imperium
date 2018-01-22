namespace Oxide.Plugins
{
  public partial class Imperium
  {
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

      War war = Wars.GetActiveWarBetween(faction, enemy);

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
        Wars.EndWar(war, WarEndReason.Treaty);
        user.SendMessage(Messages.WarEnded, enemy.Id);
        PrintToChat(Messages.WarEndedTreatyAcceptedAnnouncement, faction.Id, enemy.Id);
        OnDiplomacyChanged();
      }
      else
      {
        user.SendMessage(Messages.PeaceOffered, enemy.Id);
      }
    }
  }
}
