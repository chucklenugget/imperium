namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimAssignCommand(User user, string[] args)
    {
      if (args.Length == 0)
      {
        user.SendMessage(Messages.CannotAssignClaimBadUsage);
        return;
      }

      if (!user.HasPermission(PERM_CHANGE_CLAIMS))
      {
        user.SendMessage(Messages.CannotAssignClaimNoPermission);
        return;
      }

      string factionId = NormalizeFactionId(args[0]);
      Faction faction = Factions.Get(factionId);

      if (faction == null)
      {
        user.SendMessage(Messages.InteractionFailedUnknownFaction, factionId);
        return;
      }

      user.SendMessage(Messages.SelectClaimCupboardToAssign);
      user.BeginInteraction(new AssigningClaimInteraction(faction));
    }
  }
}