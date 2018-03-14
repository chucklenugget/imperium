namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class MapOptions
    {
      [JsonProperty("pinsEnabled")]
      public bool PinsEnabled;

      [JsonProperty("minPinNameLength")]
      public int MinPinNameLength;

      [JsonProperty("maxPinNameLength")]
      public int MaxPinNameLength;

      [JsonProperty("pinCost")]
      public int PinCost;

      [JsonProperty("commandCooldownSeconds")]
      public int CommandCooldownSeconds;

      [JsonProperty("imageUrl")]
      public string ImageUrl;

      [JsonProperty("imageSize")]
      public int ImageSize;

      public static MapOptions Default = new MapOptions {
        PinsEnabled = true,
        MinPinNameLength = 2,
        MaxPinNameLength = 20,
        PinCost = 100,
        CommandCooldownSeconds = 10,
        ImageUrl = "",
        ImageSize = 1440
      };
    }
  }
}
