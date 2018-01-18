namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnClaimUpkeepCommand(User user)
    {
      if (!Options.EnableUpkeep)
      {
        user.SendMessage(Messages.UpkeepDisabled);
        return;
      }

      Faction faction = Factions.GetByUser(user);

      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMemberOfFaction);
        return;
      }

      if (faction.MemberSteamIds.Count < Options.MinFactionMembers)
      {
        user.SendMessage(Messages.InteractionFailedFactionTooSmall, Options.MinFactionMembers);
        return;
      }

      Area[] areas = Areas.GetAllClaimedByFaction(faction);

      if (areas.Length == 0)
      {
        user.SendMessage(Messages.NoAreasClaimed);
        return;
      }

      int upkeep = faction.GetUpkeepPerPeriod();
      var nextPaymentHours = (int)faction.NextUpkeepPaymentTime.Subtract(DateTime.UtcNow).TotalHours;

      if (nextPaymentHours > 0)
        user.SendMessage(Messages.UpkeepCost, upkeep, areas.Length, faction.Id, nextPaymentHours);
      else
        user.SendMessage(Messages.UpkeepCostOverdue, upkeep, areas.Length, faction.Id, nextPaymentHours);
    }
  }
}