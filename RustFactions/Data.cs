namespace Oxide.Plugins
{
  using System;
  using Oxide.Core.Configuration;

  public partial class RustFactions : RustPlugin
  {
    class RustFactionsData
    {
      public Claim[] Claims;
      public TaxPolicy[] TaxPolicies;
      public string[] BadlandsAreas;
      public Town[] Towns;
    }

    void LoadData(RustFactions plugin, DynamicConfigFile file)
    {
      try
      {
        var data = file.ReadObject<RustFactionsData>();
        if (data.Claims != null) Claims.Load(data.Claims);
        if (data.TaxPolicies != null) Taxes.Load(data.TaxPolicies);
        if (data.BadlandsAreas != null) Badlands.Load(data.BadlandsAreas);
        if (data.Towns != null) Towns.Load(data.Towns);
      }
      catch (Exception err)
      {
        Puts(err.ToString());
        PrintWarning("Couldn't load serialized data, defaulting to an empty map.");
      }
    }

    void SaveData(DynamicConfigFile file)
    {
      var serialized = new RustFactionsData {
        Claims = Claims.Serialize(),
        TaxPolicies = Taxes.Serialize(),
        BadlandsAreas = Badlands.Serialize(),
        Towns = Towns.Serialize()
      };

      file.WriteObject(serialized, true);
    }
  }
}
