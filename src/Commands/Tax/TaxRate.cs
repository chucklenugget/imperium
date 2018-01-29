namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnTaxRateCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (faction == null)
      {
        user.SendChatMessage(Messages.CannotSetTaxRateNotMemberOfFaction);
        return;
      }

      if (!faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.CannotSetTaxRateNotFactionLeader);
        return;
      }

      float taxRate;
      try
      {
        taxRate = Convert.ToInt32(args[0]) / 100f;
      }
      catch
      {
        user.SendChatMessage(Messages.CannotSetTaxRateInvalidValue, Options.MaxTaxRate * 100);
        return;
      }

      if (taxRate < 0 || taxRate > Options.MaxTaxRate)
      {
        user.SendChatMessage(Messages.CannotSetTaxRateInvalidValue, Options.MaxTaxRate * 100);
        return;
      }

      Factions.SetTaxRate(faction, taxRate);
      user.SendChatMessage(Messages.SetTaxRateSuccessful, faction.Id, taxRate * 100);
    }
  }
}
