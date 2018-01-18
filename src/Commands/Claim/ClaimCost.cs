namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimCostCommand(User user, string[] args)
    {
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

      if (args.Length > 1)
      {
        user.SendMessage(Messages.CannotShowClaimCostBadUsage);
        return;
      }

      Area area;
      if (args.Length == 0)
        area = user.CurrentArea;
      else
        area = Areas.Get(NormalizeAreaId(args[0]));

      if (area == null)
      {
        user.SendMessage(Messages.CannotShowClaimCostBadUsage);
        return;
      }

      if (area.Type == AreaType.Badlands)
      {
        user.SendMessage(Messages.CannotClaimAreaIsBadlands, area.Id);
        return;
      }
      else if (area.Type != AreaType.Wilderness)
      {
        user.SendMessage(Messages.CannotClaimAreaIsClaimed, area.Id, area.FactionId);
        return;
      }

      int cost = area.GetClaimCost(faction);
      user.SendMessage(Messages.ClaimCost, area.Id, faction.Id, cost);
    }
  }
}