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

        if (area == null)
        {
          User.SendChatMessage(Messages.YouAreInTheGreatUnknown);
          return false;
        }

        if (area.Type == AreaType.Badlands)
        {
          User.SendChatMessage(Messages.AreaIsBadlands, area.Id);
          return false;
        }

        Area[] ownedAreas = Instance.Areas.GetAllClaimedByFaction(Faction);
        AreaType type = (ownedAreas.Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

        Instance.PrintToChat(Messages.AreaClaimAssignedAnnouncement, Faction.Id, area.Id);
        Instance.Log($"{Util.Format(User)} assigned {area.Id} to {Faction.Id}");

        Instance.Areas.Claim(area, type, Faction, User, cupboard);
        return true;
      }
    }
  }
}
