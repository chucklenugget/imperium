namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimGiveCommand(User user, string[] args)
    {
      if (args.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/claim give FACTION");
        return;
      }

      Faction sourceFaction = Factions.GetByMember(user);

      if (!EnsureCanChangeFactionClaims(user, sourceFaction))
        return;

      string factionId = Util.NormalizeFactionId(args[0]);
      Faction targetFaction = Factions.Get(factionId);

      if (targetFaction == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, factionId);
        return;
      }

      user.SendChatMessage(Messages.SelectClaimCupboardToTransfer);
      user.BeginInteraction(new TransferringClaimInteraction(sourceFaction, targetFaction));
    }
  }
}