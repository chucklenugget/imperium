namespace Oxide.Plugins
{
  using System.Text.RegularExpressions;

  public partial class Imperium
  {
    void OnFactionCreateCommand(User user, string[] args)
    {
      var idRegex = new Regex("^[a-zA-Z0-9]{2,6}$");

      if (user.Faction != null)
      {
        user.SendChatMessage(Messages.AlreadyMemberOfFaction);
        return;
      }

      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction create NAME");
        return;
      }

      string id = args[0].Trim();

      if (!idRegex.IsMatch(id))
      {
        user.SendChatMessage(Messages.InvalidFactionName);
        return;
      }

      if (Factions.Exists(id))
      {
        user.SendChatMessage(Messages.FactionAlreadyExists, id);
        return;
      }

      PrintToChat(Messages.FactionCreatedAnnouncement, id);
      Log($"{Util.Format(user)} created faction {id}");

      Faction faction = Factions.Create(id, user);
      user.SetFaction(faction);
    }
  }
}