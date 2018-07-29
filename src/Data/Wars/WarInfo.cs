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

      [JsonProperty("state"), JsonConverter(typeof(StringEnumConverter))]
      public WarState State;

      [JsonProperty("endReason"), JsonConverter(typeof(StringEnumConverter))]
      public WarEndReason? EndReason;

      [JsonProperty("startTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime DeclarationTime;

      [JsonProperty("startTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime? StartTime;

      [JsonProperty("attackerPeaceOfferingTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime? AttackerPeaceOfferingTime;

      [JsonProperty("defenderPeaceOfferingTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime? DefenderPeaceOfferingTime;

      [JsonProperty("endTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime? EndTime;
    }
  }
}
