namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    class UserManager
    {
      Dictionary<string, User> Users = new Dictionary<string, User>();

      public User[] GetAll()
      {
        return Users.Values.ToArray();
      }

      public User Get(BasePlayer player)
      {
        if (player == null) return null;
        return Get(player.UserIDString);
      }

      public User Get(string userId)
      {
        User user;
        if (Users.TryGetValue(userId, out user))
          return user;
        else
          return null;
      }

      public User Find(string searchString)
      {
        User user = Get(searchString);

        if (user != null)
          return user;

        return Users.Values
          .Where(u => u.Name.ToLowerInvariant().Contains(searchString.ToLowerInvariant()))
          .OrderBy(u => GetLevenshteinDistance(searchString.ToLowerInvariant(), u.Name.ToLowerInvariant()))
          .FirstOrDefault();
      }

      public User Add(BasePlayer player)
      {
        Remove(player);

        User user = player.gameObject.AddComponent<User>();
        user.Init(player);

        Faction faction = Instance.Factions.GetByMember(user);
        if (faction != null)
          user.SetFaction(faction);

        Users[user.Player.UserIDString] = user;

        return user;
      }

      public bool Remove(BasePlayer player)
      {
        User user = Get(player);
        if (user == null) return false;

        UnityEngine.Object.DestroyImmediate(user);
        Users.Remove(player.UserIDString);

        return true;
      }

      public void Init()
      {
        List<BasePlayer> players = BasePlayer.activePlayerList;

        Instance.Puts($"Creating user objects for {players.Count} players...");

        foreach (BasePlayer player in players)
          Add(player);

        Instance.Puts($"Created {Users.Count} user objects.");
      }

      public void Destroy()
      {
        User[] users = UnityEngine.Object.FindObjectsOfType<User>();

        if (users == null)
          return;

        Instance.Puts($"Destroying {users.Length} user objects.");

        foreach (var user in users)
          UnityEngine.Object.DestroyImmediate(user);

        Users.Clear();

        Instance.Puts("User objects destroyed.");
      }
    }
  }
}
