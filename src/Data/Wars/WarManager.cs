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

      public War[] GetAllWars()
      {
        return Wars.Where(war => war.State != WarState.Ended).OrderBy(war => war.DeclarationTime).ToArray();
      }

      public War[] GetWarsByFaction(Faction faction)
      {
        return GetWarsByFaction(faction.Id);
      }

      public War[] GetWarsByFaction(string factionId)
      {
        return GetAllWars().Where(war => war.AttackerId == factionId || war.DefenderId == factionId).ToArray();
      }

      public War GetWarBetween(Faction firstFaction, Faction secondFaction)
      {
        return GetWarBetween(firstFaction.Id, secondFaction.Id);
      }

      public War GetWarBetween(string firstFactionId, string secondFactionId)
      {
        return GetAllWars().SingleOrDefault(war =>
          (war.AttackerId == firstFactionId && war.DefenderId == secondFactionId) ||
          (war.DefenderId == firstFactionId && war.AttackerId == secondFactionId)
        );
      }

      public bool AreFactionsAtWar(Faction firstFaction, Faction secondFaction)
      {
        return AreFactionsAtWar(firstFaction.Id, secondFaction.Id);
      }

      public bool AreFactionsAtWar(string firstFactionId, string secondFactionId)
      {
        War war = GetWarBetween(firstFactionId, secondFactionId);
        return war != null && war.HasStarted;
      }

      public War DeclareWar(Faction attacker, Faction defender, User user, bool startImmediately)
      {
        var war = new War(attacker, defender, user, startImmediately);
        Wars.Add(war);
        Instance.OnDiplomacyChanged();
        return war;
      }

      public War AcceptWar(War war)
      {
        war.Start();
        Instance.OnDiplomacyChanged();
        return war;
      }

      public void EndWar(War war, WarEndReason reason)
      {
        war.EndTime = DateTime.UtcNow;
        war.EndReason = reason;
        Instance.OnDiplomacyChanged();
      }

      public void CheckDiplomacyTimersForAllWars()
      {
        foreach (War war in Wars.Where(w => w.State == WarState.Declared && w.DiplomacyTimeRemaining == TimeSpan.Zero))
        {
          war.Start();
          Instance.PrintToChat(Messages.WarStartedAnnouncement, war.AttackerId, war.DefenderId);
          Instance.Log($"[WAR] Diplomacy timer ended for war between {war.AttackerId} and {war.DefenderId}, starting war");
          Instance.OnDiplomacyChanged();
        }
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
        {
          var war = new War(info);
          Wars.Add(war);
          Instance.Log($"[LOAD] War {war.AttackerId} vs {war.DefenderId}, state = {war.State}");
        }

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