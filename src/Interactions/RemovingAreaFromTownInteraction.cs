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

        if (!Instance.EnsureCanManageTowns(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        if (area.Type != AreaType.Town)
        {
          User.SendChatMessage(Messages.AreaNotPartOfTown, area.Id);
          return false;
        }

        User.SendChatMessage(Messages.AreaRemovedFromTown, area.Id, area.Name);
        Instance.Log($"{Util.Format(User)} removed {area.Id} from the town of {area.Name} on behalf of {Faction.Id}");

        Instance.Areas.RemoveFromTown(area);
        return true;
      }
    }
  }
}
