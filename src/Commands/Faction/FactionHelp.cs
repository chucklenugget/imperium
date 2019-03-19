namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnFactionHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/faction</color>: Show information about your faction");
      sb.AppendLine("  <color=#ffd479>/f MESSAGE...</color>: Send a message to all online members of your faction");

      if (user.HasPermission(Permission.ManageFactions))
        sb.AppendLine("  <color=#ffd479>/faction create</color>: Create a new faction");

      sb.AppendLine("  <color=#ffd479>/faction join FACTION</color>: Join a faction if you have been invited");
      sb.AppendLine("  <color=#ffd479>/faction leave</color>: Leave your current faction");
      sb.AppendLine("  <color=#ffd479>/faction invite \"PLAYER\"</color>: Invite another player to join your faction");
      sb.AppendLine("  <color=#ffd479>/faction kick \"PLAYER\"</color>: Kick a player out of your faction");
      sb.AppendLine("  <color=#ffd479>/faction promote \"PLAYER\"</color>: Promote a faction member to manager");
      sb.AppendLine("  <color=#ffd479>/faction demote \"PLAYER\"</color>: Remove a faction member as manager");
      sb.AppendLine("  <color=#ffd479>/faction disband forever</color>: Disband your faction immediately (no undo!)");
      sb.AppendLine("  <color=#ffd479>/faction help</color>: Prints this message");

      if (user.HasPermission(Permission.AdminFactions))
      {
        sb.AppendLine("Admin commands:");
        sb.AppendLine("  <color=#ffd479>/faction admin promote FACTION \"PLAYER\"</color>: Forcibly promote a player");
        sb.AppendLine("  <color=#ffd479>/faction admin demote FACTION \"PLAYER\"</color>: Forcibly demote a player");
        sb.AppendLine("  <color=#ffd479>/faction admin kick FACTION \"PLAYER\"</color>: Forcibly kick a player from a faction");
        sb.AppendLine("  <color=#ffd479>/faction admin owner FACTION \"PLAYER\"</color>: Forcibly change the owner of a faction");
        sb.AppendLine("  <color=#ffd479>/faction admin disband FACTION</color>: Forcibly disband a faction");
      }

      user.SendChatMessage(sb);
    }
  }
}