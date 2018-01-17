namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using Oxide.Core.Configuration;

  public partial class Imperium : RustPlugin
  {
    class ImperiumOptions
    {
      public bool EnableAreaClaims;
      public bool EnableTaxation;
      public bool EnableBadlands;
      public bool EnableTowns;
      public bool EnableDefensiveBonuses;
      public bool EnableUpkeep;
      public int MinFactionMembers;
      public int MinAreaNameLength;
      public int MinCassusBelliLength;
      public float DefaultTaxRate;
      public float MaxTaxRate;
      public float BadlandsGatherBonus;
      public List<int> ClaimCosts;
      public List<int> UpkeepCosts;
      public List<float> DefensiveBonuses;
      public int UpkeepCheckIntervalMinutes;
      public int UpkeepCollectionPeriodHours;
      public int UpkeepGracePeriodHours;
      public string MapImageUrl;
      public int MapImageSize;
      public int CommandCooldownSeconds;
    }

    ImperiumOptions LoadOptions(DynamicConfigFile file)
    {
      return new ImperiumOptions {
        EnableAreaClaims = file.Get<bool>("EnableAreaClaims"),
        EnableTaxation = file.Get<bool>("EnableTaxation"),
        EnableBadlands = file.Get<bool>("EnableBadlands"),
        EnableTowns = file.Get<bool>("EnableTowns"),
        EnableDefensiveBonuses = file.Get<bool>("EnableDefensiveBonuses"),
        EnableUpkeep = file.Get<bool>("EnableUpkeep"),
        MinFactionMembers = file.Get<int>("MinFactionMembers"),
        MinAreaNameLength = file.Get<int>("MinAreaNameLength"),
        MinCassusBelliLength = file.Get<int>("MinCassusBelliLength"),
        DefaultTaxRate = file.Get<float>("DefaultTaxRate"),
        MaxTaxRate = file.Get<float>("MaxTaxRate"),
        BadlandsGatherBonus = file.Get<float>("BadlandsGatherBonus"),
        ClaimCosts = file.Get<List<int>>("ClaimCosts"),
        UpkeepCosts = file.Get<List<int>>("UpkeepCosts"),
        UpkeepCheckIntervalMinutes = file.Get<int>("UpkeepCheckIntervalMinutes"),
        UpkeepCollectionPeriodHours = file.Get<int>("UpkeepCollectionPeriodHours"),
        UpkeepGracePeriodHours = file.Get<int>("UpkeepGracePeriodHours"),
        DefensiveBonuses = file.Get<List<float>>("DefensiveBonuses"),
        MapImageUrl = file.Get<string>("MapImageUrl"),
        MapImageSize = file.Get<int>("MapImageSize"),
        CommandCooldownSeconds = file.Get<int>("CommandCooldownSeconds")
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
      Config["EnableUpkeep"] = true;
      Config["MinFactionMembers"] = 3;
      Config["MinAreaNameLength"] = 3;
      Config["MinCassusBelliLength"] = 50;
      Config["DefaultTaxRate"] = 0.1f;
      Config["MaxTaxRate"] = 0.2f;
      Config["BadlandsGatherBonus"] = 0.1f;
      Config["ClaimCosts"] = new List<int> { 0, 100, 200, 300, 400, 500 };
      Config["UpkeepCosts"] = new List<int> { 10, 10, 20, 30, 40, 50 };
      Config["UpkeepCheckIntervalMinutes"] = 15;
      Config["UpkeepCollectionPeriodHours"] = 24;
      Config["UpkeepGracePeriodHours"] = 12;
      Config["DefensiveBonuses"] = new List<float> { 0, 0.5f, 1f };
      Config["MapImageUrl"] = "";
      Config["MapImageSize"] = 1440;
      Config["CommandCooldownSeconds"] = 10;
      SaveConfig();
    }
  }
}
