namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionCreateCommand(User user, string[] args)
    {
      if (!user.HasPermission(Permission.ManageFactions))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

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

      string id = Util.RemoveSpecialCharacters(args[0].Replace(" ", ""));

      if (id.Length < Options.Factions.MinFactionNameLength || id.Length > Options.Factions.MaxFactionNameLength)
      {
        user.SendChatMessage(Messages.InvalidFactionName, Options.Factions.MinFactionNameLength, Options.Factions.MaxFactionNameLength);
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