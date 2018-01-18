namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("towns")]
    void OnTownsCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableTowns)
      {
        user.SendMessage(Messages.TownsDisabled);
        return;
      }

      OnTownListCommand(user);
    }
  }
}
