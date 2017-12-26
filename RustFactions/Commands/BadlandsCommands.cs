namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class RustFactions
  {
    [ChatCommand("badlands")]
    void OnBadlandsCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableBadlands)
      {
        user.SendMessage(Messages.BadlandsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        var areas = Areas.GetAllByType(AreaType.Badlands).Select(a => a.Id);
        user.SendMessage(Messages.BadlandsList, FormatList(areas), Options.BadlandsGatherBonus);
        return;
      }

      if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
      {
        user.SendMessage(Messages.CannotSetBadlandsNoPermission);
        return;
      }

      var areaIds = args.Skip(1).Select(arg => NormalizeAreaId(arg)).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          if (args.Length < 2)
            user.SendMessage(Messages.CannotSetBadlandsWrongUsage);
          else
            OnAddBadlandsCommand(user, areaIds);
          break;
        case "remove":
          if (args.Length < 2)
            user.SendMessage(Messages.CannotSetBadlandsWrongUsage);
          else
            OnRemoveBadlandsCommand(user, areaIds);
          break;
        case "set":
          if (args.Length < 2)
            user.SendMessage(Messages.CannotSetBadlandsWrongUsage);
          else
            OnSetBadlandsCommand(user, areaIds);
          break;
        case "clear":
          if (args.Length != 1)
            user.SendMessage(Messages.CannotSetBadlandsWrongUsage);
          else
            OnSetBadlandsCommand(user, new string[0]);
          break;
        default:
          user.SendMessage(Messages.CannotSetBadlandsWrongUsage);
          break;
      }
    }
    
    void OnAddBadlandsCommand(User user, string[] args)
    {
      var areas = new List<Area>();

      foreach (string arg in args)
      {
        Area area = Areas.Get(NormalizeAreaId(arg));

        if (area == null)
        {
          user.SendMessage(Messages.CannotSetBadlandsUnknownArea, arg);
          return;
        }

        if (area.Type != AreaType.Unclaimed)
        {
          user.SendMessage(Messages.CannotSetBadlandsNotUnclaimed, area.Id);
          return;
        }

        areas.Add(area);
      }

      Areas.AddBadlands(areas);

      var badlands = Areas.GetAllByType(AreaType.Badlands).Select(a => a.Id);
      user.SendMessage(Messages.BadlandsSet, FormatList(badlands));
    }

    void OnRemoveBadlandsCommand(User user, string[] args)
    {
      var areas = new List<Area>();

      foreach (string arg in args)
      {
        Area area = Areas.Get(NormalizeAreaId(arg));

        if (area == null)
        {
          user.SendMessage(Messages.CannotSetBadlandsUnknownArea, arg);
          return;
        }

        if (area.Type != AreaType.Badlands)
        {
          user.SendMessage(Messages.CannotSetBadlandsNotBadlands, area.Id);
          return;
        }

        areas.Add(area);
      }

      Areas.Unclaim(areas);

      var badlands = Areas.GetAllByType(AreaType.Badlands).Select(a => a.Id);
      user.SendMessage(Messages.BadlandsSet, FormatList(badlands));
    }

    void OnSetBadlandsCommand(User user, string[] args)
    {
      var areas = new List<Area>();

      foreach (string arg in args)
      {
        Area area = Areas.Get(NormalizeAreaId(arg));

        if (area == null)
        {
          user.SendMessage(Messages.CannotSetBadlandsUnknownArea, arg);
          return;
        }

        if (area.Type != AreaType.Unclaimed)
        {
          user.SendMessage(Messages.CannotSetBadlandsNotUnclaimed, area.Id);
          return;
        }

        areas.Add(area);
      }

      Areas.Unclaim(Areas.GetAllByType(AreaType.Badlands));
      Areas.AddBadlands(areas);

      var badlands = Areas.GetAllByType(AreaType.Badlands).Select(a => a.Id);
      user.SendMessage(Messages.BadlandsSet, FormatList(badlands));
    }
    
  }

}
