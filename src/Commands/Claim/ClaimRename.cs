namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimRenameCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      if (args.Length != 2)
      {
        user.SendChatMessage(Messages.Usage, "/claim rename XY \"NAME\"");
        return;
      }

      var areaId = NormalizeAreaId(args[0]);
      var name = NormalizeName(args[1]);

      if (name == null || name.Length < Options.MinAreaNameLength)
      {
        user.SendChatMessage(Messages.InvalidAreaName, Options.MinAreaNameLength);
        return;
      }

      Area area = Areas.Get(areaId);

      if (area == null)
      {
        user.SendChatMessage(Messages.UnknownArea, areaId);
        return;
      }

      if (area.FactionId != faction.Id)
      {
        user.SendChatMessage(Messages.AreaNotOwnedByYourFaction, area.Id);
        return;
      }

      if (area.Type == AreaType.Town)
      {
        user.SendChatMessage(Messages.CannotRenameAreaIsTown, area.Id, area.Name);
        return;
      }

      area.Name = name;
      user.SendChatMessage(Messages.AreaRenamed, area.Id, area.Name);
    }
  }
}