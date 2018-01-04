namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;
  using Oxide.Core.Configuration;
  using Newtonsoft.Json.Converters;

  public partial class RustFactions : RustPlugin
  {
    public enum AreaType
    {
      Wilderness,
      Claimed,
      Headquarters,
      Town,
      Badlands
    }

    enum WarEndReason
    {
      Treaty,
      AttackerEliminatedDefender,
      DefenderEliminatedAttacker
    }

    public class AreaInfo
    {
      [JsonProperty("areaId")]
      public string AreaId;

      [JsonProperty("name")]
      public string Name;

      [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
      public AreaType Type;

      [JsonProperty("factionId")]
      public string FactionId;

      [JsonProperty("claimantId")]
      public ulong? ClaimantId;

      [JsonProperty("cupboardId")]
      public uint? CupboardId;

      [JsonProperty("territoryDepth")]
      public int TerritoryDepth;
    }

    class FactionInfo
    {
      [JsonProperty("factionId")]
      public string FactionId;

      [JsonProperty("taxRate")]
      public float TaxRate;

      [JsonProperty("taxChestId")]
      public uint? TaxChestId;
    }

    class WarInfo
    {
      [JsonProperty("attackerId")]
      public string AttackerId;

      [JsonProperty("defenderId")]
      public string DefenderId;

      [JsonProperty("declarerId")]
      public ulong DeclarerId;

      [JsonProperty("cassusBelli")]
      public string CassusBelli;

      [JsonProperty("attackerPeaceOfferingTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime? AttackerPeaceOfferingTime;

      [JsonProperty("defenderPeaceOfferingTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime? DefenderPeaceOfferingTime;

      [JsonProperty("startTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime StartTime;

      [JsonProperty("endTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime? EndTime;

      [JsonProperty("endReason"), JsonConverter(typeof(StringEnumConverter))]
      public WarEndReason? EndReason;
    }

    class RustFactionsData
    {
      [JsonProperty("areas")]
      public AreaInfo[] Areas;

      [JsonProperty("factions")]
      public FactionInfo[] Factions;

      [JsonProperty("wars")]
      public WarInfo[] Wars;

      public RustFactionsData()
      {
        Areas = new AreaInfo[0];
        Factions = new FactionInfo[0];
        Wars = new WarInfo[0];
      }
    }

    RustFactionsData LoadData(RustFactions core, DynamicConfigFile file)
    {
      RustFactionsData data;

      try
      {
        data = file.ReadObject<RustFactionsData>();
      }
      catch (Exception err)
      {
        Puts(err.ToString());
        PrintWarning("Couldn't load serialized data, defaulting to an empty map.");
        data = new RustFactionsData();
      }

      return data;
    }

    void SaveData(DynamicConfigFile file)
    {
      var serialized = new RustFactionsData {
        Areas = Areas.SerializeState(),
        Factions = Factions.SerializeState(),
        Wars = Diplomacy.SerializeWars()
      };

      file.WriteObject(serialized, true);
    }
  }
}
