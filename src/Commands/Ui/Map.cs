namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("map")]
    void OnMapCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!user.Map.IsVisible && !EnforceCommandCooldown(user, "map", Options.Map.CommandCooldownSeconds))
        return;

      user.Map.Toggle();
    }
  }
}
