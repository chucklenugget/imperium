namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTownExpandCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      Town town = Areas.GetTownByMayor(user);
      if (town == null)
      {
        user.SendChatMessage(Messages.NotMayorOfTown);
        return;
      }

      user.SendChatMessage(Messages.SelectTownCupboardToExpand, town.Name);
      user.BeginInteraction(new AddingAreaToTownInteraction(faction, town));
    }
  }
}
