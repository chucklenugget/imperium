namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class MapOptions
    {
      [JsonProperty("commandCooldownSeconds")]
      public int CommandCooldownSeconds;

      [JsonProperty("imageUrl")]
      public string ImageUrl;

      [JsonProperty("imageSize")]
      public int ImageSize;

      public static MapOptions Default = new MapOptions {
        CommandCooldownSeconds = 10,
        ImageUrl = "",
        ImageSize = 1440
      };
    }
  }
}
