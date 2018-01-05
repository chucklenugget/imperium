namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  public partial class RustFactions
  {
    [ChatCommand("claim")]
    void OnClaimCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableAreaClaims)
      {
        user.SendMessage(Messages.AreaClaimsDisabled);
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
        case "delete":
          OnClaimDeleteCommand(user, restArguments);
          break;
        default:
          OnClaimHelpCommand(user);
          break;
      }
    }

    void OnClaimAddCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      user.SendMessage(Messages.SelectClaimCupboardToAdd);
      user.BeginInteraction(new AddingClaimInteraction(faction));
    }

    void OnClaimRemoveCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      user.SendMessage(Messages.SelectClaimCupboardToRemove);
      user.BeginInteraction(new RemovingClaimInteraction(faction));
    }

    void OnClaimHeadquartersCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      user.SendMessage(Messages.SelectClaimCupboardForHeadquarters);
      user.BeginInteraction(new SelectingHeadquartersInteraction(faction));
    }

    void OnClaimRenameCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      if (args.Length != 2)
      {
        user.SendMessage(Messages.CannotRenameAreaBadUsage);
        return;
      }

      var areaId = NormalizeAreaId(args[0]);
      var name = NormalizeName(args[1]);

      if (name == null || name.Length < Options.MinAreaNameLength)
      {
        user.SendMessage(Messages.CannotRenameAreaBadName, areaId, Options.MinAreaNameLength);
        return;
      }

      Area area = Areas.Get(areaId);

      if (area == null)
      {
        user.SendMessage(Messages.CannotRenameAreaUnknownAreaId, areaId);
        return;
      }

      if (area.FactionId != faction.Id)
      {
        user.SendMessage(Messages.CannotRenameAreaNotClaimedByFaction, area.Id);
        return;
      }

      if (area.Type == AreaType.Town)
      {
        user.SendMessage(Messages.CannotRenameAreaIsTown, area.Id, area.Name);
        return;
      }

      area.Name = name;
      user.SendMessage(Messages.AreaRenamed, area.Id, area.Name);
    }

    void OnClaimCostCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByUser(user);

      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMemberOfFaction);
        return;
      }

      if (faction.MemberSteamIds.Count < Options.MinFactionMembers)
      {
        user.SendMessage(Messages.InteractionFailedFactionTooSmall, Options.MinFactionMembers);
        return;
      }

      if (args.Length > 1)
      {
        user.SendMessage(Messages.CannotShowClaimCostBadUsage);
        return;
      }

      Area area;
      if (args.Length == 0)
        area = user.CurrentArea;
      else
        area = Areas.Get(NormalizeAreaId(args[0]));

      if (area == null)
      {
        user.SendMessage(Messages.CannotShowClaimCostBadUsage);
        return;
      }

      if (area.Type == AreaType.Badlands)
      {
        user.SendMessage(Messages.CannotClaimAreaIsBadlands, area.Id);
        return;
      }
      else if (area.Type != AreaType.Wilderness)
      {
        user.SendMessage(Messages.CannotClaimAreaIsClaimed, area.Id, area.FactionId);
        return;
      }

      int cost = area.GetClaimCost(faction);
      user.SendMessage(Messages.ClaimCost, area.Id, faction.Id, cost);
    }

    void OnClaimUpkeepCommand(User user)
    {
      if (!Options.EnableUpkeep)
      {
        user.SendMessage(Messages.UpkeepDisabled);
        return;
      }

      Faction faction = Factions.GetByUser(user);

      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMemberOfFaction);
        return;
      }

      if (faction.MemberSteamIds.Count < Options.MinFactionMembers)
      {
        user.SendMessage(Messages.InteractionFailedFactionTooSmall, Options.MinFactionMembers);
        return;
      }

      Area[] areas = Areas.GetAllClaimedByFaction(faction);

      if (areas.Length == 0)
      {
        user.SendMessage(Messages.NoAreasClaimed);
        return;
      }

      int upkeep = faction.GetUpkeepPerPeriod();
      var nextPaymentHours = (int)faction.NextUpkeepPaymentTime.Subtract(DateTime.UtcNow).TotalHours;

      if (nextPaymentHours > 0)
        user.SendMessage(Messages.UpkeepCost, upkeep, areas.Length, faction.Id, nextPaymentHours);
      else
        user.SendMessage(Messages.UpkeepCostOverdue, upkeep, areas.Length, faction.Id, nextPaymentHours);
    }

    void OnClaimShowCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendMessage(Messages.CannotShowClaimBadUsage);
        return;
      }

      Area area = Areas.Get(NormalizeAreaId(args[0]));

      switch (area.Type)
      {
        case AreaType.Badlands:
          user.SendMessage(Messages.AreaIsBadlands, area.Id);
          return;
        case AreaType.Claimed:
          user.SendMessage(Messages.AreaIsClaimed, area.Id, area.FactionId);
          return;
        case AreaType.Headquarters:
          user.SendMessage(Messages.AreaIsHeadquarters, area.Id, area.FactionId);
          return;
        case AreaType.Town:
          user.SendMessage(Messages.AreaIsTown, area.Id, area.Name, area.FactionId);
          return;
        default:
          user.SendMessage(Messages.AreaIsWilderness, area.Id);
          return;
      }
    }

    void OnClaimListCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendMessage(Messages.CannotListClaimsBadUsage);
        return;
      }

      string factionId = NormalizeFactionId(args[0]);
      Faction faction = Factions.Get(factionId);

      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedUnknownFaction, factionId);
        return;
      }

      Area[] areas = Areas.GetAllClaimedByFaction(faction);
      Area headquarters = areas.FirstOrDefault(a => a.Type == AreaType.Headquarters);

      var sb = new StringBuilder();

      if (areas.Length == 0)
      {
        sb.AppendFormat(String.Format("<color=#ffd479>[{0}]</color> has no land holdings.", factionId));
      }
      else
      {
        float percentageOfMap = (areas.Length / (float)Areas.Count) * 100;
        sb.AppendLine(String.Format("<color=#ffd479>[{0}] owns {1} tiles ({2:F2}% of the known world)</color>", faction.Id, areas.Length, percentageOfMap));
        sb.AppendLine(String.Format("Headquarters: {0}", (headquarters == null) ? "Unknown" : headquarters.Id));
        sb.AppendLine(String.Format("Areas claimed: {0}", FormatList(areas.Select(a => a.Id))));
      }

      user.SendMessage(sb);
    }

    void OnClaimHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/claim</color>: Add a claim for your faction");
      sb.AppendLine("  <color=#ffd479>/claim hq</color>: Select your faction's headquarters");
      sb.AppendLine("  <color=#ffd479>/claim remove</color>: Remove a claim for your faction (no undo!)");
      sb.AppendLine("  <color=#ffd479>/claim rename XY \"NAME\"</color>: Rename an area claimed by your faction");
      sb.AppendLine("  <color=#ffd479>/claim show XY</color>: Show who owns an area");
      sb.AppendLine("  <color=#ffd479>/claim list FACTION</color>: List all areas claimed for a faction");
      sb.AppendLine("  <color=#ffd479>/claim cost [XY]</color>: Show the cost for your faction to claim an area");

      if (!Options.EnableUpkeep)
        sb.AppendLine("  <color=#ffd479>/claim upkeep</color>: Show information about upkeep costs for your faction");

      sb.AppendLine("  <color=#ffd479>/claim help</color>: Prints this message");

      if (user.HasPermission(PERM_CHANGE_CLAIMS))
      {
        sb.AppendLine("Admin commands:");
        sb.AppendLine("  <color=#ffd479>/claim delete XY [XY XY XY...]</color>: Remove the claim on the specified areas (no undo!)");
      }

      user.SendMessage(sb);
    }

    void OnClaimDeleteCommand(User user, string[] args)
    {
      if (args.Length == 0)
      {
        user.SendMessage(Messages.CannotDeleteClaimsBadUsage);
        return;
      }

      if (!user.HasPermission(PERM_CHANGE_CLAIMS))
      {
        user.SendMessage(Messages.CannotDeleteClaimsNoPermission);
        return;
      }

      var areas = new List<Area>();
      foreach (string arg in args)
      {
        Area area = Areas.Get(NormalizeAreaId(arg));

        if (area.Type == AreaType.Badlands)
        {
          user.SendMessage(Messages.CannotDeleteClaimsAreaIsBadlands, area.Id);
          return;
        }

        if (area.Type == AreaType.Wilderness)
        {
          user.SendMessage(Messages.CannotDeleteClaimsAreaNotUnclaimed, area.Id);
          return;
        }

        areas.Add(area);
      }

      foreach (Area area in areas)
      {
        PrintToChat(Messages.AreaClaimDeletedAnnouncement, area.FactionId, area.Id);
        History.Record(EventType.AreaClaimDeleted, area, null, user);
      }

      Areas.Unclaim(areas);
    }

    bool EnsureCanChangeFactionClaims(User user, Faction faction)
    {
      if (faction == null || !faction.IsLeader(user))
      {
        user.SendMessage(Messages.InteractionFailedNotLeaderOfFaction);
        return false;
      }

      if (faction.MemberSteamIds.Count < Options.MinFactionMembers)
      {
        user.SendMessage(Messages.InteractionFailedFactionTooSmall, Options.MinFactionMembers);
        return false;
      }

      return true;
    }

    bool EnsureCanUseCupboardAsClaim(User user, BuildingPrivlidge cupboard)
    {
      if (cupboard == null)
      {
        user.SendMessage(Messages.SelectingCupboardFailedInvalidTarget);
        return false;
      }

      if (!cupboard.IsAuthed(user.Player))
      {
        user.SendMessage(Messages.SelectingCupboardFailedNotAuthorized);
        return false;
      }

      return true;
    }
  }
}
