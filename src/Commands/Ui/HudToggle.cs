namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ConsoleCommand("imperium.hud.toggle")]
    void OnHudToggleConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);
      if (user == null) return;

      if (!EnforceCommandCooldown(user))
        return;

      user.Hud.Toggle();
    }
  }
}
