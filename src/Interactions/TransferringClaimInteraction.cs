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

        if (!Instance.EnsureCanChangeFactionClaims(User, SourceFaction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
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

        if (TargetFaction.MemberIds.Count < Instance.Options.MinFactionMembers)
        {
          User.SendChatMessage(Messages.FactionTooSmall, Instance.Options.MinFactionMembers);
          return false;
        }

        Instance.PrintToChat(Messages.AreaClaimTransferredAnnouncement, SourceFaction.Id, area.Id, TargetFaction.Id);

        AreaType type = (Instance.Areas.GetAllClaimedByFaction(TargetFaction).Length == 0) ? AreaType.Headquarters : AreaType.Claimed;
        Instance.Areas.Claim(area, type, TargetFaction, User, cupboard);

        return true;
      }
    }
  }
}
