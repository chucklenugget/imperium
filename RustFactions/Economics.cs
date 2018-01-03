namespace Oxide.Plugins
{
  public partial class RustFactions
  {

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

  }

}
