namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    class WarManager
    {
      List<War> Wars = new List<War>();

      public War[] GetAllActiveWars()
      {
        return Wars.Where(war => war.IsActive).OrderBy(war => war.StartTime).ToArray();
      }

      public War[] GetAllActiveWarsByFaction(Faction faction)
      {
        return GetAllActiveWarsByFaction(faction.Id);
      }

      public War[] GetAllActiveWarsByFaction(string factionId)
      {
        return GetAllActiveWars().Where(war => war.AttackerId == factionId || war.DefenderId == factionId).ToArray();
      }

      public War GetActiveWarBetween(Faction firstFaction, Faction secondFaction)
      {
        return GetActiveWarBetween(firstFaction.Id, secondFaction.Id);
      }

      public War GetActiveWarBetween(string firstFactionId, string secondFactionId)
      {
        return GetAllActiveWars().SingleOrDefault(war =>
          (war.AttackerId == firstFactionId && war.DefenderId == secondFactionId) ||
          (war.DefenderId == firstFactionId && war.AttackerId == secondFactionId)
        );
      }

      public War DeclareWar(Faction attacker, Faction defender, User user, string cassusBelli)
      {
        var war = new War(attacker, defender, user, cassusBelli);
        Wars.Add(war);
        Instance.OnDiplomacyChanged();
        return war;
      }

      public void EndWar(War war, WarEndReason reason)
      {
        war.EndTime = DateTime.UtcNow;
        war.EndReason = reason;
        Instance.OnDiplomacyChanged();
      }

      public void EndAllWarsForEliminatedFactions()
      {
        bool dirty = false;

        foreach (War war in Wars)
        {
          if (Instance.Areas.GetAllClaimedByFaction(war.AttackerId).Length == 0)
          {
            war.EndTime = DateTime.UtcNow;
            war.EndReason = WarEndReason.DefenderEliminatedAttacker;
            dirty = true;
          }
          if (Instance.Areas.GetAllClaimedByFaction(war.DefenderId).Length == 0)
          {
            war.EndTime = DateTime.UtcNow;
            war.EndReason = WarEndReason.AttackerEliminatedDefender;
            dirty = true;
          }
        }

        if (dirty)
          Instance.OnDiplomacyChanged();
      }

      public void Init(IEnumerable<WarInfo> warInfos)
      {
        Instance.Puts($"Loading {warInfos.Count()} wars...");

        foreach (WarInfo info in warInfos)
          Wars.Add(new War(info));

        Instance.Puts("Wars loaded.");
      }

      public void Destroy()
      {
        Wars.Clear();
      }

      public WarInfo[] Serialize()
      {
        return Wars.Select(war => war.Serialize()).ToArray();
      }
    }
  }
}