namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTownDisbandCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      Town town = Areas.GetTownByMayor(user);
      if (town == null)
      {
        user.SendMessage(Messages.InteractionFailedNotMayorOfTown);
        return;
      }

      Areas.RemoveFromTown(town.Areas);

      user.SendMessage(Messages.TownDisbanded, town.Name);
      PrintToChat(Messages.TownDisbandedAnnouncement, faction.Id, town.Name);
    }
  }
}
