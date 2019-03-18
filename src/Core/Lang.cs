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
      public const string UpkeepDisabled = "Upkeep is currently disabled.";
      public const string WarDisabled = "War is currently disabled.";
      public const string PinsDisabled = "Map pins are currently disabled.";
      public const string PvpModeDisabled = "PVP Mode is currently not available.";

      public const string AreaIsBadlands = "<color=#ffd479>{0}</color> is a part of the badlands.";
      public const string AreaIsClaimed = "<color=#ffd479>{0}</color> has been claimed by <color=#ffd479>[{1}]</color>.";
      public const string AreaIsHeadquarters = "<color=#ffd479>{0}</color> is the headquarters of <color=#ffd479>[{1}]</color>.";
      public const string AreaIsWilderness = "<color=#ffd479>{0}</color> has not been claimed by a faction.";
      public const string AreaNotBadlands = "<color=#ffd479>{0}</color> is not a part of the badlands.";
      public const string AreaNotOwnedByYourFaction = "<color=#ffd479>{0}</color> is not owned by your faction.";
      public const string AreaNotWilderness = "<color=#ffd479>{0}</color> is not currently wilderness.";
      public const string AreaNotContiguous = "<color=#ffd479>{0}</color> is not connected to territory owned by <color=#ffd479>[{1}]</color>.";
      public const string YouAreInTheGreatUnknown = "You are currently in the great unknown!";

      public const string InteractionCanceled = "Command canceled.";
      public const string NoInteractionInProgress = "You aren't currently executing any commands.";
      public const string NoAreasClaimed = "Your faction has not claimed any areas.";
      public const string NotMemberOfFaction = "You are not a member of a faction.";
      public const string AlreadyMemberOfFaction = "You are already a member of a faction.";
      public const string NotLeaderOfFaction = "You must be an owner or a manager of a faction.";
      public const string FactionTooSmallToOwnLand = "To own land, a faction must have least {0} members.";
      public const string FactionOwnsTooMuchLand = "<color=#ffd479>[{0}]</color> already owns the maximum number of areas (<color=#ffd479>{1}</color>).";
      public const string FactionHasTooManyMembers = "<color=#ffd479>[{0}]</color> already has the maximum number of members (<color=#ffd479>{1}</color>).";
      public const string FactionDoesNotOwnLand = "Your faction must own at least one area.";
      public const string FactionAlreadyExists = "There is already a faction named <color=#ffd479>[{0}]</color>.";
      public const string FactionDoesNotExist = "There is no faction named <color=#ffd479>[{0}]</color>.";
      public const string InvalidUser = "Couldn't find a user whose name matches \"{0}\".";
      public const string InvalidFactionName = "Faction names must be between {0} and {1} alphanumeric characters.";
      public const string NotAtWar = "You are not currently at war with <color=#ffd479>[{0}]</color>!";

      public const string Usage = "Usage: <color=#ffd479>{0}</color>";
      public const string CommandIsOnCooldown = "You can't do that again so quickly. Try again in {0} seconds.";
      public const string NoPermission = "You don't have permission to do that.";

      public const string MemberAdded = "You have added <color=#ffd479>{0}</color> as a member of <color=#ffd479>[{1}]</color>.";
      public const string MemberRemoved = "You have removed <color=#ffd479>{0}</color> as a member of <color=#ffd479>[{1}]</color>.";
      public const string ManagerAdded = "You have added <color=#ffd479>{0}</color> as a manager of <color=#ffd479>[{1}]</color>.";
      public const string ManagerRemoved = "You have removed <color=#ffd479>{0}</color> as a manager of <color=#ffd479>[{1}]</color>.";
      public const string UserIsAlreadyMemberOfFaction = "<color=#ffd479>{0}</color> is already a member of <color=#ffd479>[{1}]</color>.";
      public const string UserIsNotMemberOfFaction = "<color=#ffd479>{0}</color> is not a member of <color=#ffd479>[{1}]</color>.";
      public const string UserIsAlreadyManagerOfFaction = "<color=#ffd479>{0}</color> is already a manager of <color=#ffd479>[{1}]</color>.";
      public const string UserIsNotManagerOfFaction = "<color=#ffd479>{0}</color> is not a manager of <color=#ffd479>[{1}]</color>.";
      public const string CannotPromoteOrDemoteOwnerOfFaction = "<color=#ffd479>{0}</color> cannot be promoted or demoted, since they are the owner of <color=#ffd479>[{1}]</color>.";
      public const string CannotKickLeaderOfFaction = "<color=#ffd479>{0}</color> cannot be kicked, since they are an owner or manager of <color=#ffd479>[{1}]</color>.";
      public const string InviteAdded = "You have invited <color=#ffd479>{0}</color> to join <color=#ffd479>[{1}]</color>.";
      public const string InviteReceived = "<color=#ffd479>{0}</color> has invited you to join <color=#ffd479>[{1}]</color>. Say <color=#ffd479>/faction join {1}</color> to accept.";
      public const string CannotJoinFactionNotInvited = "You cannot join <color=#ffd479>[{0}]</color>, because you have not been invited.";
      public const string YouJoinedFaction = "You are now a member of <color=#ffd479>[{0}]</color>.";
      public const string YouLeftFaction = "You are no longer a member of <color=#ffd479>[{0}]</color>.";

      public const string SelectingCupboardFailedInvalidTarget = "You must select a tool cupboard.";
      public const string SelectingCupboardFailedNotAuthorized = "You must be authorized on the tool cupboard.";
      public const string SelectingCupboardFailedNotClaimCupboard = "That tool cupboard doesn't represent an area claim made by your faction.";

      public const string CannotClaimAreaAlreadyClaimed = "<color=#ffd479>{0}</color> has already been claimed by <color=#ffd479>[{1}]</color>.";
      public const string CannotClaimAreaCannotAfford = "Claiming this area costs <color=#ffd479>{0}</color> scrap. Add this amount to your inventory and try again.";
      public const string CannotClaimAreaAlreadyOwned = "The area <color=#ffd479>{0}</color> is already owned by your faction, and this cupboard represents the claim.";

      public const string SelectClaimCupboardToAdd = "Use the hammer to select a tool cupboard to represent the claim. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectClaimCupboardToRemove = "Use the hammer to select the tool cupboard representing the claim you want to remove. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectClaimCupboardForHeadquarters = "Use the hammer to select the tool cupboard to represent your faction's headquarters. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectClaimCupboardToAssign = "Use the hammer to select a tool cupboard to represent the claim to assign to <color=#ffd479>[{0}]</color>. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectClaimCupboardToTransfer = "Use the hammer to select the tool cupboard representing the claim to give to <color=#ffd479>[{0}]</color>. Say <color=#ffd479>/cancel</color> to cancel.";

      public const string ClaimCupboardMoved = "You have moved the claim <color=#ffd479>{0}</color> to a new tool cupboard.";
      public const string ClaimCaptured = "You have captured <color=#ffd479>{0}</color> from <color=#ffd479>[{1}]</color>!";
      public const string ClaimAdded = "You have claimed <color=#ffd479>{0}</color> for your faction.";
      public const string ClaimRemoved = "You have removed your faction's claim on <color=#ffd479>{0}</color>.";
      public const string ClaimTransferred = "You have transferred ownership of <color=#ffd479>{0}</color> to <color=#ffd479>[{1}]</color>.";

      public const string InvalidAreaName = "Area names must be between <color=#ffd479>{0}</color> and <color=#ffd479>{1}</color> characters long.";
      public const string UnknownArea = "Unknown area <color=#ffd479>{0}</color>.";
      public const string AreaRenamed = "<color=#ffd479>{0}</color> is now known as <color=#ffd479>{1}</color>.";

      public const string ClaimsList = "<color=#ffd479>[{0}]</color> has claimed: <color=#ffd479>{1}</color>";
      public const string ClaimCost = "<color=#ffd479>{0}</color> can be claimed by <color=#ffd479>[{1}]</color> for <color=#ffd479>{2}</color> scrap.";
      public const string UpkeepCost = "It will cost <color=#ffd479>{0}</color> scrap per day to maintain the <color=#ffd479>{1}</color> areas claimed by <color=#ffd479>[{2}]</color>. Upkeep is due <color=#ffd479>{3}</color> hours from now.";
      public const string UpkeepCostOverdue = "It will cost <color=#ffd479>{0}</color> scrap per day to maintain the <color=#ffd479>{1}</color> areas claimed by <color=#ffd479>[{2}]</color>. Your upkeep is <color=#ffd479>{3}</color> hours overdue! Fill your tax chest with scrap immediately, before your claims begin to fall into ruin.";

      public const string SelectTaxChest = "Use the hammer to select the container to receive your faction's tribute. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectingTaxChestFailedInvalidTarget = "That can't be used as a tax chest.";
      public const string SelectingTaxChestSucceeded = "You have selected a new tax chest that will receive <color=#ffd479>{0}%</color> of the materials harvested within land owned by <color=#ffd479>[{1}]</color>. To change the tax rate, say <color=#ffd479>/tax rate PERCENT</color>.";

      public const string CannotSetTaxRateInvalidValue = "You must specify a valid percentage between <color=#ffd479>0-{0}%</color> as a tax rate.";
      public const string SetTaxRateSuccessful = "You have set the tax rate on the land holdings of <color=#ffd479>[{0}]</color> to <color=#ffd479>{1}%</color>.";

      public const string BadlandsSet = "Badlands areas are now: <color=#ffd479>{0}</color>";
      public const string BadlandsList = "Badlands areas are: <color=#ffd479>{0}</color>. Gather bonus is <color=#ffd479>{1}%</color>.";

      public const string CannotDeclareWarAgainstYourself = "You cannot declare war against yourself!";
      public const string CannotDeclareWarAlreadyAtWar = "You area already at war with <color=#ffd479>[{0}]</color>!";
      public const string CannotDeclareWarInvalidCassusBelli = "You cannot declare war against <color=#ffd479>[{0}]</color>, because your reason doesn't meet the minimum length.";
      public const string CannotOfferPeaceAlreadyOfferedPeace = "You have already offered peace to <color=#ffd479>[{0}]</color>.";
      public const string PeaceOffered = "You have offered peace to <color=#ffd479>[{0}]</color>. They must accept it before the war will end.";

      public const string EnteredBadlands = "<color=#ff0000>BORDER:</color> You have entered the badlands! Player violence is allowed here.";
      public const string EnteredWilderness = "<color=#ffd479>BORDER:</color> You have entered the wilderness.";
      public const string EnteredClaimedArea = "<color=#ffd479>BORDER:</color> You have entered land claimed by <color=#ffd479>[{0}]</color>.";

      public const string EnteredPvpMode = "<color=#ff0000>PVP ENABLED:</color> You are now in PVP mode. You can now hurt, and be hurt by, other players who are also in PVP mode.";
      public const string ExitedPvpMode = "<color=#00ff00>PVP DISABLED:</color> You are no longer in PVP mode. You can't be harmed by other players except inside of normal PVP areas.";
      public const string PvpModeOnCooldown = "You must wait at least {0} seconds to exit or re-enter PVP mode.";

      public const string InvalidPinType = "Unknown map pin type <color=#ffd479>{0}</color>. Say <color=#ffd479>/pin types</color> to see a list of available types.";
      public const string InvalidPinName = "Map pin names must be between <color=#ffd479>{0}</color> and <color=#ffd479>{1}</color> characters long.";
      public const string CannotCreatePinCannotAfford = "Creating a new map pin costs <color=#ffd479>{0}</color> scrap. Add this amount to your inventory and try again.";
      public const string CannotCreatePinAlreadyExists = "Cannot create a new map pin named <color=#ffd479>{0}</color>, since one already exists with the same name in <color=#ffd479>{1}</color>.";
      public const string UnknownPin = "Unknown map pin <color=#ffd479>{0}</color>.";
      public const string CannotRemovePinAreaNotOwnedByYourFaction = "Cannot remove the map pin named <color=#ffd479>{0}</color>, because the area <color=#ffd479>{1} is not owned by your faction.";
      public const string PinRemoved = "Removed map pin <color=#ffd479>{0}</color>.";

      public const string FactionCreatedAnnouncement = "<color=#00ff00>FACTION CREATED:</color> A new faction <color=#ffd479>[{0}]</color> has been created!";
      public const string FactionDisbandedAnnouncement = "<color=#00ff00>FACTION DISBANDED:</color> <color=#ffd479>[{0}]</color> has been disbanded!";
      public const string FactionMemberJoinedAnnouncement = "<color=#00ff00>MEMBER JOINED:</color> <color=#ffd479>{0}</color> has joined <color=#ffd479>[{1}]</color>!";
      public const string FactionMemberLeftAnnouncement = "<color=#00ff00>MEMBER LEFT:</color> <color=#ffd479>{0}</color> has left <color=#ffd479>[{1}]</color>!";

      public const string AreaClaimedAnnouncement = "<color=#00ff00>AREA CLAIMED:</color> <color=#ffd479>[{0}]</color> claims <color=#ffd479>{1}</color>!";
      public const string AreaClaimedAsHeadquartersAnnouncement = "<color=#00ff00>AREA CLAIMED:</color> <color=#ffd479>[{0}]</color> claims <color=#ffd479>{1}</color> as their headquarters!";
      public const string AreaCapturedAnnouncement = "<color=#ff0000>AREA CAPTURED:</color> <color=#ffd479>[{0}]</color> has captured <color=#ffd479>{1}</color> from <color=#ffd479>[{2}]</color>!";
      public const string AreaClaimRemovedAnnouncement = "<color=#ff0000>CLAIM REMOVED:</color> <color=#ffd479>[{0}]</color> has relinquished their claim on <color=#ffd479>{1}</color>!";
      public const string AreaClaimTransferredAnnouncement = "<color=#ff0000>CLAIM TRANSFERRED:</color> <color=#ffd479>[{0}]</color> has transferred their claim on <color=#ffd479>{1}</color> to <color=#ffd479>[{2}]</color>!";
      public const string AreaClaimAssignedAnnouncement = "<color=#ff0000>AREA CLAIM ASSIGNED:</color> <color=#ffd479>{0}</color> has been assigned to <color=#ffd479>[{1}]</color> by an admin.";
      public const string AreaClaimDeletedAnnouncement = "<color=#ff0000>AREA CLAIM REMOVED:</color> <color=#ffd479>[{0}]</color>'s claim on <color=#ffd479>{1}</color> has been removed by an admin.";
      public const string AreaClaimLostCupboardDestroyedAnnouncement = "<color=#ff0000>AREA CLAIM LOST:</color> <color=#ffd479>[{0}]</color> has lost its claim on <color=#ffd479>{1}</color>, because the tool cupboard was destroyed!";
      public const string AreaClaimLostFactionDisbandedAnnouncement = "<color=#ff0000>AREA CLAIM LOST:</color> <color=#ffd479>[{0}]</color> has been disbanded, losing its claim on <color=#ffd479>{1}</color>!";
      public const string AreaClaimLostUpkeepNotPaidAnnouncement = "<color=#ff0000>AREA CLAIM LOST:</color>: <color=#ffd479>[{0}]</color> has lost their claim on <color=#ffd479>{1}</color> after it fell into ruin!";
      public const string HeadquartersChangedAnnouncement = "<color=#00ff00>HQ CHANGED:</color> The headquarters of <color=#ffd479>[{0}]</color> is now <color=#ffd479>{1}</color>.";
      public const string WarDeclaredAnnouncement = "<color=#ff0000>WAR DECLARED:</color> <color=#ffd479>[{0}]</color> has declared war on <color=#ffd479>[{1}]</color>! Their reason: <color=#ffd479>{2}</color>";
      public const string WarEndedTreatyAcceptedAnnouncement = "<color=#00ff00>WAR ENDED:</color> The war between <color=#ffd479>[{0}]</color> and <color=#ffd479>[{1}]</color> has ended after both sides have agreed to a treaty.";
      public const string WarEndedFactionEliminatedAnnouncement = "<color=#00ff00>WAR ENDED:</color> The war between <color=#ffd479>[{0}]</color> and <color=#ffd479>[{1}]</color> has ended, since <color=#ffd479>[{2}]</color> no longer holds any land.";
      public const string PinAddedAnnouncement = "<color=#00ff00>POINT OF INTEREST:</color> <color=#ffd479>[{0}]</color> announces the creation of <color=#ffd479>{1}</color>, a new {2} located in <color=#ffd479>{3}</color>!";
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
