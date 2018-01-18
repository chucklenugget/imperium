namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("cancel")]
    void OnCancelCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);

      if (user.CurrentInteraction == null)
      {
        user.SendMessage(Messages.NoInteractionInProgress);
        return;
      }

      user.SendMessage(Messages.InteractionCanceled);
      user.CancelInteraction();
    }
  }
}
