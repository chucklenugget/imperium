namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnClaimHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/claim</color>: Add a claim for your faction");
      sb.AppendLine("  <color=#ffd479>/claim hq</color>: Select your faction's headquarters");
      sb.AppendLine("  <color=#ffd479>/claim remove</color>: Remove a claim for your faction (no undo!)");
      sb.AppendLine("  <color=#ffd479>/claim give FACTION</color>: Give a claimed area to another faction (no undo!)");
      sb.AppendLine("  <color=#ffd479>/claim rename XY \"NAME\"</color>: Rename an area claimed by your faction");
      sb.AppendLine("  <color=#ffd479>/claim show XY</color>: Show who owns an area");
      sb.AppendLine("  <color=#ffd479>/claim list FACTION</color>: List all areas claimed for a faction");
      sb.AppendLine("  <color=#ffd479>/claim cost [XY]</color>: Show the cost for your faction to claim an area");

      if (!Options.Upkeep.Enabled)
        sb.AppendLine("  <color=#ffd479>/claim upkeep</color>: Show information about upkeep costs for your faction");

      sb.AppendLine("  <color=#ffd479>/claim help</color>: Prints this message");

      if (user.HasPermission(Permission.AdminClaims))
      {
        sb.AppendLine("Admin commands:");
        sb.AppendLine("  <color=#ffd479>/claim assign FACTION</color>: Use the hammer to assign a claim to another faction");
        sb.AppendLine("  <color=#ffd479>/claim delete XY [XY XY XY...]</color>: Remove the claim on the specified areas (no undo!)");
      }

      user.SendChatMessage(sb);
    }
  }
}