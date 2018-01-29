namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;

  public partial class Imperium : RustPlugin
  {
    class WarInfo
    {
      [JsonProperty("attackerId")]
      public string AttackerId;

      [JsonProperty("defenderId")]
      public string DefenderId;

      [JsonProperty("declarerId")]
      public string DeclarerId;

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
  }
}
