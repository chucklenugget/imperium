namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class ImperiumMapOptions
    {
      [JsonProperty("commandCooldownSeconds")]
      public int CommandCooldownSeconds;

      [JsonProperty("imageUrl")]
      public string ImageUrl;

      [JsonProperty("imageSize")]
      public int ImageSize;

      public static ImperiumMapOptions Default = new ImperiumMapOptions {
        CommandCooldownSeconds = 10,
        ImageUrl = "",
        ImageSize = 1440
      };
    }
  }
}
