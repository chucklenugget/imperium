namespace Oxide.Plugins
{
  public partial class Imperium
  {
    class SelectingHeadquartersInteraction : Interaction
    {
      public Faction Faction { get; private set; }

      public SelectingHeadquartersInteraction(Faction faction)
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

        Core.Areas.SetHeadquarters(area, Faction);

        User.SendMessage(Messages.HeadquartersMoved, area.Id);
        Core.PrintToChat(Messages.HeadquartersChangedAnnouncement, Faction.Id, area.Id);
        Core.History.Record(EventType.HeadquartersMoved, area, Faction, User);

        return true;
      }
    }
  }
}
