namespace Oxide.Plugins
{
  using System;

  public partial class RustFactions
  {
    [ChatCommand("taxchest")]
    void OnTaxChestCommand(BasePlayer player, string command, string[] args)
    {
      if (!Options.EnableTaxation)
      {
        SendMessage(player, "TaxationDisabled");
        return;
      };

      PlayerInteractionState playerState = PlayerInteractionStates.Get(player);
      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        SendMessage(player, "CannotSelectTaxChestNotMemberOfFaction");
        return;
      }

      if (!faction.IsLeader(player))
      {
        SendMessage(player, "CannotSelectTaxChestNotFactionLeader");
        return;
      }

      if (playerState == PlayerInteractionState.SelectingTaxChest)
      {
        SendMessage(player, "SelectingTaxChestCanceled");
        PlayerInteractionStates.Reset(player);
      }
      else
      {
        SendMessage(player, "SelectTaxChest");
        PlayerInteractionStates.Set(player, PlayerInteractionState.SelectingTaxChest);
      }
    }

    [ChatCommand("taxrate")]
    void OnTaxRateCommand(BasePlayer player, string command, string[] args)
    {
      if (!Options.EnableTaxation)
      {
        SendMessage(player, "TaxationDisabled");
        return;
      };

      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        SendMessage(player, "CannotSetTaxRateNotMemberOfFaction");
        return;
      }

      if (!faction.IsLeader(player))
      {
        SendMessage(player, "CannotSetTaxRateNotFactionLeader");
        return;
      }

      int taxRate;
      try
      {
        taxRate = Convert.ToInt32(args[0]);
      }
      catch
      {
        SendMessage(player, "CannotSetTaxRateInvalidValue", Options.MaxTaxRate);
        return;
      }

      if (taxRate < 0 || taxRate > Options.MaxTaxRate)
      {
        SendMessage(player, "CannotSetTaxRateInvalidValue", Options.MaxTaxRate);
        return;
      }

      TaxPolicies.SetTaxRate(faction.Id, taxRate);
      SendMessage(player, "SetTaxRateSuccessful", faction.Id, taxRate);
    }

    bool TrySetTaxChest(BasePlayer player, HitInfo hit)
    {
      var container = hit.HitEntity as StorageContainer;

      if (container == null)
      {
        SendMessage(player, "SelectingTaxChestFailedInvalidTarget");
        return false;
      }

      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        // This covers the unlikely case the player is removed from the faction before they finish selecting.
        SendMessage(player, "CannotSelectTaxChestNotMemberOfFaction");
        return false;
      }

      TaxPolicies.SetTaxChest(faction.Id, container);
      SendMessage(player, "SelectingTaxChestSucceeded", faction.Id);
      TaxChests[container.net.ID] = container;

      Puts($"Tax chest for {faction.Id} set to {container.net.ID}");

      return true;
    }
    
    void ChargeTaxIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!Options.EnableTaxation) return;

      var player = entity as BasePlayer;
      if (player == null) return;

      Area area;
      if (!PlayersInAreas.TryGetValue(player.userID, out area))
      {
        PrintWarning("Player gathered outside of a defined area. This shouldn't happen.");
        return;
      }

      Claim claim = Claims.Get(area);
      if (claim == null) return;

      TaxPolicy policy = TaxPolicies.Get(claim);
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
