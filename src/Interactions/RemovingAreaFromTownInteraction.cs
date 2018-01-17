namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    class RemovingAreaFromTownInteraction : Interaction
    {
      public Faction Faction { get; private set; }
      public Town Town { get; private set; }

      public RemovingAreaFromTownInteraction(Faction faction, Town town)
      {
        Faction = faction;
        Town = town;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        if (!Core.EnsureCanManageTowns(User, Faction) || !Core.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = Core.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        if (area.Type != AreaType.Town)
        {
          User.SendMessage(Messages.CannotRemoveFromTownNotPartOfTown, area.Id);
          return false;
        }

        Core.Areas.RemoveFromTown(area);
        User.SendMessage(Messages.AreaRemovedFromTown, area.Id, area.Name);

        return true;
      }
    }
  }
}
