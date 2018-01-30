namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTaxChestCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return;
      }

      user.SendChatMessage(Messages.SelectTaxChest);
      user.BeginInteraction(new SelectingTaxChestInteraction(faction));
    }
  }
}
