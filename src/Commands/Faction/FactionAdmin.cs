namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    void OnFactionAdminCommand(User user, string[] args)
    {
      if (!user.HasPermission(Permission.AdminFactions))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

      if (args.Length == 0)
      {
        OnFactionHelpCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "promote":
          OnFactionAdminPromoteCommand(user, restArguments);
          break;
        case "demote":
          OnFactionAdminDemoteCommand(user, restArguments);
          break;
        case "kick":
          OnFactionAdminKickCommand(user, restArguments);
          break;
        case "owner":
          OnFactionAdminOwnerCommand(user, restArguments);
          break;
        case "disband":
          OnFactionAdminDisbandCommand(user, restArguments);
          break;
        default:
          OnFactionHelpCommand(user);
          break;
      }
    }
  }
}
