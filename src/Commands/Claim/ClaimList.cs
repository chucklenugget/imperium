namespace Oxide.Plugins
{
  using System;
  using System.Linq;
  using System.Text;

  public partial class Imperium
  {
    void OnClaimListCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendMessage(Messages.CannotListClaimsBadUsage);
        return;
      }

      string factionId = NormalizeFactionId(args[0]);
      Faction faction = Factions.Get(factionId);

      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedUnknownFaction, factionId);
        return;
      }

      Area[] areas = Areas.GetAllClaimedByFaction(faction);
      Area headquarters = areas.FirstOrDefault(a => a.Type == AreaType.Headquarters);

      var sb = new StringBuilder();

      if (areas.Length == 0)
      {
        sb.AppendFormat(String.Format("<color=#ffd479>[{0}]</color> has no land holdings.", factionId));
      }
      else
      {
        float percentageOfMap = (areas.Length / (float)Areas.Count) * 100;
        sb.AppendLine(String.Format("<color=#ffd479>[{0}] owns {1} tiles ({2:F2}% of the known world)</color>", faction.Id, areas.Length, percentageOfMap));
        sb.AppendLine(String.Format("Headquarters: {0}", (headquarters == null) ? "Unknown" : headquarters.Id));
        sb.AppendLine(String.Format("Areas claimed: {0}", FormatList(areas.Select(a => a.Id))));
      }

      user.SendMessage(sb);
    }
  }
}