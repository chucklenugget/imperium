namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using System.Collections.Generic;

  public partial class Imperium : RustPlugin
  {
    class ImperiumWarOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("minCassusBelliLength")]
      public int MinCassusBelliLength;

      [JsonProperty("defensiveBonuses")]
      public List<float> DefensiveBonuses = new List<float>();

      public static ImperiumWarOptions Default = new ImperiumWarOptions {
        Enabled = true,
        MinCassusBelliLength = 50,
        DefensiveBonuses = new List<float> { 0, 0.5f, 1f }
      };
    }
  }
}
