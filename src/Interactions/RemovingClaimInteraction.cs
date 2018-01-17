namespace Oxide.Plugins
{
  public partial class Imperium
  {
    class RemovingClaimInteraction : Interaction
    {
      public Faction Faction { get; private set; }

      public RemovingClaimInteraction(Faction faction)
      {
        Faction = faction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        if (!Core.EnsureCanChangeFactionClaims(User, Faction) || !Core.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = Core.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        User.SendMessage(Messages.ClaimRemoved, area.Id);
        Core.PrintToChat(Messages.AreaClaimRemovedAnnouncement, Faction.Id, area.Id);
        Core.Areas.Unclaim(area);
        Core.History.Record(EventType.AreaClaimRemoved, area, Faction, User);

        Area newHeadquarters = Core.Areas.SelectNewHeadquartersIfNecessary(Faction);
        if (newHeadquarters != null)
          Core.PrintToChat(Messages.HeadquartersChangedAnnouncement, Faction.Id, newHeadquarters.Id);

        return true;
      }
    }
  }
}
