﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnWarEndCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserAndFactionCanEngageInDiplomacy(user, faction))
        return;

      Faction enemy = Factions.Get(Util.NormalizeFactionId(args[0]));

      if (enemy == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      War war = Wars.GetWarBetween(faction, enemy);

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

      if (war.State == WarState.AttackerOfferingPeace || war.State == WarState.DefenderOfferingPeace)
      {
        PrintToChat(Messages.WarEndedTreatyAcceptedAnnouncement, faction.Id, enemy.Id);
        Log($"{Util.Format(user)} accepted the peace offering of {enemy.Id} on behalf of {faction.Id}");
        Wars.EndWar(war, WarEndReason.Treaty);
        OnDiplomacyChanged();
      }
      else
      {
        war.OfferPeace(faction);
        user.SendChatMessage(Messages.PeaceOffered, enemy.Id);
        Log($"{Util.Format(user)} offered peace to faction {enemy.Id} on behalf of {faction.Id}");
      }
    }
  }
}
