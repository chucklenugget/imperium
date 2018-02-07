namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class ImperiumTaxesOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("defaultTaxRate")]
      public float DefaultTaxRate;

      [JsonProperty("maxTaxRate")]
      public float MaxTaxRate;

      [JsonProperty("claimedLandGatherBonus")]
      public float ClaimedLandGatherBonus;

      [JsonProperty("townGatherBonus")]
      public float TownGatherBonus;

      [JsonProperty("badlandsGatherBonus")]
      public float BadlandsGatherBonus;

      public static ImperiumTaxesOptions Default = new ImperiumTaxesOptions {
        Enabled = true,
        DefaultTaxRate = 0.1f,
        MaxTaxRate = 0.2f,
        ClaimedLandGatherBonus = 0.1f,
        TownGatherBonus = 0.1f,
        BadlandsGatherBonus = 0.1f
      };
    }
  }
}
