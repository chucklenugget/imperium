namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class TaxOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("defaultTaxRate")]
      public float DefaultTaxRate;

      [JsonProperty("maxTaxRate")]
      public float MaxTaxRate;

      [JsonProperty("claimedLandGatherBonus")]
      public float ClaimedLandGatherBonus;

      [JsonProperty("badlandsGatherBonus")]
      public float BadlandsGatherBonus;

      public static TaxOptions Default = new TaxOptions {
        Enabled = true,
        DefaultTaxRate = 0.1f,
        MaxTaxRate = 0.2f,
        ClaimedLandGatherBonus = 0.1f,
        BadlandsGatherBonus = 0.1f
      };
    }
  }
}
