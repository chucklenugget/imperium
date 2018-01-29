namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("claim")]
    void OnClaimCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableAreaClaims)
      {
        user.SendChatMessage(Messages.AreaClaimsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        OnClaimAddCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          OnClaimAddCommand(user);
          break;
        case "remove":
          OnClaimRemoveCommand(user);
          break;
        case "hq":
          OnClaimHeadquartersCommand(user);
          break;
        case "rename":
          OnClaimRenameCommand(user, restArguments);
          break;
        case "give":
          OnClaimGiveCommand(user, restArguments);
          break;
        case "cost":
          OnClaimCostCommand(user, restArguments);
          break;
        case "upkeep":
          OnClaimUpkeepCommand(user);
          break;
        case "show":
          OnClaimShowCommand(user, restArguments);
          break;
        case "list":
          OnClaimListCommand(user, restArguments);
          break;
        case "assign":
          OnClaimAssignCommand(user, restArguments);
          break;
        case "delete":
          OnClaimDeleteCommand(user, restArguments);
          break;
        default:
          OnClaimHelpCommand(user);
          break;
      }
    }
  }
}
