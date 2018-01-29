namespace Oxide.Plugins
{
  public partial class Imperium
  {

    void OnTaxChestCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (faction == null)
      {
        user.SendChatMessage(Messages.CannotSelectTaxChestNotMemberOfFaction);
        return;
      }

      if (!faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.CannotSelectTaxChestNotFactionLeader);
        return;
      }

      user.SendChatMessage(Messages.SelectTaxChest);
      user.BeginInteraction(new SelectingTaxChestInteraction(faction));
    }
  }
}
