namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnBadlandsHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/badlands add XY [XY XY...]</color>: Add area(s) to the badlands");
      sb.AppendLine("  <color=#ffd479>/badlands remove XY [XY XY...]</color>: Remove area(s) from the badlands");
      sb.AppendLine("  <color=#ffd479>/badlands set XY [XY XY...]</color>: Set the badlands to a list of areas");
      sb.AppendLine("  <color=#ffd479>/badlands clear</color>: Remove all areas from the badlands");
      sb.AppendLine("  <color=#ffd479>/badlands help</color>: Prints this message");

      user.SendChatMessage(sb);
    }
  }
}
