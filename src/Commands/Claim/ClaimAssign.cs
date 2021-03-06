﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimAssignCommand(User user, string[] args)
    {
      if (!user.HasPermission(Permission.AdminClaims))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

      if (args.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/claim assign FACTION");
        return;
      }

      string factionId = Util.NormalizeFactionId(args[0]);
      Faction faction = Factions.Get(factionId);

      if (faction == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, factionId);
        return;
      }

      user.SendChatMessage(Messages.SelectClaimCupboardToAssign);
      user.BeginInteraction(new AssigningClaimInteraction(faction));
    }
  }
}