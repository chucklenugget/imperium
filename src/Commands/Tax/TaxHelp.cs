namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnTaxHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/tax rate NN</color>: Set the tax rate for your faction");
      sb.AppendLine("  <color=#ffd479>/tax chest</color>: Select a container to use as your faction's tax chest");
      sb.AppendLine("  <color=#ffd479>/tax help</color>: Prints this message");

      user.SendChatMessage(sb);
    }
  }
}
