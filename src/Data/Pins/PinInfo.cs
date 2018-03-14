namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;
  using UnityEngine;

  public partial class Imperium : RustPlugin
  {
    class PinInfo
    {
      [JsonProperty("name")]
      public string Name;

      [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
      public PinType Type;

      [JsonProperty("position"), JsonConverter(typeof(UnityVector3Converter))]
      public Vector3 Position;

      [JsonProperty("areaId")]
      public string AreaId;

      [JsonProperty("creatorId")]
      public string CreatorId;
    }
  }
}
