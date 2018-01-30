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

        if (!Instance.EnsureCanChangeFactionClaims(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = User.CurrentArea;
        AreaType type = (Instance.Areas.GetAllClaimedByFaction(Faction).Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

        if (area == null)
        {
          Instance.PrintWarning("Player attempted to add claim but wasn't in an area. This shouldn't happen.");
          return false;
        }

        if (area.Type == AreaType.Badlands)
        {
          User.SendChatMessage(Messages.AreaIsBadlands, area.Id);
          return false;
        }

        if (area.Type == AreaType.Town)
        {
          User.SendChatMessage(Messages.AreaIsTown, area.Id, area.Name, area.FactionId);
          return false;
        }

        if (area.Type == AreaType.Wilderness)
        {
          int cost = area.GetClaimCost(Faction);

          if (cost > 0)
          {
            ItemDefinition scrapDef = ItemManager.FindItemDefinition("scrap");
            List<Item> stacks = User.Player.inventory.FindItemIDs(scrapDef.itemid);

            if (!Instance.TryCollectFromStacks(scrapDef, stacks, cost))
            {
              User.SendChatMessage(Messages.CannotClaimAreaCannotAfford, cost);
              return false;
            }
          }

          User.SendChatMessage(Messages.ClaimAdded, area.Id);

          if (type == AreaType.Headquarters)
            Instance.PrintToChat(Messages.AreaClaimedAsHeadquartersAnnouncement, Faction.Id, area.Id);
          else
            Instance.PrintToChat(Messages.AreaClaimedAnnouncement, Faction.Id, area.Id);

          Instance.Log($"{Util.Format(User)} claimed {area.Id} on behalf of {Faction.Id}");
          Instance.Areas.Claim(area, type, Faction, User, cupboard);
          return true;
        }

        if (area.FactionId == Faction.Id)
        {
          if (area.ClaimCupboard.net.ID == cupboard.net.ID)
          {
            User.SendChatMessage(Messages.CannotClaimAreaAlreadyOwned, area.Id);
            return false;
          }
          else
          {
            // If the same faction claims a new cupboard within the same area, move the claim to the new cupboard.
            User.SendChatMessage(Messages.ClaimCupboardMoved, area.Id);
            Instance.Log($"{Util.Format(User)} moved {area.FactionId}'s claim on {area.Id} from cupboard {Util.Format(area.ClaimCupboard)} to cupboard {Util.Format(cupboard)}");
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
            User.SendChatMessage(Messages.CannotClaimAreaAlreadyClaimed, area.Id, area.FactionId);
            return false;
          }

          string previousFactionId = area.FactionId;

          // If a new faction claims the claim cabinet for an area, they take control of that area.
          User.SendChatMessage(Messages.ClaimCaptured, area.Id, area.FactionId);
          Instance.PrintToChat(Messages.AreaCapturedAnnouncement, Faction.Id, area.Id, area.FactionId);
          Instance.Log($"{Util.Format(User)} captured the claim on {area.Id} from {area.FactionId} on behalf of {Faction.Id}");

          Instance.Areas.Claim(area, type, Faction, User, cupboard);
          return true;
        }

        Instance.PrintWarning("Area was in an unknown state during completion of AddingClaimInteraction. This shouldn't happen.");
        return false;
      }
    }
  }
}
