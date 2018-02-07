namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class ImperiumPvpOptions
    {
      [JsonProperty("allowedInBadlands")]
      public bool AllowedInBadlands;

      [JsonProperty("allowedInClaimedLand")]
      public bool AllowedInClaimedLand;

      [JsonProperty("allowedInTowns")]
      public bool AllowedInTowns;

      [JsonProperty("allowedInWilderness")]
      public bool AllowedInWilderness;

      [JsonProperty("allowedInEventZones")]
      public bool AllowedInEventZones;

      [JsonProperty("allowedInMonumentZones")]
      public bool AllowedInMonumentZones;

      public static ImperiumPvpOptions Default = new ImperiumPvpOptions {
        AllowedInBadlands = true,
        AllowedInClaimedLand = true,
        AllowedInEventZones = true,
        AllowedInMonumentZones = true,
        AllowedInTowns = true,
        AllowedInWilderness = true,
      };
    }
  }
}
