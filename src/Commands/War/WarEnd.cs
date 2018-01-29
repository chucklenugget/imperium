namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnWarEndCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanEngageInDiplomacy(user, faction))
        return;

      Faction enemy = Factions.Get(NormalizeFactionId(args[0]));

      if (enemy == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      War war = Wars.GetActiveWarBetween(faction, enemy);

      if (war == null)
      {
        user.SendChatMessage(Messages.NotAtWar, enemy.Id);
        return;
      }

      if (war.IsOfferingPeace(faction))
      {
        user.SendChatMessage(Messages.CannotOfferPeaceAlreadyOfferedPeace, enemy.Id);
        return;
      }

      war.OfferPeace(faction);

      if (war.IsAttackerOfferingPeace && war.IsDefenderOfferingPeace)
      {
        Wars.EndWar(war, WarEndReason.Treaty);
        PrintToChat(Messages.WarEndedTreatyAcceptedAnnouncement, faction.Id, enemy.Id);
        OnDiplomacyChanged();
      }
      else
      {
        user.SendChatMessage(Messages.PeaceOffered, enemy.Id);
      }
    }
  }
}
