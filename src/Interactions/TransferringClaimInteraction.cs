namespace Oxide.Plugins
{
  public partial class Imperium
  {
    class TransferringClaimInteraction : Interaction
    {
      public Faction SourceFaction { get; }
      public Faction TargetFaction { get; }

      public TransferringClaimInteraction(Faction sourceFaction, Faction targetFaction)
      {
        SourceFaction = sourceFaction;
        TargetFaction = targetFaction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        if (!Instance.EnsureUserCanChangeFactionClaims(User, SourceFaction) || !Instance.EnsureCupboardCanBeUsedForClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        if (area.FactionId != SourceFaction.Id)
        {
          User.SendChatMessage(Messages.AreaNotOwnedByYourFaction, area.Id);
          return false;
        }

        if (!Instance.EnsureFactionCanClaimArea(User, TargetFaction, area))
          return false;

        Area[] claimedAreas = Instance.Areas.GetAllClaimedByFaction(TargetFaction);
        AreaType type = (claimedAreas.Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

        Instance.PrintToChat(Messages.AreaClaimTransferredAnnouncement, SourceFaction.Id, area.Id, TargetFaction.Id);
        Instance.Log($"{Util.Format(User)} transferred {SourceFaction.Id}'s claim on {area.Id} to {TargetFaction.Id}");

        Instance.Areas.Claim(area, type, TargetFaction, User, cupboard);

        return true;
      }
    }
  }
}
