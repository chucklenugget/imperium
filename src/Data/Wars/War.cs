namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    class War
    {
      public string AttackerId { get; set; }
      public string DefenderId { get; set; }
      public string DeclarerId { get; set; }

      public WarState State { get; set; }
      public WarEndReason? EndReason { get; set; }

      public DateTime DeclarationTime { get; private set; }
      public DateTime? StartTime { get; set; }
      public DateTime? AttackerPeaceOfferingTime { get; set; }
      public DateTime? DefenderPeaceOfferingTime { get; set; }
      public DateTime? EndTime { get; set; }

      public bool HasStarted
      {
        get { return StartTime != null; }
      }

      public TimeSpan DiplomacyTimeRemaining
      {
        get
        {
          if (Instance.Options.War.DiplomacyHours == 0)
            return TimeSpan.Zero;

          TimeSpan diplomacyPeriod = TimeSpan.FromHours(Instance.Options.War.DiplomacyHours);
          TimeSpan elapsed = DateTime.UtcNow.Subtract(DeclarationTime);

          if (elapsed > diplomacyPeriod)
            return TimeSpan.Zero;
          else
            return diplomacyPeriod - elapsed;
        }
      }

      public War(Faction attacker, Faction defender, User declarer, bool startImmediately)
      {
        AttackerId = attacker.Id;
        DefenderId = defender.Id;
        DeclarerId = declarer.Id;
        DeclarationTime = DateTime.Now;

        if (startImmediately)
        {
          State = WarState.Started;
          StartTime = DeclarationTime;
        }
        else
        {
          State = WarState.Declared;
        }
      }

      public War(WarInfo info)
      {
        AttackerId = info.AttackerId;
        DefenderId = info.DefenderId;
        DeclarerId = info.DeclarerId;
        State = info.State;
        EndReason = info.EndReason;
        DeclarationTime = info.DeclarationTime;
        StartTime = info.StartTime;
        AttackerPeaceOfferingTime = info.AttackerPeaceOfferingTime;
        DefenderPeaceOfferingTime = info.DefenderPeaceOfferingTime;
        EndTime = info.EndTime;
      }

      public void Start()
      {
        State = WarState.Started;
        StartTime = DateTime.UtcNow;
      }

      public void OfferPeace(Faction faction)
      {
        if (AttackerId == faction.Id)
        {
          State = WarState.AttackerOfferingPeace;
          AttackerPeaceOfferingTime = DateTime.Now;
        }
        else if (DefenderId == faction.Id)
        {
          State = WarState.DefenderOfferingPeace;
          DefenderPeaceOfferingTime = DateTime.Now;
        }
        else
        {
          throw new InvalidOperationException(String.Format("{0} tried to offer peace but the faction wasn't involved in the war!", faction.Id));
        }
      }

      public bool IsOfferingPeace(Faction faction)
      {
        return IsOfferingPeace(faction.Id);
      }

      public bool IsOfferingPeace(string factionId)
      {
        return (factionId == AttackerId && State == WarState.AttackerOfferingPeace)
          || (factionId == DefenderId && State == WarState.DefenderOfferingPeace);
      }

      public WarInfo Serialize()
      {
        return new WarInfo {
          AttackerId = AttackerId,
          DefenderId = DefenderId,
          DeclarerId = DeclarerId,
          State = State,
          EndReason = EndReason,
          DeclarationTime = DeclarationTime,
          StartTime = StartTime,
          AttackerPeaceOfferingTime = AttackerPeaceOfferingTime,
          DefenderPeaceOfferingTime = DefenderPeaceOfferingTime,
          EndTime = EndTime
        };
      }
    }
  }
}
