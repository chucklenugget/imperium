namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnFactionChatCommand(User user, string[] args)
    {
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

      faction.SendChatMessage("<color=#a1ff46>(FACTION)</color> {0}: {1}", user.Name, message);
    }
  }
}