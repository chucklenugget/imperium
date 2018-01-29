namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  public partial class Imperium
  {
    static string FormatList(IEnumerable<string> items)
    {
      return String.Join(", ", items.ToArray());
    }

    static string RemoveSpecialCharacters(string str)
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

    static string NormalizeAreaId(string input)
    {
      return input.ToUpper().Trim();
    }

    static string NormalizeName(string input)
    {
      return RemoveSpecialCharacters(input.Trim());
    }

    static string NormalizeFactionId(string input)
    {
      string factionId = input.Trim();

      if (factionId.StartsWith("[") && factionId.EndsWith("]"))
        factionId = factionId.Substring(1, factionId.Length - 2);

      return factionId;
    }

    static int GetLevenshteinDistance(string source, string target)
    {
      if (String.IsNullOrEmpty(source) && String.IsNullOrEmpty(target))
        return 0;

      if (source.Length == target.Length)
        return source.Length;

      if (source.Length == 0)
        return target.Length;

      if (target.Length == 0)
        return source.Length;

      var distance = new int[source.Length + 1, target.Length + 1];

      for (int idx = 0; idx <= source.Length; distance[idx, 0] = idx++) ;
      for (int idx = 0; idx <= target.Length; distance[0, idx] = idx++) ;

      for (int i = 1; i <= source.Length; i++)
      {
        for (int j = 1; j <= target.Length; j++)
        {
          int cost = target[j - 1] == source[i - 1] ? 0 : 1;
          distance[i, j] = Math.Min(
            Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
            distance[i - 1, j - 1] + cost
          );
        }
      }

      return distance[source.Length, target.Length];
    }
  }
}
