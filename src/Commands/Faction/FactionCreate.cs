namespace Oxide.Plugins
{
  using System.Text.RegularExpressions;

  public partial class Imperium
  {
    void OnFactionCreateCommand(User user, string[] args)
    {
      var idRegex = new Regex("^[a-zA-Z0-9]{2,6}$");

      if (args.Length != 2)
      {
        user.SendChatMessage(Messages.Usage, "/faction create NAME \"DESCRIPTION\"");
        return;
      }

      if (user.Faction != null)
      {
        user.SendChatMessage(Messages.AlreadyMemberOfFaction);
        return;
      }

      string id = args[0].Trim();
      string description = args[1].Trim();

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

      Faction faction = Factions.Create(id, description, user);
      user.SetFaction(faction);

      PrintToChat(Messages.FactionCreatedAnnouncement, faction.Id, faction.Description);
    }
  }
}