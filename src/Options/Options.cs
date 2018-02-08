namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class ImperiumOptions
    {
      [JsonProperty("badlands")]
      public BadlandsOptions Badlands;

      [JsonProperty("claims")]
      public ClaimOptions Claims;

      [JsonProperty("decay")]
      public DecayOptions Decay;

      [JsonProperty("map")]
      public MapOptions Map;

      [JsonProperty("pvp")]
      public PvpOptions Pvp;

      [JsonProperty("raiding")]
      public RaidingOptions Raiding;

      [JsonProperty("taxes")]
      public TaxOptions Taxes;

      [JsonProperty("towns")]
      public TownOptions Towns;

      [JsonProperty("upkeep")]
      public UpkeepOptions Upkeep;

      [JsonProperty("war")]
      public WarOptions War;

      [JsonProperty("zones")]
      public ZoneOptions Zones;

      public static ImperiumOptions Default = new ImperiumOptions {
        Badlands = BadlandsOptions.Default,
        Claims = ClaimOptions.Default,
        Decay = DecayOptions.Default,
        Map = MapOptions.Default,
        Pvp = PvpOptions.Default,
        Raiding = RaidingOptions.Default,
        Towns = TownOptions.Default,
        Taxes = TaxOptions.Default,
        Upkeep = UpkeepOptions.Default,
        War = WarOptions.Default,
        Zones = ZoneOptions.Default
      };
    }

    protected override void LoadDefaultConfig()
    {
      PrintWarning("Loading default configuration.");
      Config.WriteObject(ImperiumOptions.Default, true);
    }
  }
}
