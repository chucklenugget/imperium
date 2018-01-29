namespace Oxide.Plugins
{
  using System;
  using System.Text;

  public partial class Imperium
  {
    void OnWarListCommand(User user)
    {
      var sb = new StringBuilder();
      War[] wars = Wars.GetAllActiveWars();

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
          sb.AppendFormat("{0}. <color=#ffd479>{1}</color> vs <color=#ffd479>{2}</color>: {2}", (idx + 1), war.AttackerId, war.DefenderId, war.CassusBelli);
          sb.AppendLine();
        }
      }

      user.SendChatMessage(sb);
    }
  }
}
