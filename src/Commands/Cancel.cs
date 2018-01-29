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
        user.SendChatMessage(Messages.NoInteractionInProgress);
        return;
      }

      user.SendChatMessage(Messages.InteractionCanceled);
      user.CancelInteraction();
    }
  }
}
