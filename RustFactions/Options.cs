namespace Oxide.Plugins
{
  using System;
  using Oxide.Core.Configuration;

  public partial class RustFactions : RustPlugin
  {
    class RustFactionsOptions
    {
      public bool EnableAreaClaims;
      public bool EnableTaxation;
      public bool EnableBadlands;
      public int MinFactionMembers;
      public int DefaultTaxRate;
      public int MaxTaxRate;
      public int BadlandsGatherBonus;
      public int BaseClaimCost;
      public string MapImageUrl;
      public int MapImageSize;
    }

    RustFactionsOptions LoadOptions(DynamicConfigFile file)
    {
      return new RustFactionsOptions {
        EnableAreaClaims = Convert.ToBoolean(file["EnableAreaClaims"]),
        EnableTaxation = Convert.ToBoolean(file["EnableTaxation"]),
        EnableBadlands = Convert.ToBoolean(file["EnableBadlands"]),
        MinFactionMembers = Convert.ToInt32(file["MinFactionMembers"]),
        DefaultTaxRate = Convert.ToInt32(file["DefaultTaxRate"]),
        MaxTaxRate = Convert.ToInt32(file["MaxTaxRate"]),
        BadlandsGatherBonus = Convert.ToInt32(file["BadlandsGatherBonus"]),
        BaseClaimCost = Convert.ToInt32(file["BaseClaimCost"]),
        MapImageUrl = Convert.ToString(file["MapImageUrl"]),
        MapImageSize = Convert.ToInt32(file["MapImageSize"])
      };
    }

    protected override void LoadDefaultConfig()
    {
      PrintWarning("Loading default configuration.");
      Config.Clear();
      Config["EnableAreaClaims"] = true;
      Config["EnableTaxation"] = true;
      Config["EnableBadlands"] = true;
      Config["MinFactionMembers"] = 3;
      Config["DefaultTaxRate"] = 10;
      Config["MaxTaxRate"] = 20;
      Config["BadlandsGatherBonus"] = 10;
      Config["BaseClaimCost"] = 100;
      Config["MapImageUrl"] = "";
      Config["MapImageSize"] = 1024;
      SaveConfig();
    }
  }
}
