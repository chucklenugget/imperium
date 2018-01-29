namespace Oxide.Plugins
{
  using System.Collections.Generic;

  public partial class Imperium : RustPlugin
  {
    class ImperiumOptions
    {
      public bool EnableAreaClaims;
      public bool EnableTaxation;
      public bool EnableBadlands;
      public bool EnableTowns;
      public bool EnableDefensiveBonuses;
      public bool EnableDecayReduction;
      public bool EnableUpkeep;
      public int MinFactionMembers;
      public int MinAreaNameLength;
      public int MinCassusBelliLength;
      public float DefaultTaxRate;
      public float MaxTaxRate;
      public float ClaimedLandGatherBonus;
      public float TownGatherBonus;
      public float BadlandsGatherBonus;
      public float ClaimedLandDecayReduction;
      public float TownDecayReduction;
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

    protected override void LoadDefaultConfig()
    {
      PrintWarning("Loading default configuration.");

      var options = new ImperiumOptions {
        EnableAreaClaims = true,
        EnableBadlands = true,
        EnableTaxation = true,
        EnableTowns = true,
        EnableDefensiveBonuses = true,
        EnableDecayReduction = true,
        EnableUpkeep = true,
        MinFactionMembers = 3,
        MinAreaNameLength = 3,
        MinCassusBelliLength = 50,
        DefaultTaxRate = 0.1f,
        MaxTaxRate = 0.2f,
        ClaimedLandGatherBonus = 0.1f,
        TownGatherBonus = 0.1f,
        BadlandsGatherBonus = 0.1f,
        ClaimedLandDecayReduction = 0.5f,
        TownDecayReduction = 1f,
        ClaimCosts = new List<int> { 0, 100, 200, 300, 400, 500 },
        UpkeepCosts = new List<int> { 10, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 },
        UpkeepCheckIntervalMinutes = 15,
        UpkeepCollectionPeriodHours = 24,
        UpkeepGracePeriodHours = 12,
        DefensiveBonuses = new List<float> { 0, 0.5f, 1f },
        MapImageUrl = "",
        MapImageSize = 1440,
        CommandCooldownSeconds = 10
      };

      Config.WriteObject(options, true);
    }
  }
}
