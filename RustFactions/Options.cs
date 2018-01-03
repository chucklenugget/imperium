namespace Oxide.Plugins
{
  using Oxide.Core.Configuration;
  using System.Collections.Generic;

  public partial class RustFactions : RustPlugin
  {
    class RustFactionsOptions
    {
      public bool EnableAreaClaims;
      public bool EnableTaxation;
      public bool EnableBadlands;
      public bool EnableTowns;
      public bool EnableDefensiveBonuses;
      public int MinFactionMembers;
      public int MinAreaNameLength;
      public int MinCassusBelliLength;
      public float DefaultTaxRate;
      public float MaxTaxRate;
      public float BadlandsGatherBonus;
      public List<int> ClaimCosts;
      public List<float> DefensiveBonuses;
      public string MapImageUrl;
      public int MapImageSize;
      public string RulesUrl;
    }

    RustFactionsOptions LoadOptions(DynamicConfigFile file)
    {
      return new RustFactionsOptions {
        EnableAreaClaims = file.Get<bool>("EnableAreaClaims"),
        EnableTaxation = file.Get<bool>("EnableTaxation"),
        EnableBadlands = file.Get<bool>("EnableBadlands"),
        EnableTowns = file.Get<bool>("EnableTowns"),
        EnableDefensiveBonuses = file.Get<bool>("EnableDefensiveBonuses"),
        MinFactionMembers = file.Get<int>("MinFactionMembers"),
        MinAreaNameLength = file.Get<int>("MinAreaNameLength"),
        MinCassusBelliLength = file.Get<int>("MinCassusBelliLength"),
        DefaultTaxRate = file.Get<float>("DefaultTaxRate"),
        MaxTaxRate = file.Get<float>("MaxTaxRate"),
        BadlandsGatherBonus = file.Get<float>("BadlandsGatherBonus"),
        ClaimCosts = file.Get<List<int>>("ClaimCosts"),
        DefensiveBonuses = file.Get<List<float>>("DefensiveBonuses"),
        MapImageUrl = file.Get<string>("MapImageUrl"),
        MapImageSize = file.Get<int>("MapImageSize"),
        RulesUrl = file.Get<string>("RulesUrl")
      };
    }

    protected override void LoadDefaultConfig()
    {
      PrintWarning("Loading default configuration.");
      Config.Clear();
      Config["EnableAreaClaims"] = true;
      Config["EnableTaxation"] = true;
      Config["EnableBadlands"] = true;
      Config["EnableTowns"] = true;
      Config["EnableDefensiveBonuses"] = true;
      Config["MinFactionMembers"] = 3;
      Config["MinAreaNameLength"] = 3;
      Config["MinCassusBelliLength"] = 50;
      Config["DefaultTaxRate"] = 0.1f;
      Config["MaxTaxRate"] = 0.2f;
      Config["BadlandsGatherBonus"] = 0.1f;
      Config["ClaimCosts"] = new List<int> { 0, 100, 200, 300, 400, 500 };
      Config["DefensiveBonuses"] = new List<float> { 0, 0.5f, 1f };
      Config["MapImageUrl"] = "";
      Config["MapImageSize"] = 1440;
      Config["RulesUrl"] = "";
      SaveConfig();
    }
  }
}
