namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    [ChatCommand("f")]
    void OnFactionChatCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      string message = String.Join(" ", args).Trim();

      if (message.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/f MESSAGE...");
        return;
      }

      Faction faction = Factions.GetByMember(user);

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      faction.SendChatMessage("<color=#a1ff46>(FACTION)</color> {0}: {1}", user.UserName, message);
      Puts("[FACTION] {0} - {1}: {2}", faction.Id, user.UserName, message);
    }

    [ChatCommand("c")]
    void OnClanChatCommand(BasePlayer player, string command, string[] args)
    {
      OnFactionChatCommand(player, command, args);
    }
  }
}