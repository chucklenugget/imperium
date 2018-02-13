namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class FactionOptions
    {
      [JsonProperty("maxMembers")]
      public int? MaxMembers;

      public static FactionOptions Default = new FactionOptions {
        MaxMembers = null
      };
    }
  }
}
