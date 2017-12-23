namespace Oxide.Plugins
{
  using Newtonsoft.Json.Linq;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  public static class ExtensionMethods
  {
    public static string RemoveSpecialCharacters(this string str)
    {
      StringBuilder sb = new StringBuilder(str.Length);
      foreach (char c in str)
      {
        if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= 'А' && c <= 'Я') || (c >= 'а' && c <= 'я') || c == '.' || c == '_')
          sb.Append(c);
      }
      return sb.ToString();
    }
  }

  public partial class RustFactions
  {
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
