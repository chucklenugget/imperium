namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimPvpCommand(User user, string[] args)
    {
      if (!Options.Pvp.EnablePvpTogglingForClaims)
      {
        user.SendChatMessage(Messages.PvpTogglingDisabled);
        return;
      }

      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserCanChangeFactionClaims(user, faction))
        return;

      if (args.Length != 2)
      {
        user.SendChatMessage(Messages.Usage, "/claim pvp XY on|off");
        return;
      }

      string mode = args[1].ToLowerInvariant();

      if (mode != "on" && mode != "off")
      {
        user.SendChatMessage(Messages.Usage, "/claim pvp XY on|off");
        return;
      }

      bool isPvp = (mode == "on");
      Area area = Areas.Get(args[0]);

      if (area == null)
      {
        user.SendChatMessage(Messages.UnknownArea, args[0]);
        return;
      }

      if (area.FactionId != user.Faction.Id)
      {
        user.SendChatMessage(Messages.AreaNotOwnedByYourFaction, area.Id);
        return;
      }

      if (area.HasPendingRuleChange)
      {
        user.SendChatMessage(Messages.AreaAlreadyHasRuleChangePending, area.Id);
        return;
      }

      if (isPvp && area.IsPvpEnabled == true)
      {
        user.SendChatMessage(Messages.AreaIsAlreadyPvp, area.Id);
        return;
      }

      if (!isPvp && area.IsPvpEnabled == false)
      {
        user.SendChatMessage(Messages.AreaIsNotPvp, area.Id);
        return;
      }

      if (isPvp)
      {
        PrintToChat(Messages.AreaPvpEnabledAnnouncement, user.Faction.Id, area.Id, Options.Pvp.RuleChangeDelaySeconds);
        Log($"[PVP] {Util.Format(user)} has enabled PVP on {area.Id} on behalf of {faction.Id}");
      }
      else
      {
        PrintToChat(Messages.AreaPvpDisabledAnnouncement, user.Faction.Id, area.Id, Options.Pvp.RuleChangeDelaySeconds);
        Log($"[PVP] {Util.Format(user)} has disabled PVP on {area.Id} on behalf of {faction.Id}");
      }

      area.SetPvpEnabled(isPvp);
    }
  }
}