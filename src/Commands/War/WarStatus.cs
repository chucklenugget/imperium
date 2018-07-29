namespace Oxide.Plugins
{
  using System;
  using System.Text;

  public partial class Imperium
  {
    void OnWarStatusCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      var sb = new StringBuilder();
      War[] wars = Wars.GetWarsByFaction(faction);

      if (wars.Length == 0)
      {
        sb.AppendLine("Your faction is not involved in any wars.");
      }
      else
      {
        sb.AppendLine(String.Format("<color=#ffd479>Your faction is involved in {0} wars:</color>", wars.Length));
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
