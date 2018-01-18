namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimRemoveCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      user.SendMessage(Messages.SelectClaimCupboardToRemove);
      user.BeginInteraction(new RemovingClaimInteraction(faction));
    }
  }
}