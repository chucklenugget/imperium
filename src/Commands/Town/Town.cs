﻿namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("town")]
    void OnTownCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableTowns)
      {
        user.SendChatMessage(Messages.TownsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        OnTownHelpCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "create":
          OnTownCreateCommand(user, restArguments);
          break;
        case "expand":
          OnTownExpandCommand(user);
          break;
        case "remove":
          OnTownRemoveCommand(user);
          break;
        case "disband":
          OnTownDisbandCommand(user);
          break;
        case "list":
          OnTownListCommand(user);
          break;
        default:
          OnTownHelpCommand(user);
          break;
      }
    }
  }
}
