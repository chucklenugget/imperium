namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnTownHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/towns [list]</color>: Lists all towns on the island");
      sb.AppendLine("  <color=#ffd479>/towns help</color>: Prints this message");

      if (user.HasPermission(PERM_CHANGE_TOWNS))
      {
        sb.AppendLine("Mayor commands:");
        sb.AppendLine("  <color=#ffd479>/towns create \"NAME\"</color>: Create a new town");
        sb.AppendLine("  <color=#ffd479>/towns expand</color>: Add an area to your town");
        sb.AppendLine("  <color=#ffd479>/towns remove</color>: Remove an area from your town");
        sb.AppendLine("  <color=#ffd479>/towns disband</color>: Disband your town immediately (no undo!)");
      }

      user.SendChatMessage(sb);
    }
  }
}
