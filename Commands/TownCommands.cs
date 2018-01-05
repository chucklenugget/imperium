namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using UnityEngine;

  public partial class RustFactions
  {
    [ChatCommand("towns")]
    void OnTownsCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableTowns)
      {
        user.SendMessage(Messages.TownsDisabled);
        return;
      }

      OnTownListCommand(user);
    }

    [ChatCommand("town")]
    void OnTownCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableTowns)
      {
        user.SendMessage(Messages.TownsDisabled);
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

    void OnTownCreateCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      if (args.Length == 0)
      {
        user.SendMessage(Messages.CannotCreateTownWrongUsage);
        return;
      }

      Town town = Areas.GetTownByMayor(user);
      if (town != null)
      {
        user.SendMessage(Messages.CannotCreateTownAlreadyMayor, town.Name);
        return;
      }

      var name = NormalizeName(args[0]);

      town = Areas.GetTown(name);
      if (town != null)
      {
        user.SendMessage(Messages.CannotCreateTownSameNameAlreadyExists, town.Name);
        return;
      }

      user.SendMessage(Messages.SelectTownCupboardToCreate, name);
      user.BeginInteraction(new CreatingTownInteraction(faction, name));
    }

    void OnTownExpandCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      Town town = Areas.GetTownByMayor(user);
      if (town == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMayorOfTown);
        return;
      }

      user.SendMessage(Messages.SelectTownCupboardToExpand, town.Name);
      user.BeginInteraction(new AddingAreaToTownInteraction(faction, town));
    }

    void OnTownRemoveCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      Town town = Areas.GetTownByMayor(user);
      if (town == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMayorOfTown);
        return;
      }

      user.SendMessage(Messages.SelectTownCupboardToRemove);
      user.BeginInteraction(new RemovingAreaFromTownInteraction(faction, town));
    }

    void OnTownDisbandCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      Town town = Areas.GetTownByMayor(user);
      if (town == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMayorOfTown);
        return;
      }

      Areas.RemoveFromTown(town.Areas);

      user.SendMessage(Messages.TownDisbanded, town.Name);
      PrintToChat(Messages.TownDisbandedAnnouncement, faction.Id, town.Name);
    }

    void OnTownListCommand(User user)
    {
      Town[] towns = Areas.GetAllTowns();
      var sb = new StringBuilder();

      if (towns.Length == 0)
      {
        sb.AppendFormat(String.Format("No towns have been founded."));
      }
      else
      {
        sb.AppendLine(String.Format("<color=#ffd479>There are {0} towns on the island:</color>", towns.Length));
        foreach (Town town in towns)
        {
          var areaIds = town.Areas.Select(area => area.Id);
          float distance = town.GetDistanceFromEntity(user.Player);
          int population = town.GetPopulation();
          sb.AppendLine(String.Format("  <color=#ffd479>{0}:</color> {1:0.00}km ({2}), population {3}", town.Name, distance, FormatList(areaIds), population));
        }
      }

      user.SendMessage(sb);
    }

    void OnTownHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/town list</color> (or <color=#ffd479>/towns</color>): Lists all towns on the island");
      sb.AppendLine("  <color=#ffd479>/town help</color>: Prints this message");

      if (user.HasPermission(PERM_CHANGE_TOWNS))
      {
        sb.AppendLine("Mayor commands:");
        sb.AppendLine("  <color=#ffd479>/town create \"NAME\"</color>: Create a new town");
        sb.AppendLine("  <color=#ffd479>/town expand</color>: Add an area to your town");
        sb.AppendLine("  <color=#ffd479>/town remove</color>: Remove an area from your town");
        sb.AppendLine("  <color=#ffd479>/town disband</color>: Disband your town immediately (no undo!)");
      }

      user.SendMessage(sb);
    }

    bool EnsureCanManageTowns(User user, Faction faction)
    {
      if (!user.HasPermission(PERM_CHANGE_TOWNS))
      {
        user.SendMessage(Messages.CannotManageTownsNoPermission);
        return false;
      }

      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMemberOfFaction);
        return false;
      }

      return true;
    }

    bool EnsureCanUseCupboardAsTown(User user, BuildingPrivlidge cupboard)
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
