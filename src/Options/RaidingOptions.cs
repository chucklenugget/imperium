namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class RaidingOptions
    {
      [JsonProperty("allowedInBadlands")]
      public bool AllowedInBadlands;

      [JsonProperty("allowedInClaimedLand")]
      public bool AllowedInClaimedLand;

      [JsonProperty("allowedInTowns")]
      public bool AllowedInTowns;

      [JsonProperty("allowedInWilderness")]
      public bool AllowedInWilderness;

      public static RaidingOptions Default = new RaidingOptions {
        AllowedInBadlands = true,
        AllowedInClaimedLand = true,
        AllowedInTowns = true,
        AllowedInWilderness = true
      };
    }
  }
}
