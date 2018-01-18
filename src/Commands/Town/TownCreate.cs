namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTownCreateCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      if (args.Length == 0)
      {
        user.SendMessage(Messages.CannotCreateTownWrongUsage);
        return;
      }

      Town town = Areas.GetTownByMayor(user);
      if (town != null)
      {
        user.SendMessage(Messages.CannotCreateTownAlreadyMayor, town.Name);
        return;
      }

      var name = NormalizeName(args[0]);

      town = Areas.GetTown(name);
      if (town != null)
      {
        user.SendMessage(Messages.CannotCreateTownSameNameAlreadyExists, town.Name);
        return;
      }

      user.SendMessage(Messages.SelectTownCupboardToCreate, name);
      user.BeginInteraction(new CreatingTownInteraction(faction, name));
    }
  }
}
