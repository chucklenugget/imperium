namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimGiveCommand(User user, string[] args)
    {
      if (args.Length == 0)
      {
        user.SendMessage(Messages.CannotTransferClaimBadUsage);
        return;
      }

      Faction sourceFaction = Factions.GetByUser(user);

      if (!EnsureCanChangeFactionClaims(user, sourceFaction))
        return;

      string factionId = NormalizeFactionId(args[0]);
      Faction targetFaction = Factions.Get(factionId);

      if (targetFaction == null)
      {
        user.SendMessage(Messages.InteractionFailedUnknownFaction, factionId);
        return;
      }

      user.SendMessage(Messages.SelectClaimCupboardToTransfer);
      user.BeginInteraction(new TransferringClaimInteraction(sourceFaction, targetFaction));
    }
  }
}