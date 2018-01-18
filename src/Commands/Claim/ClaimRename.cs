namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimRenameCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByUser(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      if (args.Length != 2)
      {
        user.SendMessage(Messages.CannotRenameAreaBadUsage);
        return;
      }

      var areaId = NormalizeAreaId(args[0]);
      var name = NormalizeName(args[1]);

      if (name == null || name.Length < Options.MinAreaNameLength)
      {
        user.SendMessage(Messages.CannotRenameAreaBadName, areaId, Options.MinAreaNameLength);
        return;
      }

      Area area = Areas.Get(areaId);

      if (area == null)
      {
        user.SendMessage(Messages.CannotRenameAreaUnknownAreaId, areaId);
        return;
      }

      if (area.FactionId != faction.Id)
      {
        user.SendMessage(Messages.CannotRenameAreaNotClaimedByFaction, area.Id);
        return;
      }

      if (area.Type == AreaType.Town)
      {
        user.SendMessage(Messages.CannotRenameAreaIsTown, area.Id, area.Name);
        return;
      }

      area.Name = name;
      user.SendMessage(Messages.AreaRenamed, area.Id, area.Name);
    }
  }
}