namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using System.Collections.Generic;

  public partial class Imperium : RustPlugin
  {
    class ImperiumUpkeepOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("costs")]
      public List<int> Costs = new List<int>();

      [JsonProperty("checkIntervalMinutes")]
      public int CheckIntervalMinutes;

      [JsonProperty("collectionPeriodHours")]
      public int CollectionPeriodHours;

      [JsonProperty("gracePeriodHours")]
      public int GracePeriodHours;

      public static ImperiumUpkeepOptions Default = new ImperiumUpkeepOptions {
        Enabled = false,
        Costs = new List<int> { 10, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 },
        CheckIntervalMinutes = 15,
        CollectionPeriodHours = 24,
        GracePeriodHours = 12
      };
    }
  }
}
