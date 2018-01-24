namespace Oxide.Plugins
{
  public partial class Imperium
  {
    class AssigningClaimInteraction : Interaction
    {
      public Faction Faction { get; private set; }

      public AssigningClaimInteraction(Faction faction)
      {
        Faction = faction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        Area area = User.CurrentArea;
        AreaType type = (Core.Areas.GetAllClaimedByFaction(Faction).Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

        if (area == null)
        {
          Core.PrintWarning("Player attempted to assign claim but wasn't in an area. This shouldn't happen.");
          return false;
        }

        if (area.Type == AreaType.Badlands)
        {
          User.SendMessage(Messages.CannotClaimAreaIsBadlands, area.Id);
          return false;
        }

        Core.PrintToChat(Messages.AreaClaimAssignedAnnouncement, Faction.Id, area.Id);

        Core.Areas.Claim(area, type, Faction, User, cupboard);
        Core.History.Record(EventType.AreaClaimAssigned, area, Faction, User);

        return true;
      }
    }
  }
}
