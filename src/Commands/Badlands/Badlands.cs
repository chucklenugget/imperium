namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("badlands")]
    void OnBadlandsCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableBadlands)
      {
        user.SendChatMessage(Messages.BadlandsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        var areas = Areas.GetAllByType(AreaType.Badlands).Select(a => a.Id);
        user.SendChatMessage(Messages.BadlandsList, FormatList(areas), Options.BadlandsGatherBonus);
        return;
      }

      if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

      var areaIds = args.Skip(1).Select(arg => NormalizeAreaId(arg)).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          if (args.Length < 2)
            user.SendChatMessage(Messages.Usage, "/badlands add [XY XY XY...]");
          else
            OnAddBadlandsCommand(user, areaIds);
          break;

        case "remove":
          if (args.Length < 2)
            user.SendChatMessage(Messages.Usage, "/badlands remove [XY XY XY...]");
          else
            OnRemoveBadlandsCommand(user, areaIds);
          break;

        case "set":
          if (args.Length < 2)
            user.SendChatMessage(Messages.Usage, "/badlands set [XY XY XY...]");
          else
            OnSetBadlandsCommand(user, areaIds);
          break;

        case "clear":
          if (args.Length != 1)
            user.SendChatMessage(Messages.Usage, "/badlands clear");
          else
            OnSetBadlandsCommand(user, new string[0]);
          break;

        default:
          OnBadlandsHelpCommand(user);
          break;
      }
    }
  }
}
