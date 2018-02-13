namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnFactionShowCommand(User user)
    {
      Faction faction = user.Faction;

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      var sb = new StringBuilder();

      sb.Append("You are ");
      if (faction.HasOwner(user))
        sb.Append("the owner");
      else if (faction.HasManager(user))
        sb.Append("a manager");
      else
        sb.Append("a member");

      sb.AppendLine($"of <color=#ffd479>[{faction.Id}]</color>.");

      User[] activeMembers = faction.GetAllActiveMembers();

      sb.AppendLine($"<color=#ffd479>{faction.MemberCount}</color> member(s), <color=#ffd479>{activeMembers.Length}</color> online:");
      sb.Append("  ");

      foreach (User member in activeMembers)
        sb.Append($"<color=#ffd479>{member.UserName}</color>, ");

      sb.Remove(sb.Length - 2, 2);
      sb.AppendLine();

      if (faction.InviteIds.Count > 0)
      {
        User[] activeInvitedUsers = faction.GetAllActiveInvitedUsers();

        sb.AppendLine($"<color=#ffd479>{faction.InviteIds.Count}</color> invited player(s), <color=#ffd479>{activeInvitedUsers.Length}</color> online:");
        sb.Append("  ");

        foreach (User invitedUser in activeInvitedUsers)
          sb.Append($"<color=#ffd479>{invitedUser.UserName}</color>, ");

        sb.Remove(sb.Length - 2, 2);
        sb.AppendLine();
      }

      user.SendChatMessage(sb);
    }
  }
}