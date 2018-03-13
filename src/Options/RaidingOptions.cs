namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class RaidingOptions
    {
      [JsonProperty("restrictRaiding")]
      public bool RestrictRaiding;

      [JsonProperty("allowedInBadlands")]
      public bool AllowedInBadlands;

      [JsonProperty("allowedInClaimedLand")]
      public bool AllowedInClaimedLand;

      [JsonProperty("allowedInWilderness")]
      public bool AllowedInWilderness;

      public static RaidingOptions Default = new RaidingOptions {
        RestrictRaiding = false,
        AllowedInBadlands = true,
        AllowedInClaimedLand = true,
        AllowedInWilderness = true
      };
    }
  }
}
