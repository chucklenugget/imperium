namespace Oxide.Plugins
{
  public partial class Imperium
  {
    class SelectingTaxChestInteraction : Interaction
    {
      public Faction Faction { get; private set; }

      public SelectingTaxChestInteraction(Faction faction)
      {
        Faction = faction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var container = hit.HitEntity as StorageContainer;

        if (container == null)
        {
          User.SendChatMessage(Messages.SelectingTaxChestFailedInvalidTarget);
          return false;
        }

        Instance.Factions.SetTaxChest(Faction, container);
        User.SendChatMessage(Messages.SelectingTaxChestSucceeded, Faction.TaxRate * 100, Faction.Id);

        return true;
      }
    }
  }
}
