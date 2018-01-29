namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnWarHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/war list</color>: Show all active wars");
      sb.AppendLine("  <color=#ffd479>/war status</color>: Show all active wars your faction is involved in");
      sb.AppendLine("  <color=#ffd479>/war declare FACTION \"REASON\"</color>: Declare war against another faction");
      sb.AppendLine("  <color=#ffd479>/war end FACTION</color>: Offer to end a war, or accept an offer made to you");
      sb.AppendLine("  <color=#ffd479>/war help</color>: Show this message");

      user.SendChatMessage(sb);
    }
  }
}
