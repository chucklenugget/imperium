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
      War[] wars = Wars.GetAllActiveWarsByFaction(faction);

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
          sb.AppendFormat("{0}. <color=#ffd479>{1}</color> vs <color=#ffd479>{2}</color>", (idx + 1), war.AttackerId, war.DefenderId);
          if (war.IsAttackerOfferingPeace) sb.AppendFormat(": <color=#ffd479>{0}</color> is offering peace!", war.AttackerId);
          if (war.IsDefenderOfferingPeace) sb.AppendFormat(": <color=#ffd479>{0}</color> is offering peace!", war.DefenderId);
          sb.AppendLine();
        }
      }

      user.SendChatMessage(sb);
    }
  }
}
