namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("war")]
    void OnWarCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.War.Enabled)
      {
        user.SendChatMessage(Messages.WarDisabled);
        return;
      }

      if (args.Length == 0)
      {
        OnWarHelpCommand(user);
        return;
      }

      var restArgs = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "list":
          OnWarListCommand(user);
          break;
        case "status":
          OnWarStatusCommand(user);
          break;
        case "declare":
          OnWarDeclareCommand(user, restArgs);
          break;
        case "accept":
          OnWarAcceptCommand(user, restArgs);
          break;
        case "end":
          OnWarEndCommand(user, restArgs);
          break;
        default:
          OnWarHelpCommand(user);
          break;
      }
    }
  }
}
