namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  public partial class Imperium
  {
    void OnPinHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/pin list [TYPE]</color>: List all pins (or all of a certain type)");
      sb.AppendLine("  <color=#ffd479>/pin add TYPE \"NAME\"</color>: Create a pin at your current location");
      sb.AppendLine("  <color=#ffd479>/pin remove \"NAME\"</color>: Remove a pin you created");
      sb.AppendLine("  <color=#ffd479>/pin help</color>: Prints this message");

      if (user.HasPermission(Permission.AdminPins))
      {
        sb.AppendLine("Admin commands:");
        sb.AppendLine("  <color=#ffd479>/pin delete XY</color>: Delete a pin from an area");
      }

      sb.Append("Available pin types: ");
      foreach (string type in Enum.GetNames(typeof(PinType)).OrderBy(str => str.ToLowerInvariant()))
        sb.AppendFormat("<color=#ffd479>{0}</color>, ", type.ToLowerInvariant());
      sb.Remove(sb.Length - 2, 2);
      sb.AppendLine();

      user.SendChatMessage(sb);
    }
  }
}
