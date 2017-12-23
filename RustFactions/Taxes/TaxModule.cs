namespace Oxide.Plugins
{
  using System;
  using System.Linq;
  using System.Text;

  public partial class RustFactions
  {
    [ChatCommand("tax")]
    void OnTaxCommand(BasePlayer player, string command, string[] args)
    {
      if (!Options.EnableTaxation)
      {
        SendMessage(player, Messages.TaxationDisabled);
        return;
      };

      if (args.Length == 0)
      {
        OnTaxHelpCommand(player);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "chest":
          OnTaxChestCommand(player);
          break;
        case "set":
          OnTaxSetCommand(player, restArguments);
          break;
        case "help":
        default:
          OnTaxHelpCommand(player);
          break;
      }
    }

    void OnTaxChestCommand(BasePlayer player)
    {
      User user = Users.Get(player);
      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        SendMessage(player, Messages.CannotSelectTaxChestNotMemberOfFaction);
        return;
      }

      if (!faction.IsLeader(player))
      {
        SendMessage(player, Messages.CannotSelectTaxChestNotFactionLeader);
        return;
      }

      SendMessage(player, Messages.SelectTaxChest);
      user.PendingInteraction = new SelectingTaxChestInteraction();
    }

    void OnTaxSetCommand(BasePlayer player, string[] args)
    {
      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        SendMessage(player, Messages.CannotSetTaxRateNotMemberOfFaction);
        return;
      }

      if (!faction.IsLeader(player))
      {
        SendMessage(player, Messages.CannotSetTaxRateNotFactionLeader);
        return;
      }

      int taxRate;
      try
      {
        taxRate = Convert.ToInt32(args[0]);
      }
      catch
      {
        SendMessage(player, Messages.CannotSetTaxRateInvalidValue, Options.MaxTaxRate);
        return;
      }

      if (taxRate < 0 || taxRate > Options.MaxTaxRate)
      {
        SendMessage(player, Messages.CannotSetTaxRateInvalidValue, Options.MaxTaxRate);
        return;
      }

      Taxes.SetTaxRate(faction.Id, taxRate);
      SendMessage(player, Messages.SetTaxRateSuccessful, faction.Id, taxRate);
    }

    void OnTaxHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/tax set NN</color>: Set the tax rate for your faction");
      sb.AppendLine("  <color=#ffd479>/tax container</color>: Select a container to receive the taxed resources");
      sb.AppendLine("  <color=#ffd479>/tax help</color>: Prints this message");

      SendMessage(player, sb);
    }

    bool TrySetTaxChest(BasePlayer player, HitInfo hit)
    {
      var container = hit.HitEntity as StorageContainer;

      if (container == null)
      {
        SendMessage(player, Messages.SelectingTaxChestFailedInvalidTarget);
        return false;
      }

      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        // This covers the unlikely case the player is removed from the faction before they finish selecting.
        SendMessage(player, Messages.CannotSelectTaxChestNotMemberOfFaction);
        return false;
      }

      TaxPolicy policy = Taxes.SetTaxChest(faction.Id, container);
      SendMessage(player, Messages.SelectingTaxChestSucceeded, policy.TaxRate, faction.Id);
      TaxChests[container.net.ID] = container;

      Puts($"Tax chest for {faction.Id} set to {container.net.ID}");

      return true;
    }
    
    void ChargeTaxIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!Options.EnableTaxation) return;

      var player = entity as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);

      if (user.CurrentArea == null)
      {
        PrintWarning("Player gathered outside of a defined area. This shouldn't happen.");
        return;
      }

      Claim claim = Claims.Get(user.CurrentArea);
      if (claim == null) return;

      TaxPolicy policy = Taxes.Get(claim);
      if (policy != null && policy.TaxChestId != null && policy.TaxRate > 0)
      {
        StorageContainer container;
        if (TaxChests.TryGetValue((uint)policy.TaxChestId, out container) && !container.inventory.IsFull())
        {
          ItemDefinition itemDef = ItemManager.FindItemDefinition(item.info.itemid);
          if (itemDef != null)
          {
            var tax = (int)(item.amount * (policy.TaxRate / 100f));
            item.amount -= tax;
            container.inventory.AddItem(itemDef, tax);
          }
        }
      }
    }    
  }

}
