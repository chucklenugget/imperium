namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTownCreateCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserCanManageTowns(user, faction))
        return;

      if (args.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/town create NAME");
        return;
      }

      Town town = Areas.GetTownByMayor(user);
      if (town != null)
      {
        user.SendChatMessage(Messages.CannotCreateTownAlreadyMayor, town.Name);
        return;
      }

      var name = Util.NormalizeAreaName(args[0]);

      town = Areas.GetTown(name);
      if (town != null)
      {
        user.SendChatMessage(Messages.CannotCreateTownSameNameAlreadyExists, town.Name);
        return;
      }

      user.SendChatMessage(Messages.SelectTownCupboardToCreate, name);
      user.BeginInteraction(new CreatingTownInteraction(faction, name));
    }
  }
}
