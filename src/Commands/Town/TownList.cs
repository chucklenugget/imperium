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
          var areaIds = town.Areas.Select(area => area.Id);
          float distance = (float) Math.Floor(town.GetDistanceFromEntity(user.Player));
          int population = town.GetPopulation();
          sb.AppendLine(String.Format("  <color=#ffd479>{0}:</color> {1:0.00}m ({2}), population {3}", town.Name, distance, FormatList(areaIds), population));
        }
      }

      user.SendChatMessage(sb);
    }
  }
}
