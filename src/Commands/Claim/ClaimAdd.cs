namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimAddCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      user.SendMessage(Messages.SelectClaimCupboardToAdd);
      user.BeginInteraction(new AddingClaimInteraction(faction));
    }
  }
}