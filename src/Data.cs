namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;
  using Oxide.Core.Configuration;
  using Newtonsoft.Json.Converters;

  public partial class Imperium : RustPlugin
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
    }

    class FactionInfo
    {
      [JsonProperty("factionId")]
      public string FactionId;

      [JsonProperty("taxRate")]
      public float TaxRate;

      [JsonProperty("taxChestId")]
      public uint? TaxChestId;

      [JsonProperty("nextUpkeepPaymentTime")]
      public DateTime NextUpkeepPaymentTime;
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

    class ImperiumSavedData
    {
      [JsonProperty("areas")]
      public AreaInfo[] Areas;

      [JsonProperty("factions")]
      public FactionInfo[] Factions;

      [JsonProperty("wars")]
      public WarInfo[] Wars;

      public ImperiumSavedData()
      {
        Areas = new AreaInfo[0];
        Factions = new FactionInfo[0];
        Wars = new WarInfo[0];
      }
    }

    ImperiumSavedData LoadData(Imperium core, DynamicConfigFile file)
    {
      ImperiumSavedData data;

      try
      {
        data = file.ReadObject<ImperiumSavedData>();
      }
      catch (Exception err)
      {
        Puts(err.ToString());
        PrintWarning("Couldn't load serialized data, defaulting to an empty map.");
        data = new ImperiumSavedData();
      }

      return data;
    }

    void SaveData(DynamicConfigFile file)
    {
      var serialized = new ImperiumSavedData {
        Areas = Areas.SerializeState(),
        Factions = Factions.SerializeState(),
        Wars = Wars.SerializeWars()
      };

      file.WriteObject(serialized, true);
    }
  }
}
