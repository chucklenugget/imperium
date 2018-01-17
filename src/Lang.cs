namespace Oxide.Plugins
{
  using System.Linq;
  using System.Reflection;

  public partial class Imperium : RustPlugin
  {
    static class Messages
    {
      public const string AreaClaimsDisabled = "Area claims are currently disabled.";
      public const string TaxationDisabled = "Taxation is currently disabled.";
      public const string BadlandsDisabled = "Badlands are currently disabled.";
      public const string TownsDisabled = "Towns are currently disabled.";
      public const string UpkeepDisabled = "Upkeep is currently disabled.";

      public const string InteractionCanceled = "Command canceled.";
      public const string NoInteractionInProgress = "You aren't currently executing any commands.";
      public const string NoAreasClaimed = "Your faction has not claimed any areas.";
      public const string CommandIsOnCooldown = "You can't do that again so quickly. Try again in {0} seconds.";

      public const string SelectingCupboardFailedInvalidTarget = "You must select a tool cupboard.";
      public const string SelectingCupboardFailedNotAuthorized = "You must be authorized on the tool cupboard.";
      public const string SelectingCupboardFailedNotClaimCupboard = "That tool cupboard doesn't represent an area claim made by your faction.";

      public const string InteractionFailedNotMemberOfFaction = "You must be a member of a faction.";
      public const string InteractionFailedNotLeaderOfFaction = "You must be an owner or a moderator of a faction.";
      public const string InteractionFailedFactionTooSmall = "Your faction must have least {0} members.";
      public const string InteractionFailedNotMayorOfTown = "You are not the mayor of a town. To create one, use /town create NAME.";
      public const string InteractionFailedFactionDoesNotOwnLand = "Your faction must own at least one area.";
      public const string InteractionFailedUnknownFaction = "Unknown faction [{0}].";
      public const string InteractionFailedNotAtWar = "You are not currently at war with [{0}]!";

      public const string CannotClaimAreaIsBadlands = "You cannot claim the area {0}, because it is part of the badlands.";
      public const string CannotClaimAreaIsTown = "You cannot claim the area {0}, because it is already a town owned by [{1}].";
      public const string CannotClaimAreaIsClaimed = "You cannot claim the area {0}, because it is already claimed by [{1}]!";
      public const string CannotClaimAreaCannotAfford = "Claiming this area costs {0} scrap. Add this amount to your inventory and try again.";
      public const string CannotClaimAreaAlreadyOwned = "The area {0} is already owned by your faction, and this cupboard represents the claim.";

      public const string SelectClaimCupboardToAdd = "Use the hammer to select a tool cupboard to represent the claim. Say /cancel to cancel.";
      public const string SelectClaimCupboardToRemove = "Use the hammer to select the tool cupboard representing the claim you want to remove. Say /cancel to cancel.";
      public const string SelectClaimCupboardForHeadquarters = "Use the hammer to select the tool cupboard to represent your faction's headquarters. Say /cancel to cancel.";

      public const string ClaimCupboardMoved = "You have moved the claim on area {0} to a new tool cupboard.";
      public const string HeadquartersMoved = "You have declared {0} to be your faction's headquarters.";
      public const string ClaimCaptured = "You have captured the area {0} from [{1}]!";
      public const string ClaimAdded = "You have claimed the area {0} for your faction.";
      public const string ClaimRemoved = "You have removed your faction's claim on {0}.";

      public const string CannotRenameAreaBadUsage = "Usage: /claim rename XY \"NAME\"";
      public const string CannotRenameAreaBadName = "Cannot rename {0}. The name must be at least {1} characters long.";
      public const string CannotRenameAreaUnknownAreaId = "Cannot rename {0}. Unknown area ID.";
      public const string CannotRenameAreaNotClaimedByFaction = "Cannot rename {0}, because it isn't claimed by your faction.";
      public const string CannotRenameAreaIsTown = "Cannot rename {0}, because it is part of the town {1}.";
      public const string AreaRenamed = "The area {0} is now known as {1}.";

      public const string CannotShowClaimBadUsage = "Usage: /claim show XY";
      public const string CannotListClaimsBadUsage = "Usage: /claim list FACTION";
      public const string AreaIsBadlands = "{0} is part of the badlands and cannot be claimed.";
      public const string AreaIsClaimed = "{0} is owned by [{1}].";
      public const string AreaIsHeadquarters = "{0} is the headquarters of [{1}].";
      public const string AreaIsWilderness = "{0} has not been claimed by a faction.";
      public const string AreaIsTown = "{0} is part of the town of {1}, which is managed by [{2}].";
      public const string ClaimsList = "[{0}] has claimed: {1}";

      public const string CannotShowClaimCostBadUsage = "Usage: /claim cost [XY] (If grid cell is omitted, it will show the cost of your current location.)";
      public const string ClaimCost = "{0} can be claimed by [{1}] for {2} scrap.";
      public const string UpkeepCost = "It will cost {0} scrap per day to maintain the {1} areas claimed by [{2}]. Upkeep is due {3} hours from now.";
      public const string UpkeepCostOverdue = "It will cost {0} scrap per day to maintain the {1} areas claimed by [{2}]. Your upkeep is {3} hours overdue! Fill your tax chest with scrap immediately, before your claims begin to fall into ruin.";

      public const string CannotDeleteClaimsBadUsage = "Usage: /claims delete XY [XY XY...]";
      public const string CannotDeleteClaimsNoPermission = "You don't have permission to delete claims you don't own. Did you mean /claim remove?";
      public const string CannotDeleteClaimsAreaIsBadlands = "You cannot delete the claim on {0}, because it is part of the badlands.";
      public const string CannotDeleteClaimsAreaNotUnclaimed = "You cannot delete the claim on {0}, because it has not been claimed by a faction.";

      public const string CannotSelectTaxChestNotMemberOfFaction = "You cannot select a tax chest without being a member of a faction!";
      public const string CannotSelectTaxChestNotFactionLeader = "You cannot select a tax chest because you aren't an owner or a moderator of your faction.";
      public const string SelectTaxChest = "Use the hammer to select the container to receive your faction's tribute. Say /cancel to cancel.";
      public const string SelectingTaxChestFailedInvalidTarget = "That can't be used as a tax chest.";
      public const string SelectingTaxChestSucceeded = "You have selected a new tax chest that will receive {0}% of the materials harvested within land owned by [{1}]. To change the tax rate, say /tax rate PERCENT.";

      public const string CannotSetTaxRateNotMemberOfFaction = "You cannot set a tax rate without being a member of a faction!";
      public const string CannotSetTaxRateNotFactionLeader = "You cannot set a tax rate because you aren't an owner or a moderator of your faction.";
      public const string CannotSetTaxRateInvalidValue = "You must specify a valid percentage between 0-{0}% as a tax rate.";
      public const string SetTaxRateSuccessful = "You have set the tax rate on the land holdings of [{0}] to {1}%.";

      public const string CannotSetBadlandsNoPermission = "You don't have permission to alter badlands.";
      public const string CannotSetBadlandsWrongUsage = "Usage: /badlands <add|remove|set|clear> [XY XY XY...]";
      public const string CannotSetBadlandsUnknownArea = "Cannot add {0} to badlands. Unknown area identifier.";
      public const string CannotSetBadlandsNotWilderness = "Cannot add {0} to badlands, since it is not currently wilderness.";
      public const string CannotSetBadlandsNotBadlands = "Cannot remove {0} from badlands, since it is not currently badlands.";

      public const string BadlandsSet = "Badlands areas are now: {0}";
      public const string BadlandsList = "Badlands areas are: {0}. Gather bonus is {1}%.";

      public const string CannotManageTownsNoPermission = "You don't have permission to manage towns.";
      public const string CannotCreateTownWrongUsage = "Usage: /town create NAME";
      public const string CannotCreateTownAlreadyMayor = "You cannot create a new town, because you are already the mayor of {0}. To expand the town instead, use /town expand.";
      public const string CannotCreateTownSameNameAlreadyExists = "You cannot create a new town named {0}, because a town with that name already exists. To expand the town instead, use /town expand.";
      public const string CannotAddToTownAreaIsHeadquarters = "The area {0} cannot be added to a town, because it is currently your faction's headquarters.";
      public const string CannotAddToTownOneAlreadyExists = "The area {0} cannot be added to town, because it is already part of the town of {1}.";
      public const string CannotRemoveFromTownNotPartOfTown = "The area {0} cannot be removed from a town, because it is not part of a town.";

      public const string SelectTownCupboardToCreate = "Use the hammer to select a tool cupboard to represent {0}. Say /cancel to cancel.";
      public const string SelectTownCupboardToExpand = "Use the hammer to select a tool cupboard to add to {0}. Say /cancel to cancel.";
      public const string SelectTownCupboardToRemove = "Use the hammer to select the tool cupboard representing the town you want to remove. Say /cancel to cancel.";
      public const string SelectingTownCupboardFailedNotTownCupboard = "That tool cupboard doesn't represent a town!";
      public const string SelectingTownCupboardFailedNotMayor = "That tool cupboard represents {0}, which you are not the mayor of!";
      public const string TownCreated = "You have founded the town of {0}.";
      public const string AreaAddedToTown = "You have added the area {0} to the town of {1}.";
      public const string AreaRemovedFromTown = "You have removed the area {0} from the town of {1}.";
      public const string TownDisbanded = "You have disbanded the town of {0}!";

      public const string CannotDeclareWarWrongUsage = "Usage: /war declare FACTION REASON";
      public const string CannotDeclareWarAgainstYourself = "You cannot declare war against yourself!";
      public const string CannotDeclareWarAlreadyAtWar = "You cannot declare war against [{0}], because you are already at war with them!";
      public const string CannotDeclareWarInvalidCassusBelli = "You cannot declare war against [{0}], because your reason doesn't meet the minimum length.";
      public const string CannotOfferPeaceAlreadyOfferedPeace = "You have already offered peace to [{0}].";
      public const string WarDeclared = "You have declared WAR against [{0}]!";
      public const string WarEnded = "You have accepted the offer of peace from [{0}], ending the war.";
      public const string PeaceOffered = "You have offered peace to [{0}]. They must accept it before the war will end.";

      public const string EnteredBadlands = "<color=#ff0000>BORDER:</color> You have entered the badlands! Player violence is allowed here.";
      public const string EnteredWilderness = "<color=#ffd479>BORDER:</color> You have entered the wilderness.";
      public const string EnteredTown = "<color=#ffd479>BORDER:</color> You have entered the town of {0}, controlled by [{1}].";
      public const string EnteredClaimedArea = "<color=#ffd479>BORDER:</color> You have entered land claimed by [{0}].";

      public const string AreaClaimedAnnouncement = "<color=#00ff00>AREA CLAIMED:</color> [{0}] claims {1}!";
      public const string AreaClaimedAsHeadquartersAnnouncement = "<color=#00ff00>AREA CLAIMED:</color> [{0}] claims {1} as their headquarters!";
      public const string AreaCapturedAnnouncement = "<color=#ff0000>AREA CAPTURED:</color> [{0}] has captured {1} from [{2}]!";
      public const string AreaClaimRemovedAnnouncement = "<color=#ff0000>CLAIM REMOVED:</color> [{0}] has relinquished their claim on {1}!";
      public const string AreaClaimDeletedAnnouncement = "<color=#ff0000>AREA CLAIM REMOVED:</color> [{0}]'s claim on {1} has been removed by an admin.";
      public const string AreaClaimLostCupboardDestroyedAnnouncement = "<color=#ff0000>AREA CLAIM LOST:</color> [{0}] has lost its claim on {1}, because the tool cupboard was destroyed!";
      public const string AreaClaimLostFactionDisbandedAnnouncement = "<color=#ff0000>AREA CLAIM LOST:</color> [{0}] has been disbanded, losing its claim on {1}!";
      public const string AreaClaimLostUpkeepNotPaidAnnouncement = "<color=#ff0000>AREA CLAIM LOST:</color>: [{0}] has lost their claim on {1} after it fell into ruin!";
      public const string HeadquartersChangedAnnouncement = "<color=#00ff00>HQ CHANGED:</color> The headquarters of [{0}] is now {1}.";
      public const string TownCreatedAnnouncement = "<color=#00ff00>TOWN FOUNDED:</color> [{0}] has founded the town of {1} in {2}.";
      public const string TownDisbandedAnnouncement = "<color=#ff0000>TOWN DISBANDED:</color> [{0}] has disbanded the town of {1}.";
      public const string WarDeclaredAnnouncement = "<color=#ff0000>WAR DECLARED:</color> [{0}] has declared war on [{1}]! Their reason: {2}";
      public const string WarEndedTreatyAcceptedAnnouncement = "<color=#00ff00>WAR ENDED:</color> The war between [{0}] and [{1}] has ended after both sides have agreed to a treaty.";
      public const string WarEndedFactionEliminatedAnnouncement = "<color=#00ff00>WAR ENDED:</color> The war between [{0}] and [{1}] has ended, since [{2}] no longer holds any land.";
    }

    void InitLang()
    {
      var messages = typeof(Messages).GetFields(BindingFlags.Public)
        .Select(f => (string)f.GetRawConstantValue())
        .ToDictionary(str => str);

      lang.RegisterMessages(messages, this);
    }
  }
}
