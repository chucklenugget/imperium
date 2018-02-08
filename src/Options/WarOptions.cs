namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using System.Collections.Generic;

  public partial class Imperium : RustPlugin
  {
    class WarOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("minCassusBelliLength")]
      public int MinCassusBelliLength;

      [JsonProperty("defensiveBonuses")]
      public List<float> DefensiveBonuses = new List<float>();

      public static WarOptions Default = new WarOptions {
        Enabled = true,
        MinCassusBelliLength = 50,
        DefensiveBonuses = new List<float> { 0, 0.5f, 1f }
      };
    }
  }
}
