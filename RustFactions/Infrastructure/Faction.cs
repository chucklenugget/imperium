namespace Oxide.Plugins
{
  using Newtonsoft.Json.Linq;
  using System.Collections.Generic;
  using System.Linq;

  public partial class RustFactions
  {
    public class Faction
    {
      public string Id;
      public string Description;
      public string OwnerSteamId;
      public HashSet<string> ModeratorSteamIds;
      public HashSet<string> MemberSteamIds;

      public Faction(JObject clanData)
      {
        Id = clanData["tag"].ToString();
        Description = clanData["description"].ToString();
        OwnerSteamId = clanData["owner"].ToString();
        ModeratorSteamIds = new HashSet<string>(clanData["moderators"].Select(token => token.ToString()));
        MemberSteamIds = new HashSet<string>(clanData["members"].Select(token => token.ToString()));
      }

      public bool IsLeader(BasePlayer player)
      {
        return (player.UserIDString == OwnerSteamId) || (ModeratorSteamIds.Contains(player.UserIDString));
      }
    }
  }
}
