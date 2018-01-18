namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimHeadquartersCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      user.SendMessage(Messages.SelectClaimCupboardForHeadquarters);
      user.BeginInteraction(new SelectingHeadquartersInteraction(faction));
    }
  }
}