namespace Oxide.Plugins
{
  public partial class Imperium
  {

    void OnTaxChestCommand(User user)
    {
      Faction faction = Factions.GetByUser(user);

      if (faction == null)
      {
        user.SendMessage(Messages.CannotSelectTaxChestNotMemberOfFaction);
        return;
      }

      if (!faction.IsLeader(user))
      {
        user.SendMessage(Messages.CannotSelectTaxChestNotFactionLeader);
        return;
      }

      user.SendMessage(Messages.SelectTaxChest);
      user.BeginInteraction(new SelectingTaxChestInteraction(faction));
    }
  }
}
