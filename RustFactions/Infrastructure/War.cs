namespace Oxide.Plugins
{
  using System;

  public partial class RustFactions
  {
    class War
    {
      public string AttackerId { get; set; }
      public string DefenderId { get; set; }
      public ulong DeclarerId { get; set; }
      public string CassusBelli { get; set; }

      public DateTime? AttackerPeaceOfferingTime { get; set; }
      public DateTime? DefenderPeaceOfferingTime { get; set; }

      public DateTime StartTime { get; private set; }
      public DateTime? EndTime { get; set; }
      public WarEndReason? EndReason { get; set; }

      public bool IsActive
      {
        get { return EndTime == null; }
      }

      public bool IsAttackerOfferingPeace
      {
        get { return AttackerPeaceOfferingTime != null; }
      }

      public bool IsDefenderOfferingPeace
      {
        get { return DefenderPeaceOfferingTime != null; }
      }

      public War(Faction attacker, Faction defender, User declarer, string cassusBelli)
      {
        AttackerId = attacker.Id;
        DefenderId = defender.Id;
        DeclarerId = declarer.Id;
        CassusBelli = cassusBelli;
        StartTime = DateTime.Now;
      }

      public War(WarInfo info)
      {
        AttackerId = info.AttackerId;
        DefenderId = info.DefenderId;
        DeclarerId = info.DeclarerId;
        CassusBelli = info.CassusBelli;
        StartTime = info.StartTime;
        EndTime = info.EndTime;
      }

      public void OfferPeace(Faction faction)
      {
        if (AttackerId == faction.Id)
          AttackerPeaceOfferingTime = DateTime.Now;
        else if (DefenderId == faction.Id)
          DefenderPeaceOfferingTime = DateTime.Now;
        else
          throw new InvalidOperationException(String.Format("{0} tried to offer peace but the faction wasn't involved in the war!", faction.Id));
      }

      public bool IsOfferingPeace(Faction faction)
      {
        return IsOfferingPeace(faction.Id);
      }

      public bool IsOfferingPeace(string factionId)
      {
        return (factionId == AttackerId && IsAttackerOfferingPeace) || (factionId == DefenderId && IsDefenderOfferingPeace);
      }

      public WarInfo Serialize()
      {
        return new WarInfo {
          AttackerId = AttackerId,
          DefenderId = DefenderId,
          DeclarerId = DeclarerId,
          CassusBelli = CassusBelli,
          AttackerPeaceOfferingTime = AttackerPeaceOfferingTime,
          DefenderPeaceOfferingTime = DefenderPeaceOfferingTime,
          StartTime = StartTime,
          EndTime = EndTime,
          EndReason = EndReason
        };
      }
    }
  }
}
