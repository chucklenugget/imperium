namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnClaimRenameCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserCanChangeFactionClaims(user, faction))
        return;

      if (args.Length != 2)
      {
        user.SendChatMessage(Messages.Usage, "/claim rename XY \"NAME\"");
        return;
      }

      var areaId = Util.NormalizeAreaId(args[0]);
      var name = Util.NormalizeAreaName(args[1]);

      if (name == null || name.Length < Options.Claims.MinAreaNameLength || name.Length > Options.Claims.MaxAreaNameLength)
      {
        user.SendChatMessage(Messages.InvalidAreaName, Options.Claims.MinAreaNameLength, Options.Claims.MaxAreaNameLength);
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

      user.SendChatMessage(Messages.AreaRenamed, area.Id, name);
      Log($"{Util.Format(user)} renamed {area.Id} to {name}");

      area.Name = name;
    }
  }
}