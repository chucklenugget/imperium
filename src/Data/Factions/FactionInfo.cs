namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class FactionInfo
    {
      [JsonProperty("id")]
      public string Id;

      [JsonProperty("ownerId")]
      public string OwnerId;

      [JsonProperty("memberIds")]
      public string[] MemberIds;

      [JsonProperty("managerIds")]
      public string[] ManagerIds;

      [JsonProperty("inviteIds")]
      public string[] InviteIds;

      [JsonProperty("taxRate")]
      public float TaxRate;

      [JsonProperty("taxChestId")]
      public uint? TaxChestId;

      [JsonProperty("nextUpkeepPaymentTime")]
      public DateTime NextUpkeepPaymentTime;
    }
  }
}
