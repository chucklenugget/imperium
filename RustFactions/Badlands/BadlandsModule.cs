namespace Oxide.Plugins
{
  using System.Linq;

  public partial class RustFactions
  {
    [ChatCommand("badlands")]
    void OnBadlandsCommand(BasePlayer player, string command, string[] args)
    {
      if (!Options.EnableBadlands)
      {
        SendMessage(player, "BadlandsDisabled");
        return;
      }

      if (args.Length == 0)
      {
        SendMessage(player, "BadlandsList", FormatList(Badlands.GetAll()), Options.BadlandsGatherBonus);
        return;
      }

      var areaIds = args.Skip(1).Select(arg => arg.ToUpper()).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
            SendMessage(player, "CannotSetBadlandsNoPermission");
          else if (args.Length < 2)
            SendMessage(player, "CannotSetBadlandsWrongUsage");
          else
            AddBadlands(player, areaIds);
          break;
        case "remove":
          if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
            SendMessage(player, "CannotSetBadlandsNoPermission");
          else if (args.Length < 2)
            SendMessage(player, "CannotSetBadlandsWrongUsage");
          else
            RemoveBadlands(player, areaIds);
          break;
        case "set":
          if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
            SendMessage(player, "CannotSetBadlandsNoPermission");
          else if (args.Length < 2)
            SendMessage(player, "CannotSetBadlandsWrongUsage");
          else
            SetBadlands(player, areaIds);
          break;
        case "clear":
          if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
            SendMessage(player, "CannotSetBadlandsNoPermission");
          else if (args.Length != 1)
            SendMessage(player, "CannotSetBadlandsWrongUsage");
          else
            SetBadlands(player, new string[0]);
          break;
        default:
          SendMessage(player, "CannotSetBadlandsWrongUsage");
          break;
      }
    }
    
    void AddBadlands(BasePlayer player, string[] areaIds)
    {
      foreach (string areaId in areaIds)
      {
        Claim claim = Claims.Get(areaId);
        if (claim != null)
        {
          SendMessage(player, "CannotSetBadlandsAreaClaimed", claim.AreaId, claim.FactionId);
          return;
        }
      }

      Badlands.Add(areaIds);
      SendMessage(player, "BadlandsAdded", FormatList(areaIds), FormatList(Badlands.GetAll()));
    }

    void RemoveBadlands(BasePlayer player, string[] areaIds)
    {
      Badlands.Remove(areaIds);
      SendMessage(player, "BadlandsRemoved", FormatList(areaIds), FormatList(Badlands.GetAll()));
    }

    void SetBadlands(BasePlayer player, string[] areaIds)
    {
      Badlands.Set(areaIds);
      SendMessage(player, "BadlandsSet", FormatList(Badlands.GetAll()));
    }
    
    void AwardBadlandsBonusIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!Options.EnableBadlands) return;

      var player = entity as BasePlayer;
      if (player == null) return;

      Area area;
      if (!PlayersInAreas.TryGetValue(player.userID, out area))
      {
        PrintWarning("Player gathered outside of a defined area. This shouldn't happen.");
        return;
      }

      if (Badlands.Contains(area.Id))
      {
        var bonus = (int)(item.amount * (Options.BadlandsGatherBonus / 100f));
        item.amount += bonus;
      }
    }
    
  }

}
