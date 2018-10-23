namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("hud")]
    void OnHudCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!EnforceCommandCooldown(user, "hud", Options.Map.CommandCooldownSeconds))
        return;

      user.Hud.Toggle();
    }
  }
}
