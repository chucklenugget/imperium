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
    }

    void LoadData(RustFactions plugin, DynamicConfigFile file)
    {
      try
      {
        var data = file.ReadObject<RustFactionsData>();
        if (data.Claims != null) Claims = new ClaimManager(plugin, data.Claims);
        if (data.TaxPolicies != null) Taxes = new TaxManager(plugin, data.TaxPolicies);
        if (data.BadlandsAreas != null) Badlands = new BadlandsManager(plugin, data.BadlandsAreas);
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
        BadlandsAreas = Badlands.Serialize()
      };

      file.WriteObject(serialized, true);
    }
  }
}
