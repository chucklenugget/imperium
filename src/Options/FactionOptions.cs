namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class FactionOptions
    {
      [JsonProperty("minFactionNameLength")]
      public int MinFactionNameLength;

      [JsonProperty("maxFactionNameLength")]
      public int MaxFactionNameLength;

      [JsonProperty("maxMembers")]
      public int? MaxMembers;

      public static FactionOptions Default = new FactionOptions {
        MinFactionNameLength = 1,
        MaxFactionNameLength = 8,
        MaxMembers = null
      };
    }
  }
}
