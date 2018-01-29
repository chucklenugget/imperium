namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimRemoveCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      user.SendChatMessage(Messages.SelectClaimCupboardToRemove);
      user.BeginInteraction(new RemovingClaimInteraction(faction));
    }
  }
}