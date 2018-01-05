namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class RustFactions
  {
    void CollectUpkeepForAllFactions()
    {
      foreach (Faction faction in Factions.GetAll())
        CollectUpkeep(faction);
    }

    void CollectUpkeep(Faction faction)
    {
      Area[] areas = Areas.GetAllClaimedByFaction(faction);

      if (areas.Length == 0)
        return;

      if (faction.IsUpkeepPaid)
      {
        Puts($"{faction.Id}: Upkeep not due until {faction.NextUpkeepPaymentTime}");
        return;
      }

      int amountOwed = faction.GetUpkeepPerPeriod();
      var hoursSincePaid = (int)DateTime.UtcNow.Subtract(faction.NextUpkeepPaymentTime).TotalHours;

      Puts($"{faction.Id}: {hoursSincePaid} hours since upkeep paid, trying to collect {amountOwed} scrap upkeep for {areas.Length} area claims");

      if (faction.TaxChest != null)
      {
        ItemDefinition scrapDef = ItemManager.FindItemDefinition("scrap");
        List<Item> stacks = faction.TaxChest.inventory.FindItemsByItemID(scrapDef.itemid);
        if (TryCollectFromStacks(scrapDef, stacks, amountOwed))
        {
          faction.NextUpkeepPaymentTime = faction.NextUpkeepPaymentTime.AddHours(Options.UpkeepCollectionPeriodHours);
          Puts($"{faction.Id}: {amountOwed} scrap upkeep collected, next payment due {faction.NextUpkeepPaymentTime}");
          return;
        }
      }

      if (hoursSincePaid <= Options.UpkeepGracePeriodHours)
      {
        Puts($"{faction.Id}: Couldn't collect upkeep, but still within {Options.UpkeepGracePeriodHours} hour grace period");
        return;
      }

      Area lostArea = areas.OrderBy(area => Areas.GetDepthInsideFriendlyTerritory(area)).First();
      Areas.Unclaim(lostArea);

      Puts($"{faction.Id}: upkeep not paid in {hoursSincePaid} hours, seizing claim on {lostArea.Id}");
      PrintToChat(Messages.AreaClaimLostUpkeepNotPaidAnnouncement, faction.Id, lostArea.Id);
    }

    void ChargeTaxIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!Options.EnableTaxation) return;

      var player = entity as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);
      if (user == null) return;

      Area area = user.CurrentArea;
      if (area == null)
      {
        PrintWarning("Player gathered outside of a defined area. This shouldn't happen.");
        return;
      }

      if (!area.IsTaxableClaim)
        return;

      Faction faction = Factions.Get(area.FactionId);

      if (faction.CanCollectTaxes && !faction.TaxChest.inventory.IsFull())
      {
        ItemDefinition itemDef = ItemManager.FindItemDefinition(item.info.itemid);
        if (itemDef != null)
        {
          var tax = (int)(item.amount * faction.TaxRate);
          item.amount -= tax;
          faction.TaxChest.inventory.AddItem(itemDef, tax);
        }
      }
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

      if (user.CurrentArea.Type == AreaType.Badlands)
      {
        var bonus = (int)(item.amount * Options.BadlandsGatherBonus);
        item.amount += bonus;
      }
    }

    bool TryCollectFromStacks(ItemDefinition itemDef, IEnumerable<Item> stacks, int amount)
    {
      if (stacks.Sum(item => item.amount) < amount)
        return false;

      int amountRemaining = amount;
      var dirtyContainers = new HashSet<ItemContainer>();

      foreach (Item stack in stacks)
      {
        var amountToTake = Math.Min(stack.amount, amountRemaining);

        stack.amount -= amountToTake;
        amountRemaining -= amountToTake;

        dirtyContainers.Add(stack.GetRootContainer());

        if (stack.amount == 0)
          stack.RemoveFromContainer();

        if (amountRemaining == 0)
          break;
      }

      foreach (ItemContainer container in dirtyContainers)
        container.MarkDirty();

      return true;
    }
  }

}
