namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimCostCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      if (faction.MemberIds.Count < Options.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmall, Options.MinFactionMembers);
        return;
      }

      if (args.Length > 1)
      {
        user.SendChatMessage(Messages.Usage, "/claim cost [XY]");
        return;
      }

      Area area;
      if (args.Length == 0)
        area = user.CurrentArea;
      else
        area = Areas.Get(NormalizeAreaId(args[0]));

      if (area == null)
      {
        user.SendChatMessage(Messages.Usage, "/claim cost [XY]");
        return;
      }

      if (area.Type == AreaType.Badlands)
      {
        user.SendChatMessage(Messages.AreaIsBadlands, area.Id);
        return;
      }
      else if (area.Type != AreaType.Wilderness)
      {
        user.SendChatMessage(Messages.CannotClaimAreaAlreadyClaimed, area.Id, area.FactionId);
        return;
      }

      int cost = area.GetClaimCost(faction);
      user.SendChatMessage(Messages.ClaimCost, area.Id, faction.Id, cost);
    }
  }
}