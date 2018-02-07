namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class ImperiumOptions
    {
      [JsonProperty("badlands")]
      public ImperiumBadlandsOptions Badlands;

      [JsonProperty("claims")]
      public ImperiumClaimOptions Claims;

      [JsonProperty("decay")]
      public ImperiumDecayOptions Decay;

      [JsonProperty("map")]
      public ImperiumMapOptions Map;

      [JsonProperty("pvp")]
      public ImperiumPvpOptions Pvp;

      [JsonProperty("raiding")]
      public ImperiumRaidingOptions Raiding;

      [JsonProperty("taxes")]
      public ImperiumTaxesOptions Taxes;

      [JsonProperty("towns")]
      public ImperiumTownOptions Towns;

      [JsonProperty("upkeep")]
      public ImperiumUpkeepOptions Upkeep;

      [JsonProperty("war")]
      public ImperiumWarOptions War;

      [JsonProperty("zones")]
      public ImperiumZonesOptions Zones;

      public static ImperiumOptions Default = new ImperiumOptions {
        Badlands = ImperiumBadlandsOptions.Default,
        Claims = ImperiumClaimOptions.Default,
        Decay = ImperiumDecayOptions.Default,
        Map = ImperiumMapOptions.Default,
        Pvp = ImperiumPvpOptions.Default,
        Raiding = ImperiumRaidingOptions.Default,
        Towns = ImperiumTownOptions.Default,
        Taxes = ImperiumTaxesOptions.Default,
        Upkeep = ImperiumUpkeepOptions.Default,
        War = ImperiumWarOptions.Default,
        Zones = ImperiumZonesOptions.Default
      };
    }

    protected override void LoadDefaultConfig()
    {
      PrintWarning("Loading default configuration.");
      Config.WriteObject(ImperiumOptions.Default, true);
    }
  }
}
