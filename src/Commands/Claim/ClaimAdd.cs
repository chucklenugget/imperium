namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimAddCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      user.SendChatMessage(Messages.SelectClaimCupboardToAdd);
      user.BeginInteraction(new AddingClaimInteraction(faction));
    }
  }
}