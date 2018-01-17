namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    class CreatingTownInteraction : Interaction
    {
      public Faction Faction { get; private set; }
      public string Name { get; private set; }

      public CreatingTownInteraction(Faction faction, string name)
      {
        Faction = faction;
        Name = name;
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

        Core.Areas.AddToTown(Name, User, area);

        User.SendMessage(Messages.TownCreated, area.Name);
        Core.PrintToChat(Messages.TownCreatedAnnouncement, Faction.Id, area.Name, area.Id);

        return true;
      }
    }
  }
}
