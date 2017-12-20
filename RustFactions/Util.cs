namespace Oxide.Plugins
{
  using Newtonsoft.Json.Linq;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  public partial class RustFactions
  {
    
    void SendMessage(BasePlayer player, string messageName)
    {
      SendReply(player, lang.GetMessage(messageName, this, player.UserIDString));
    }

    void SendMessage(BasePlayer player, string messageName, params object[] args)
    {
      SendReply(player, String.Format(lang.GetMessage(messageName, this, player.UserIDString), args));
    }

    void SendMessage(BasePlayer player, StringBuilder sb)
    {
      SendReply(player, sb.ToString().TrimEnd());
    }

    void Announce(string message, params object[] args)
    {
      // TODO: Localize text
      PrintToChat(String.Format(message, args));
    }

    string FormatList(IEnumerable<string> items)
    {
      return String.Join(", ", items.ToArray());
    }

    Faction GetFactionForPlayer(BasePlayer player)
    {
      var name = Clans?.Call<string>("GetClanOf", player.userID);

      if (String.IsNullOrEmpty(name))
        return null;

      return GetFaction(name);
    }

    Faction GetFaction(string name)
    {
      var clanData = Clans?.Call<JObject>("GetClan", name);
      if (clanData == null)
        return null;
      else
        return new Faction(clanData);
    }

    string NormalizeAreaId(string input)
    {
      return input.ToUpper().Trim();
    }

    string NormalizeFactionId(string input)
    {
      string factionId = input.ToUpper().Trim();

      if (factionId.StartsWith("[") && factionId.EndsWith("]"))
        factionId = factionId.Substring(1, factionId.Length - 2);

      return factionId;
    }
    
  }

}
