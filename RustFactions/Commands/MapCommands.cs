namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    [ChatCommand("map")]
    void OnMapCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      user.Map.Toggle();
    }

    [ConsoleCommand("rustfactions.map.toggle")]
    void OnMapToggleConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);
      if (user == null) return;

      user.Map.Toggle();
    }
  }
}
