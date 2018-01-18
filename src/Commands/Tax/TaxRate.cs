namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnTaxRateCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByUser(user);

      if (faction == null)
      {
        user.SendMessage(Messages.CannotSetTaxRateNotMemberOfFaction);
        return;
      }

      if (!faction.IsLeader(user))
      {
        user.SendMessage(Messages.CannotSetTaxRateNotFactionLeader);
        return;
      }

      float taxRate;
      try
      {
        taxRate = Convert.ToInt32(args[0]) / 100f;
      }
      catch
      {
        user.SendMessage(Messages.CannotSetTaxRateInvalidValue, Options.MaxTaxRate * 100);
        return;
      }

      if (taxRate < 0 || taxRate > Options.MaxTaxRate)
      {
        user.SendMessage(Messages.CannotSetTaxRateInvalidValue, Options.MaxTaxRate * 100);
        return;
      }

      Factions.SetTaxRate(faction, taxRate);
      user.SendMessage(Messages.SetTaxRateSuccessful, faction.Id, taxRate * 100);
    }
  }
}
