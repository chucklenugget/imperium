namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class ImperiumDecayOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("claimedLandDecayReduction")]
      public float ClaimedLandDecayReduction;

      [JsonProperty("townDecayReduction")]
      public float TownDecayReduction;

      public static ImperiumDecayOptions Default = new ImperiumDecayOptions {
        Enabled = false,
        ClaimedLandDecayReduction = 0.5f,
        TownDecayReduction = 1f
      };
    }
  }
}
