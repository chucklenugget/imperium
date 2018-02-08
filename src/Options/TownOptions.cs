namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class TownOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      public static TownOptions Default = new TownOptions {
        Enabled = true
      };
    }
  }
}
