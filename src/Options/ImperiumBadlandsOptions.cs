namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class ImperiumBadlandsOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      public static ImperiumBadlandsOptions Default = new ImperiumBadlandsOptions {
        Enabled = true
      };
    }
  }
}
