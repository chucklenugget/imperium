namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class BadlandsOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      public static BadlandsOptions Default = new BadlandsOptions {
        Enabled = true
      };
    }
  }
}
