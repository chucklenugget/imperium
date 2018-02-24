namespace Oxide.Plugins
{
  using System;
  using System.Collections;
  using System.Text;

  public partial class Imperium
  {
    static class Util
    {
      const string NullString = "(null)";

      public static string Format(object obj)
      {
        if (obj == null) return NullString;

        var user = obj as User;
        if (user != null) return Format(user);

        var area = obj as Area;
        if (area != null) return Format(area);

        var entity = obj as BaseEntity;
        if (entity != null) return Format(entity);

        var list = obj as IEnumerable;
        if (list != null) return Format(list);

        return obj.ToString();
      }

      public static string Format(User user)
      {
        if (user == null)
          return NullString;
        else
          return $"{user.UserName} ({user.Id})";
      }

      public static string Format(Area area)
      {
        if (area == null)
          return NullString;
        else if (!String.IsNullOrEmpty(area.Name))
          return $"{area.Id} ({area.Name})";
        else
          return area.Id;
      }

      public static string Format(BaseEntity entity)
      {
        if (entity == null)
          return NullString;
        else if (entity.net == null)
          return "(missing networkable)";
        else
          return entity.net.ID.ToString();
      }

      public static string Format(IEnumerable items)
      {
        var sb = new StringBuilder();

        foreach (object item in items)
          sb.Append($"{Format(item)}, ");

        sb.Remove(sb.Length - 2, 2);

        return sb.ToString();
      }

      public static string NormalizeAreaId(string input)
      {
        return input.ToUpper().Trim();
      }

      public static string NormalizeAreaName(string input)
      {
        return RemoveSpecialCharacters(input.Trim());
      }

      public static string NormalizeFactionId(string input)
      {
        string factionId = input.Trim();

        if (factionId.StartsWith("[") && factionId.EndsWith("]"))
          factionId = factionId.Substring(1, factionId.Length - 2);

        return factionId;
      }

      public static string RemoveSpecialCharacters(string str)
      {
        if (string.IsNullOrEmpty(str))
          return string.Empty;

        StringBuilder sb = new StringBuilder(str.Length);
        foreach (char c in str)
        {
          if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= 'А' && c <= 'Я') || (c >= 'а' && c <= 'я') || c == ' ' || c == '.' || c == '_')
            sb.Append(c);
        }

        return sb.ToString();
      }

      public static int GetLevenshteinDistance(string source, string target)
      {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
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
}
