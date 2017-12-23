namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using UnityEngine;

  public partial class RustFactions
  {
    [ChatCommand("town")]
    void OnTownCommand(BasePlayer player, string command, string[] args)
    {
      if (!Options.EnableTowns)
      {
        SendMessage(player, Messages.TownsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        OnTownHelpCommand(player);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "create":
          OnTownCreateCommand(player, restArguments);
          break;
        case "remove":
          OnTownRemoveCommand(player);
          break;
        case "list":
          OnTownListCommand(player);
          break;
        case "help":
        default:
          OnTownHelpCommand(player);
          break;
      }
    }

    void OnTownCreateCommand(BasePlayer player, string[] args)
    {
      User user = Users.Get(player);

      if (!EnsureCanManageTowns(player))
        return;

      if (args.Length == 0)
      {
        SendMessage(player, Messages.CannotCreateTownWrongUsage);
        return;
      }

      Town town = Towns.GetByMayor(player);
      if (town != null)
      {
        SendMessage(player, Messages.CannotCreateTownAlreadyMayor, town.Name);
        return;
      }

      var name = args[0].Trim();

      SendMessage(player, Messages.SelectTownCupboardToAdd, name);
      user.PendingInteraction = new CreatingTownInteraction(name);
    }

    void OnTownRemoveCommand(BasePlayer player)
    {
      User user = Users.Get(player);

      if (!EnsureCanManageTowns(player))
        return;

      SendMessage(player, Messages.SelectTownCupboardToRemove);
      user.PendingInteraction = new RemovingTownInteraction();
    }
    
    void OnTownListCommand(BasePlayer player)
    {
      Town[] towns = Towns.GetAll();
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
          var cupboard = BaseNetworkable.serverEntities.Find(town.CupboardId) as BuildingPrivlidge;
          float distance = Vector3.Distance(player.transform.position, cupboard.transform.position) / 1000;
          int population = cupboard.authorizedPlayers.Count;
          sb.AppendLine(String.Format("<color=#ffd479>{0}:</color> {1} ({2:0.00}km), population {3}", town.Name, town.AreaId, distance, population));
        }
      }

      SendMessage(player, sb);
    }

    void OnTownHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/town list</color>: Lists all towns on the island");
      sb.AppendLine("  <color=#ffd479>/town help</color>: Prints this message");

      if (permission.UserHasPermission(player.UserIDString, PERM_CHANGE_TOWNS))
      {
        sb.AppendLine("Mayor commands:");
        sb.AppendLine("  <color=#ffd479>/town create \"NAME\"</color>: Create a town");
        sb.AppendLine("  <color=#ffd479>/town remove</color>: Remove a town that you created (no undo!)");
      }

      SendMessage(player, sb);
    }

    bool TryCreateTown(CreatingTownInteraction interaction, BasePlayer player, HitInfo hit)
    {
      User user = Users.Get(player);
      var cupboard = hit.HitEntity as BuildingPrivlidge;

      if (!EnsureCanManageTowns(player) || !EnsureCanUseCupboardAsClaim(player, cupboard))
        return false;

      Area area = user.CurrentArea;
      if (area == null)
      {
        PrintWarning("Player attempted to create town but wasn't in an area. This shouldn't happen.");
        return false;
      }

      if (Badlands.Contains(area.Id))
      {
        SendMessage(player, Messages.CannotCreateTownAreaIsBadlands);
        return false;
      }

      Town existingTown = Towns.Get(area);
      if (existingTown != null)
      {
        SendMessage(player, Messages.CannotCreateTownOneAlreadyExists, area.Id, existingTown.Name);
        return false;
      }

      Town town = new Town(area.Id, interaction.Name, player.userID, cupboard.net.ID);
      SendMessage(player, Messages.TownCreated, town.Name);
      PrintToChat("<color=#00ff00ff>TOWN FOUNDED:</color> The town of {0} has been founded in {1}.", town.Name, area.Id);

      Towns.Add(town);

      return true;
    }

    bool TryRemoveTown(BasePlayer player, HitInfo hit)
    {
      var cupboard = hit.HitEntity as BuildingPrivlidge;

      if (!EnsureCanManageTowns(player) || !EnsureCanUseCupboardAsClaim(player, cupboard))
        return false;

      Town town = Towns.GetByCupboard(cupboard.net.ID);

      if (town == null)
      {
        SendMessage(player, Messages.SelectingTownCupboardFailedNotTownCupboard);
        return false;
      }

      if (town.MayorId != player.userID)
      {
        SendMessage(player, Messages.SelectingTownCupboardFailedNotMayor);
        return false;
      }

      SendMessage(player, Messages.TownRemoved, town.Name);
      PrintToChat("<color=#ff0000ff>TOWN DISBANDED:</color> The town {0} has been disbanded!", town.Name);
      Towns.Remove(town);

      return true;
    }

    bool EnsureCanManageTowns(BasePlayer player)
    {
      if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_TOWNS))
      {
        SendMessage(player, Messages.CannotManageTownsNoPermission);
        return false;
      }

      return true;
    }

    bool EnsureCanUseCupboardAsTown(BasePlayer player, BuildingPrivlidge cupboard)
    {
      if (cupboard == null)
      {
        SendMessage(player, Messages.SelectingCupboardFailedInvalidTarget);
        return false;
      }

      if (!cupboard.IsAuthed(player))
      {
        SendMessage(player, Messages.SelectingCupboardFailedNotAuthorized);
        return false;
      }

      return true;
    }

  }

}
