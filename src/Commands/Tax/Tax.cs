namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
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
        case "rate":
          OnTaxRateCommand(user, restArguments);
          break;
        case "help":
        default:
          OnTaxHelpCommand(user);
          break;
      }
    }
  }
}
