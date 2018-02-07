namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class ImperiumTownOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      public static ImperiumTownOptions Default = new ImperiumTownOptions {
        Enabled = true
      };
    }
  }
}
