namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class RustFactions
  {
    class DiplomacyManager : RustFactionsManager
    {
      List<War> Wars = new List<War>();

      public DiplomacyManager(RustFactions core)
        : base(core)
      {
      }

      public War[] GetAllActiveWars()
      {
        return Wars.Where(war => war.IsActive).ToArray();
      }

      public War[] GetAllActiveWarsByFaction(Faction faction)
      {
        return GetAllActiveWarsByFaction(faction);
      }

      public War[] GetAllActiveWarsByFaction(string factionId)
      {
        return Wars.Where(war => war.AttackerId == factionId || war.DefenderId == factionId).ToArray();
      }

      public War GetActiveWarBetween(Faction firstFaction, Faction secondFaction)
      {
        return GetActiveWarBetween(firstFaction.Id, secondFaction.Id);
      }

      public War GetActiveWarBetween(string firstFactionId, string secondFactionId)
      {
        return Wars.SingleOrDefault(war =>
          war.IsActive &&
          (war.AttackerId == firstFactionId && war.DefenderId == secondFactionId) ||
          (war.DefenderId == firstFactionId && war.AttackerId == secondFactionId)
        );
      }

      public War DeclareWar(Faction attacker, Faction defender, User user, string cassusBelli)
      {
        var war = new War(attacker, defender, user, cassusBelli);
        Wars.Add(war);
        Core.OnDiplomacyChanged();
        return war;
      }

      public void EndWar(War war)
      {
        war.EndTime = DateTime.Now;
        Core.OnDiplomacyChanged();
      }

      public void Init(WarInfo[] warInfos)
      {
        Puts($"Loading {warInfos.Length} wars...");

        foreach (WarInfo info in warInfos)
          Wars.Add(new War(info));

        Puts("Wars loaded.");
      }

      public void Destroy()
      {
        Wars.Clear();
      }

      public WarInfo[] SerializeWars()
      {
        return Wars.Select(war => war.Serialize()).ToArray();
      }
    }
  }
}