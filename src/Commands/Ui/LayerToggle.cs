namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    [ConsoleCommand("imperium.map.togglelayer")]
    void OnMapToggleLayerConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);
      if (user == null) return;

      if (!user.Map.IsVisible)
        return;

      string str = arg.GetString(0);
      UserMapLayer layer;

      if (String.IsNullOrEmpty(str) || !Util.TryParseEnum(arg.Args[0], out layer))
        return;

      user.Preferences.ToggleMapLayer(layer);
      user.Map.Refresh();
    }
  }
}
