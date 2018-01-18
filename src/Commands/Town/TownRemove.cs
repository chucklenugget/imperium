﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTownRemoveCommand(User user)
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

      user.SendMessage(Messages.SelectTownCupboardToRemove);
      user.BeginInteraction(new RemovingAreaFromTownInteraction(faction, town));
    }
  }
}
