namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimHeadquartersCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      user.SendChatMessage(Messages.SelectClaimCupboardForHeadquarters);
      user.BeginInteraction(new SelectingHeadquartersInteraction(faction));
    }
  }
}