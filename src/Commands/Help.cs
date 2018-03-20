namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    [ChatCommand("help")]
    void OnHelpCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      var sb = new StringBuilder();

      sb.AppendLine($"<size=18>Welcome to {ConVar.Server.hostname}!</size>");
      sb.AppendLine($"Powered by {Name} v{Version} by <color=#ffd479>chucklenugget</color>");
      sb.AppendLine();

      sb.Append("The following commands are available. To learn more about each command, do <color=#ffd479>/command help</color>. ");
      sb.AppendLine("For example, to learn more about how to claim land, do <color=#ffd479>/claim help</color>.");
      sb.AppendLine();

      sb.AppendLine("<color=#ffd479>/faction</color> Create or join a faction");
      sb.AppendLine("<color=#ffd479>/claim</color> Claim areas of land");

      if (Options.Taxes.Enabled)
        sb.AppendLine("<color=#ffd479>/tax</color> Manage taxation of your land");

      if (Options.Map.PinsEnabled)
        sb.AppendLine("<color=#ffd479>/pin</color> Add pins (points of interest) to the map");

      if (Options.War.Enabled)
        sb.AppendLine("<color=#ffd479>/war</color> See active wars, declare war, or offer peace");

      if (Options.Badlands.Enabled)
      {
        if (user.HasPermission(Permission.AdminBadlands))
          sb.AppendLine("<color=#ffd479>/badlands</color> Find or change badlands areas");
        else
          sb.AppendLine("<color=#ffd479>/badlands</color> Find badlands (PVP) areas");
      }

      user.SendChatMessage(sb);
    }
  }
}
