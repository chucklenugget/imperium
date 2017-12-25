namespace Oxide.Plugins
{
  using System;
  using System.Linq;
  using System.Reflection;
  using System.Text;

  public partial class RustFactions : RustPlugin
  {
    static class Messages
    {
      public const string AreaClaimsDisabled = "Area claims are currently disabled.";
      public const string TaxationDisabled = "Taxation is currently disabled.";
      public const string BadlandsDisabled = "Badlands are currently disabled.";
      public const string TownsDisabled = "Towns are currently disabled.";
      public const string InteractionCanceled = "Command canceled.";
      public const string NoInteractionInProgress = "You aren't currently executing any commands.";

      public const string SelectingCupboardFailedInvalidTarget = "You must select a tool cupboard.";
      public const string SelectingCupboardFailedNotAuthorized = "You must be authorized on the tool cupboard.";

      public const string InteractionFailedNotMemberOfFaction = "You must be a member of a faction.";
      public const string InteractionFailedNotLeaderOfFaction = "You must be an owner or a moderator of a faction.";
      public const string InteractionFailedFactionTooSmall = "Your faction must have least {0} members.";

      public const string CannotClaimAreaBadlands = "You cannot claim this area because it is part of the badlands.";
      public const string CannotClaimAreaCannotAfford = "Claiming this area costs {0} scrap. Add this amount to your inventory and try again.";
      public const string SelectClaimCupboardToAdd = "Use the hammer to select a tool cupboard to represent the claim. Say /cancel to cancel.";
      public const string SelectClaimCupboardToRemove = "Use the hammer to select the tool cupboard representing the claim you want to remove. Say /cancel to cancel.";
      public const string SelectClaimCupboardForHeadquarters = "Use the hammer to select the tool cupboard to represent your faction's headquarters. Say /cancel to cancel.";
      public const string SelectingClaimCupboardFailedNotClaimCupboard = "That tool cabinet doesn't represent an area claim!";

      public const string ClaimCupboardMoved = "You have moved the claim on area {0} to a new tool cupboard.";
      public const string HeadquartersSet = "You have declared {0} to be your faction's headquarters.";
      public const string ClaimCaptured = "You have captured the area {0} from [{1}]!";
      public const string ClaimAdded = "You have claimed the area {0} for your faction.";
      public const string ClaimRemoved = "You have removed your faction's claim on {0}.";
      public const string ClaimFailedAlreadyClaimed = "You cannot claim the area {0}, because it is already claimed by [{1}]!";

      public const string CannotShowClaimBadUsage = "Usage: /claim show XY";
      public const string CannotListClaimsBadUsage = "Usage: /claim list FACTION";
      public const string CannotListClaimsUnknownFaction = "Unknown faction [{0}].";
      public const string AreaIsBadlands = "{0} is part of the badlands and cannot be claimed.";
      public const string AreaIsClaimed = "{0} is owned by [{1}].";
      public const string AreaIsHeadquarters = "{0} is the headquarters of [{1}].";
      public const string AreaIsUnclaimed = "{0} has not been claimed by a faction.";
      public const string ClaimsList = "[{0}] has claimed: {1}";

      public const string CannotShowClaimCostBadUsage = "Usage: /claim cost [XY] (If grid cell is omitted, it will show the cost of your current location.)";
      public const string CannotShowClaimCostAlreadyOwned = "{0} has already been claimed by [{1}]!";
      public const string ClaimCost = "{0} can be claimed by [{1}] for {2} scrap.";

      public const string CannotDeleteClaimsBadUsage = "Usage: /claims delete XY [XY XY...]";
      public const string CannotDeleteClaimsNoPermission = "You don't have permission to delete claims you don't own. Did you mean /claim remove?";
      public const string CannotDeleteClaimsAreaNotClaimed = "{0} has not been claimed by a faction.";

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
      public const string CannotSetBadlandsAreaClaimed = "Cannot set {0} as badlands, since it has already been claimed by [{1}].";
      public const string BadlandsAdded = "Added {0} to badlands. Badlands areas are now: {1}";
      public const string BadlandsRemoved = "Removed {0} from badlands. Badlands areas are now: {1}";
      public const string BadlandsSet = "Badlands areas are now: {0}";
      public const string BadlandsList = "Badlands areas are: {0}. Gather bonus is {1}%.";

      public const string CannotManageTownsNoPermission = "You don't have permission to manage towns.";
      public const string CannotCreateTownWrongUsage = "Usage: /town create NAME";
      public const string CannotCreateTownAlreadyMayor = "You cannot create a new town, because you are already the mayor of {0}.";
      public const string CannotCreateTownAreaIsBadlands = "You cannot create a new town in the badlands!";
      public const string CannotCreateTownAreaIsClaimed = "You cannot create a new town in {0}, because it has already been claimed by a faction.";
      public const string CannotCreateTownOneAlreadyExists = "You cannot create a town in {0}, because it is already the location of {1}.";
      public const string SelectTownCupboardToAdd = "Use the hammer to select a tool cupboard to represent {0}. Say /cancel to cancel.";
      public const string SelectTownCupboardToRemove = "Use the hammer to select the tool cupboard representing the town you want to remove. Say /cancel to cancel.";
      public const string SelectingTownCupboardFailedNotTownCupboard = "That tool cupboard doesn't represent a town!";
      public const string SelectingTownCupboardFailedNotMayor = "That tool cupboard represents {0}, which you are not the mayor of!";
      public const string TownCreated = "You have founded the town {0}!";
      public const string TownRemoved = "You have disbanded the town {0}!";

      public const string EnteredBadlands = "<color=#ff0000>BORDER:</color> You have entered the badlands! Player violence is allowed here.";
      public const string EnteredUnclaimedArea = "<color=#ffd479>BORDER:</color> You have entered unclaimed land.";
      public const string EnteredClaimedArea = "<color=#ffd479>BORDER:</color> You have entered land claimed by [{0}].";
    }

    void InitLang()
    {
      var messages = typeof(Messages).GetFields(BindingFlags.Public)
        .Select(f => (string)f.GetRawConstantValue())
        .ToDictionary(str => str);

      lang.RegisterMessages(messages, this);
    }

    void SendMessage(BasePlayer player, string message, params object[] args)
    {
      SendReply(player, String.Format(lang.GetMessage(message, this, player.UserIDString), args));
    }

    void SendMessage(BasePlayer player, StringBuilder sb)
    {
      SendReply(player, sb.ToString().TrimEnd());
    }
  }

}
