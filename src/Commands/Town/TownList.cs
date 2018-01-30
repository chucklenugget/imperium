namespace Oxide.Plugins
{
  using System;
  using System.Linq;
  using System.Text;

  public partial class Imperium
  {
    void OnTownListCommand(User user)
    {
      Town[] towns = Areas.GetAllTowns();
      var sb = new StringBuilder();

      if (towns.Length == 0)
      {
        sb.AppendFormat(String.Format("No towns have been founded."));
      }
      else
      {
        sb.AppendLine(String.Format("<color=#ffd479>There are {0} towns on the island:</color>", towns.Length));
        foreach (Town town in towns)
        {
          float distance = (float) Math.Floor(town.GetDistanceFromEntity(user.Player));
          sb.AppendLine(String.Format("  <color=#ffd479>{0}:</color> {1:0.00}m ({2})", town.Name, distance, Util.Format(town.Areas)));
        }
      }

      user.SendChatMessage(sb);
    }
  }
}
