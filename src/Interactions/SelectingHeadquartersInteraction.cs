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

        if (!Instance.EnsureCanChangeFactionClaims(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);
        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        Instance.Areas.SetHeadquarters(area, Faction);
        Instance.PrintToChat(Messages.HeadquartersChangedAnnouncement, Faction.Id, area.Id);

        return true;
      }
    }
  }
}
