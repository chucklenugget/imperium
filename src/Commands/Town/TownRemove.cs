namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTownRemoveCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserCanManageTowns(user, faction))
        return;

      Town town = Areas.GetTownByMayor(user);
      if (town == null)
      {
        user.SendChatMessage(Messages.NotMayorOfTown);
        return;
      }

      user.SendChatMessage(Messages.SelectTownCupboardToRemove);
      user.BeginInteraction(new RemovingAreaFromTownInteraction(faction, town));
    }
  }
}
