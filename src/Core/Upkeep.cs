namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    static class Upkeep
    {
      public static void CollectForAllFactions()
      {
        foreach (Faction faction in Instance.Factions.GetAll())
          Collect(faction);
      }

      public static void Collect(Faction faction)
      {
        Area[] areas = Instance.Areas.GetAllClaimedByFaction(faction);

        if (areas.Length == 0)
          return;

        if (faction.IsUpkeepPaid)
        {
          Instance.Log($"[UPKEEP] {faction.Id}: Upkeep not due until {faction.NextUpkeepPaymentTime}");
          return;
        }

        int amountOwed = faction.GetUpkeepPerPeriod();
        var hoursSincePaid = (int)DateTime.UtcNow.Subtract(faction.NextUpkeepPaymentTime).TotalHours;

        Instance.Log($"[UPKEEP] {faction.Id}: {hoursSincePaid} hours since upkeep paid, trying to collect {amountOwed} scrap for {areas.Length} area claims");

        if (faction.TaxChest != null)
        {
          ItemDefinition scrapDef = ItemManager.FindItemDefinition("scrap");
          List<Item> stacks = faction.TaxChest.inventory.FindItemsByItemID(scrapDef.itemid);
          if (Instance.TryCollectFromStacks(scrapDef, stacks, amountOwed))
          {
            faction.NextUpkeepPaymentTime = faction.NextUpkeepPaymentTime.AddHours(Instance.Options.Upkeep.CollectionPeriodHours);
            Instance.Log($"[UPKEEP] {faction.Id}: {amountOwed} scrap upkeep collected, next payment due {faction.NextUpkeepPaymentTime}");
            return;
          }
        }

        if (hoursSincePaid <= Instance.Options.Upkeep.GracePeriodHours)
        {
          Instance.Log($"[UPKEEP] {faction.Id}: Couldn't collect upkeep, but still within {Instance.Options.Upkeep.GracePeriodHours} hour grace period");
          return;
        }

        Area lostArea = areas.OrderBy(area => Instance.Areas.GetDepthInsideFriendlyTerritory(area)).First();

        Instance.Log($"[UPKEEP] {faction.Id}: Upkeep not paid in {hoursSincePaid} hours, seizing claim on {lostArea.Id}");
        Instance.PrintToChat(Messages.AreaClaimLostUpkeepNotPaidAnnouncement, faction.Id, lostArea.Id);

        Instance.Areas.Unclaim(lostArea);
      }
    }
  }
}
