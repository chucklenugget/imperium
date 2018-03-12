namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("pvp")]
    void OnPvpCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);

      if (!Options.Pvp.EnablePvpCommand)
      {
        user.SendChatMessage(Messages.PvpModeDisabled);
        return;
      }

      if (!EnforceCommandCooldown(user, "pvp", Options.Pvp.CommandCooldownSeconds))
        return;

      if (user.IsInPvpMode)
      {
        user.IsInPvpMode = false;
        user.SendChatMessage(Messages.ExitedPvpMode);
      }
      else
      {
        user.IsInPvpMode = true;
        user.SendChatMessage(Messages.EnteredPvpMode);
      }

      user.Hud.Refresh();
    }
  }
}
