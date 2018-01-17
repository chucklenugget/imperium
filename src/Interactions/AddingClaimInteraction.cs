namespace Oxide.Plugins
{
  using System.Collections.Generic;

  public partial class Imperium
  {
    class AddingClaimInteraction : Interaction
    {
      public Faction Faction { get; private set; }

      public AddingClaimInteraction(Faction faction)
      {
        Faction = faction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        if (!Core.EnsureCanChangeFactionClaims(User, Faction) || !Core.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = User.CurrentArea;
        AreaType type = (Core.Areas.GetAllClaimedByFaction(Faction).Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

        if (area == null)
        {
          Core.PrintWarning("Player attempted to add claim but wasn't in an area. This shouldn't happen.");
          return false;
        }

        if (area.Type == AreaType.Badlands)
        {
          User.SendMessage(Messages.CannotClaimAreaIsBadlands, area.Id);
          return false;
        }

        if (area.Type == AreaType.Town)
        {
          User.SendMessage(Messages.CannotClaimAreaIsTown, area.Id, area.FactionId);
          return false;
        }

        if (area.Type == AreaType.Wilderness)
        {
          int cost = area.GetClaimCost(Faction);

          if (cost > 0)
          {
            ItemDefinition scrapDef = ItemManager.FindItemDefinition("scrap");
            List<Item> stacks = User.Player.inventory.FindItemIDs(scrapDef.itemid);

            if (!Core.TryCollectFromStacks(scrapDef, stacks, cost))
            {
              User.SendMessage(Messages.CannotClaimAreaCannotAfford, cost);
              return false;
            }
          }

          User.SendMessage(Messages.ClaimAdded, area.Id);

          if (type == AreaType.Headquarters)
            Core.PrintToChat(Messages.AreaClaimedAsHeadquartersAnnouncement, Faction.Id, area.Id);
          else
            Core.PrintToChat(Messages.AreaClaimedAnnouncement, Faction.Id, area.Id);

          Core.Areas.Claim(area, type, Faction, User, cupboard);
          Core.History.Record(EventType.AreaClaimed, area, Faction, User);

          return true;
        }

        if (area.FactionId == Faction.Id)
        {
          if (area.ClaimCupboard.net.ID == cupboard.net.ID)
          {
            User.SendMessage(Messages.CannotClaimAreaAlreadyOwned, area.Id);
            return false;
          }
          else
          {
            // If the same faction claims a new cupboard within the same area, move the claim to the new cupboard.
            User.SendMessage(Messages.ClaimCupboardMoved, area.Id);
            area.ClaimantId = User.Id;
            area.ClaimCupboard = cupboard;
            return true;
          }
        }

        if (area.FactionId != Faction.Id)
        {
          if (area.ClaimCupboard.net.ID != cupboard.net.ID)
          {
            // A new faction can't make a claim on a new cabinet within an area that is already claimed by another faction.
            User.SendMessage(Messages.CannotClaimAreaIsClaimed, area.Id, area.FactionId);
            return false;
          }

          string previousFactionId = area.FactionId;

          // If a new faction claims the claim cabinet for an area, they take control of that area.
          User.SendMessage(Messages.ClaimCaptured, area.Id, area.FactionId);
          Core.PrintToChat(Messages.AreaCapturedAnnouncement, Faction.Id, area.Id, area.FactionId);
          Core.Areas.Claim(area, type, Faction, User, cupboard);
          Core.History.Record(EventType.AreaCaptured, area, Faction, User);

          return true;
        }

        Core.PrintWarning("Area was in an unknown state during completion of AddingClaimInteraction. This shouldn't happen.");
        return false;
      }
    }
  }
}
