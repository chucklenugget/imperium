namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("pin")]
    void OnPinCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.Map.PinsEnabled)
      {
        user.SendChatMessage(Messages.PinsDisabled);
        return;
      };

      if (args.Length == 0)
      {
        OnPinHelpCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          OnPinAddCommand(user, restArguments);
          break;
        case "remove":
          OnPinRemoveCommand(user, restArguments);
          break;
        case "list":
          OnPinListCommand(user, restArguments);
          break;
        case "delete":
          OnPinDeleteCommand(user, restArguments);
          break;
        case "help":
        default:
          OnPinHelpCommand(user);
          break;
      }
    }
  }
}
