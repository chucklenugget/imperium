namespace Oxide.Plugins
{
  using System;
  using System.Linq;
  using System.Text;

  public partial class RustFactions
  {
    [ChatCommand("tax")]
    void OnTaxCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableTaxation)
      {
        user.SendMessage(Messages.TaxationDisabled);
        return;
      };

      if (args.Length == 0)
      {
        OnTaxHelpCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "chest":
          OnTaxChestCommand(user);
          break;
        case "set":
          OnTaxSetCommand(user, restArguments);
          break;
        case "help":
        default:
          OnTaxHelpCommand(user);
          break;
      }
    }

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

    void OnTaxSetCommand(User user, string[] args)
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

      int taxRate;
      try
      {
        taxRate = Convert.ToInt32(args[0]);
      }
      catch
      {
        user.SendMessage(Messages.CannotSetTaxRateInvalidValue, Options.MaxTaxRate);
        return;
      }

      if (taxRate < 0 || taxRate > Options.MaxTaxRate)
      {
        user.SendMessage(Messages.CannotSetTaxRateInvalidValue, Options.MaxTaxRate);
        return;
      }

      Factions.SetTaxRate(faction, taxRate);
      user.SendMessage(Messages.SetTaxRateSuccessful, faction.Id, taxRate);
    }

    void OnTaxHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/tax set NN</color>: Set the tax rate for your faction");
      sb.AppendLine("  <color=#ffd479>/tax chest</color>: Select a container to use as your faction's tax chest");
      sb.AppendLine("  <color=#ffd479>/tax help</color>: Prints this message");

      user.SendMessage(sb);
    }

  }

}
