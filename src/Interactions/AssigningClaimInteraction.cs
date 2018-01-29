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
        AreaType type = (Instance.Areas.GetAllClaimedByFaction(Faction).Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

        if (area == null)
        {
          Instance.PrintWarning("Player attempted to assign claim but wasn't in an area. This shouldn't happen.");
          return false;
        }

        if (area.Type == AreaType.Badlands)
        {
          User.SendChatMessage(Messages.AreaIsBadlands, area.Id);
          return false;
        }

        Instance.PrintToChat(Messages.AreaClaimAssignedAnnouncement, Faction.Id, area.Id);
        Instance.Areas.Claim(area, type, Faction, User, cupboard);

        return true;
      }
    }
  }
}
