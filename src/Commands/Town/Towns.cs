namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("towns")]
    void OnTownsCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.Towns.Enabled)
      {
        user.SendChatMessage(Messages.TownsDisabled);
        return;
      }

      OnTownListCommand(user);
    }
  }
}
