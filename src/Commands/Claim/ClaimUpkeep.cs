namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnClaimUpkeepCommand(User user)
    {
      if (!Options.Upkeep.Enabled)
      {
        user.SendChatMessage(Messages.UpkeepDisabled);
        return;
      }

      Faction faction = Factions.GetByMember(user);

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      if (faction.MemberIds.Count < Options.Claims.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmall, Options.Claims.MinFactionMembers);
        return;
      }

      Area[] areas = Areas.GetAllClaimedByFaction(faction);

      if (areas.Length == 0)
      {
        user.SendChatMessage(Messages.NoAreasClaimed);
        return;
      }

      int upkeep = faction.GetUpkeepPerPeriod();
      var nextPaymentHours = (int)faction.NextUpkeepPaymentTime.Subtract(DateTime.UtcNow).TotalHours;

      if (nextPaymentHours > 0)
        user.SendChatMessage(Messages.UpkeepCost, upkeep, areas.Length, faction.Id, nextPaymentHours);
      else
        user.SendChatMessage(Messages.UpkeepCostOverdue, upkeep, areas.Length, faction.Id, nextPaymentHours);
    }
  }
}