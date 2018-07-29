namespace Oxide.Plugins
{
  using System;
  using System.Text;

  public partial class Imperium
  {
    void OnWarListCommand(User user)
    {
      var sb = new StringBuilder();
      War[] wars = Wars.GetAllWars();

      if (wars.Length == 0)
      {
        sb.Append("The island is at peace... for now. No wars have been declared.");
      }
      else
      {
        sb.AppendLine(String.Format("<color=#ffd479>The island is at war! {0} wars have been declared:</color>", wars.Length));
        for (var idx = 0; idx < wars.Length; idx++)
        {
          War war = wars[idx];
          sb.Append($"{idx + 1}. <color=#ffd479>{war.AttackerId}</color> vs <color=#ffd479>{war.DefenderId}</color>");

          if (war.State == WarState.Declared)
            sb.AppendFormat(" [begins in {0:hh}h{0:mm}m]", war.DiplomacyTimeRemaining);
          else if (war.State == WarState.Started)
            sb.AppendFormat(" [at war for {0:hh}h{0:mm}m]", DateTime.UtcNow.Subtract(war.StartTime.Value));

          sb.AppendLine();

          if (war.State == WarState.AttackerOfferingPeace)
            sb.AppendLine($"    (peace offered by {war.AttackerId})");
          else if (war.State == WarState.DefenderOfferingPeace)
            sb.AppendLine($"    (peace offered by {war.DefenderId})");
        }
      }

      user.SendChatMessage(sb);
    }
  }
}
