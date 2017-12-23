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
        SendMessage(player, Messages.BadlandsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        SendMessage(player, Messages.BadlandsList, FormatList(Badlands.GetAll()), Options.BadlandsGatherBonus);
        return;
      }

      if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
      {
        SendMessage(player, Messages.CannotSetBadlandsNoPermission);
        return;
      }

      var areaIds = args.Skip(1).Select(arg => arg.ToUpper()).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          if (args.Length < 2)
            SendMessage(player, Messages.CannotSetBadlandsWrongUsage);
          else
            AddBadlands(player, areaIds);
          break;
        case "remove":
          if (args.Length < 2)
            SendMessage(player, Messages.CannotSetBadlandsWrongUsage);
          else
            RemoveBadlands(player, areaIds);
          break;
        case "set":
          if (args.Length < 2)
            SendMessage(player, Messages.CannotSetBadlandsWrongUsage);
          else
            SetBadlands(player, areaIds);
          break;
        case "clear":
          if (args.Length != 1)
            SendMessage(player, Messages.CannotSetBadlandsWrongUsage);
          else
            SetBadlands(player, new string[0]);
          break;
        default:
          SendMessage(player, Messages.CannotSetBadlandsWrongUsage);
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
          SendMessage(player, Messages.CannotSetBadlandsAreaClaimed, claim.AreaId, claim.FactionId);
          return;
        }
      }

      Badlands.Add(areaIds);
      SendMessage(player, Messages.BadlandsAdded, FormatList(areaIds), FormatList(Badlands.GetAll()));
    }

    void RemoveBadlands(BasePlayer player, string[] areaIds)
    {
      Badlands.Remove(areaIds);
      SendMessage(player, Messages.BadlandsRemoved, FormatList(areaIds), FormatList(Badlands.GetAll()));
    }

    void SetBadlands(BasePlayer player, string[] areaIds)
    {
      Badlands.Set(areaIds);
      SendMessage(player, Messages.BadlandsSet, FormatList(Badlands.GetAll()));
    }
    
    void AwardBadlandsBonusIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!Options.EnableBadlands) return;

      var player = entity as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);

      if (user.CurrentArea == null)
      {
        PrintWarning("Player gathered outside of a defined area. This shouldn't happen.");
        return;
      }

      if (Badlands.Contains(user.CurrentArea))
      {
        var bonus = (int)(item.amount * (Options.BadlandsGatherBonus / 100f));
        item.amount += bonus;
      }
    }
    
  }

}
