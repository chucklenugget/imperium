namespace Oxide.Plugins
{
  using Newtonsoft.Json.Linq;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  public partial class Imperium
  {
    string FormatList(IEnumerable<string> items)
    {
      return String.Join(", ", items.ToArray());
    }

    public static string RemoveSpecialCharacters(string str)
    {
      if (String.IsNullOrEmpty(str))
        return String.Empty;

      StringBuilder sb = new StringBuilder(str.Length);
      foreach (char c in str)
      {
        if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= 'А' && c <= 'Я') || (c >= 'а' && c <= 'я') || c == ' ' || c == '.' || c == '_')
          sb.Append(c);
      }

      return sb.ToString();
    }

    string NormalizeAreaId(string input)
    {
      return input.ToUpper().Trim();
    }

    string NormalizeName(string input)
    {
      return RemoveSpecialCharacters(input.Trim());
    }

    string NormalizeFactionId(string input)
    {
      string factionId = input.Trim();

      if (factionId.StartsWith("[") && factionId.EndsWith("]"))
        factionId = factionId.Substring(1, factionId.Length - 2);

      return factionId;
    }

  }

}
