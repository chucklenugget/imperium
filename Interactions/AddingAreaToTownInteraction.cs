namespace Oxide.Plugins
{
  using System;

  public partial class RustFactions
  {
    class AddingAreaToTownInteraction : Interaction
    {
      public Faction Faction { get; private set; }
      public Town Town { get; private set; }

      public AddingAreaToTownInteraction(Faction faction, Town town)
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

        if (area.Type == AreaType.Headquarters)
        {
          User.SendMessage(Messages.CannotAddToTownAreaIsHeadquarters, area.Id);
          return false;
        }

        if (area.Type == AreaType.Town)
        {
          User.SendMessage(Messages.CannotAddToTownOneAlreadyExists, area.Id, area.Name);
          return false;
        }

        Core.Areas.AddToTown(Town.Name, User, area);
        User.SendMessage(Messages.AreaAddedToTown, area.Id, area.Name);

        return true;
      }
    }
  }
}
