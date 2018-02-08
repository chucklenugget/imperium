namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using System.Collections.Generic;

  public partial class Imperium : RustPlugin
  {
    class ZoneOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("domeDarkness")]
      public int DomeDarkness;

      [JsonProperty("eventZoneRadius")]
      public float EventZoneRadius;

      [JsonProperty("eventZoneLifespanSeconds")]
      public float EventZoneLifespanSeconds;

      [JsonProperty("monumentZones")]
      public Dictionary<string, float> MonumentZones = new Dictionary<string, float>();

      public static ZoneOptions Default = new ZoneOptions {
        Enabled = true,
        DomeDarkness = 3,
        EventZoneRadius = 150f,
        EventZoneLifespanSeconds = 600f,
        MonumentZones = new Dictionary<string, float> {
          { "airfield", 200 },
          { "sphere_tank", 120 },
          { "junkyard", 150 },
          { "launch_site", 300 },
          { "military_tunnel", 150 },
          { "powerplant", 175 },
          { "satellite_dish", 130 },
          { "trainyard", 180 },
          { "water_treatment_plant", 180 }
        }
      };
    }
  }
}
