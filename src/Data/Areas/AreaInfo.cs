namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;

  public partial class Imperium : RustPlugin
  {
    public class AreaInfo
    {
      [JsonProperty("id")]
      public string Id;

      [JsonProperty("name")]
      public string Name;

      [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
      public AreaType Type;

      [JsonProperty("factionId")]
      public string FactionId;

      [JsonProperty("claimantId")]
      public string ClaimantId;

      [JsonProperty("cupboardId")]
      public uint? CupboardId;
    }
  }
}
