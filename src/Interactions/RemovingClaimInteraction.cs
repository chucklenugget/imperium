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

        if (!Instance.EnsureCanChangeFactionClaims(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        User.SendChatMessage(Messages.ClaimRemoved, area.Id);
        Instance.PrintToChat(Messages.AreaClaimRemovedAnnouncement, Faction.Id, area.Id);
        Instance.Areas.Unclaim(area);

        return true;
      }
    }
  }
}
