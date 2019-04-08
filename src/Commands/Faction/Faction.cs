namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("faction")]
    void OnFactionCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (args.Length == 0)
      {
        OnFactionShowCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "create":
          OnFactionCreateCommand(user, restArguments);
          break;
        case "join":
          OnFactionJoinCommand(user, restArguments);
          break;
        case "leave":
          OnFactionLeaveCommand(user, restArguments);
          break;
        case "invite":
          OnFactionInviteCommand(user, restArguments);
          break;
        case "kick":
          OnFactionKickCommand(user, restArguments);
          break;
        case "promote":
          OnFactionPromoteCommand(user, restArguments);
          break;
        case "demote":
          OnFactionDemoteCommand(user, restArguments);
          break;
        case "disband":
          OnFactionDisbandCommand(user, restArguments);
          break;
        case "admin":
          OnFactionAdminCommand(user, restArguments);
          break;
        case "help":
        default:
          OnFactionHelpCommand(user);
          break;
      }
    }

    [ChatCommand("clan")]
    void OnClanCommand(BasePlayer player, string command, string[] args)
    {
      OnFactionCommand(player, command, args);
    }
  }
}
