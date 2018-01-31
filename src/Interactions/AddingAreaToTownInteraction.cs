namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
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

        if (!Instance.EnsureCanManageTowns(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        if (area.Type == AreaType.Headquarters)
        {
          User.SendChatMessage(Messages.CannotAddToTownAreaIsHeadquarters, area.Id);
          return false;
        }

        if (area.Type == AreaType.Town)
        {
          User.SendChatMessage(Messages.CannotAddToTownOneAlreadyExists, area.Id, area.Name);
          return false;
        }

        User.SendChatMessage(Messages.AreaAddedToTown, area.Id, Town.Name);
        Instance.Log($"{Util.Format(User)} added {area.Id} to town {Town.Name}");

        Instance.Areas.AddToTown(Town.Name, User, area);
        return true;
      }
    }
  }
}
