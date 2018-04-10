namespace Oxide.Plugins
{
  public partial class Imperium
  {
    static class Taxes
    {
      public static void ProcessTaxesIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
      {
        if (!Instance.Options.Taxes.Enabled)
          return;

        var player = entity as BasePlayer;
        if (player == null)
          return;

        User user = Instance.Users.Get(player);
        if (user == null)
          return;

        Area area = user.CurrentArea;
        if (area == null || !area.IsClaimed)
          return;

        if (user.Faction != null && area.FactionId == user.Faction.Id)
          return;

        Faction faction = Instance.Factions.Get(area.FactionId);
        if (!faction.CanCollectTaxes || faction.TaxChest.inventory.IsFull())
          return;

        ItemDefinition itemDef = ItemManager.FindItemDefinition(item.info.itemid);
        if (itemDef == null)
          return;

        int bonus = (int)(item.amount * Instance.Options.Taxes.ClaimedLandGatherBonus);
        var tax = (int)(item.amount * faction.TaxRate);

        faction.TaxChest.inventory.AddItem(itemDef, tax + bonus);
        item.amount -= tax;
      }

      public static void AwardBadlandsBonusIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
      {
        if (!Instance.Options.Badlands.Enabled)
          return;

        var player = entity as BasePlayer;
        if (player == null) return;

        User user = Instance.Users.Get(player);

        if (user.CurrentArea == null)
        {
          Instance.PrintWarning("Player gathered outside of a defined area. This shouldn't happen.");
          return;
        }

        if (user.CurrentArea.Type == AreaType.Badlands)
        {
          var bonus = (int)(item.amount * Instance.Options.Taxes.BadlandsGatherBonus);
          item.amount += bonus;
        }
      }
    }
  }
}
