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

        if (!Core.EnsureCanChangeFactionClaims(User, SourceFaction) || !Core.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = Core.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        if (area.FactionId != SourceFaction.Id)
        {
          User.SendMessage(Messages.CannotTransferAreaNotOwnedByFaction, area.Id, TargetFaction.Id);
          return false;
        }

        if (TargetFaction.MemberSteamIds.Count < Core.Options.MinFactionMembers)
        {
          User.SendMessage(Messages.InteractionFailedFactionTooSmall, Core.Options.MinFactionMembers);
          return false;
        }

        Core.PrintToChat(Messages.AreaClaimTransferredAnnouncement, SourceFaction.Id, area.Id, TargetFaction.Id);

        AreaType type = (Core.Areas.GetAllClaimedByFaction(TargetFaction).Length == 0) ? AreaType.Headquarters : AreaType.Claimed;
        Core.Areas.Claim(area, type, TargetFaction, User, cupboard);

        Core.History.Record(EventType.AreaClaimTransferred, area, TargetFaction, User);

        return true;
      }
    }
  }
}
