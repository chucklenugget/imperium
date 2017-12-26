namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;
  using Oxide.Core.Configuration;
  using Newtonsoft.Json.Converters;

  public partial class RustFactions : RustPlugin
  {
    public enum AreaType
    {
      Unclaimed,
      Claimed,
      Headquarters,
      Town,
      Badlands
    }

    public class AreaInfo
    {
      [JsonProperty("areaId")]
      public string AreaId;

      [JsonProperty("name")]
      public string Name;

      [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
      public AreaType Type;

      [JsonProperty("factionId")]
      public string FactionId;

      [JsonProperty("claimantId")]
      public ulong? ClaimantId;

      [JsonProperty("cupboardId")]
      public uint? CupboardId;
    }

    class FactionInfo
    {
      [JsonProperty("factionId")]
      public string FactionId;

      [JsonProperty("taxRate")]
      public int TaxRate;

      [JsonProperty("taxChestId")]
      public uint? TaxChestId;
    }

    class RustFactionsData
    {
      [JsonProperty("areas")]
      public AreaInfo[] Areas;

      [JsonProperty("factions")]
      public FactionInfo[] Factions;
    }

    RustFactionsData LoadData(RustFactions core, DynamicConfigFile file)
    {
      RustFactionsData data;

      try
      {
        data = file.ReadObject<RustFactionsData>();
      }
      catch (Exception err)
      {
        Puts(err.ToString());
        PrintWarning("Couldn't load serialized data, defaulting to an empty map.");

        data = new RustFactionsData {
          Areas = new AreaInfo[0],
          Factions = new FactionInfo[0]
        };
      }

      return data;
    }

    void SaveData(DynamicConfigFile file)
    {
      var serialized = new RustFactionsData {
        Areas = Areas.SerializeState(),
        Factions = Factions.SerializeState()
      };

      file.WriteObject(serialized, true);
    }
  }
}
