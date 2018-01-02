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

      public DateTime StartTime { get; private set; }
      public DateTime? EndTime { get; set; }

      public bool IsActive
      {
        get { return EndTime != null; }
      }

      public War(Faction attacker, Faction defender, User declarer, string cassusBelli)
      {
        AttackerId = attacker.Id;
        DefenderId = defender.Id;
        DeclarerId = declarer.Id;
        CassusBelli = cassusBelli;
        StartTime = DateTime.UtcNow;
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

      public WarInfo Serialize()
      {
        return new WarInfo {
          AttackerId = AttackerId,
          DefenderId = DefenderId,
          DeclarerId = DeclarerId,
          CassusBelli = CassusBelli,
          StartTime = StartTime,
          EndTime = EndTime
        };
      }
    }
  }
}
