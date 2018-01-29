namespace Oxide.Plugins
{
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

        Instance.Areas.AddToTown(Name, User, area);
        Instance.PrintToChat(Messages.TownCreatedAnnouncement, Faction.Id, area.Name, area.Id);
        // TODO: History

        return true;
      }
    }
  }
}
