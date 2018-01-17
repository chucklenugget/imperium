namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    class UserManager : ImperiumComponent
    {
      Dictionary<ulong, User> Users = new Dictionary<ulong, User>();

      public UserManager(Imperium core)
        : base(core)
      {
      }

      public User[] GetAll()
      {
        return Users.Values.ToArray();
      }

      public User Get(BasePlayer player)
      {
        if (player == null) return null;
        return Get(player.userID);
      }

      public User Get(ulong userId)
      {
        User user;
        if (Users.TryGetValue(userId, out user))
          return user;
        else
          return null;
      }

      public User Add(BasePlayer player)
      {
        Remove(player);

        User user = player.gameObject.AddComponent<User>();
        user.Init(Core, player);

        Users[user.Player.userID] = user;

        return user;
      }

      public bool Remove(BasePlayer player)
      {
        User user = Get(player);
        if (user == null) return false;

        UnityEngine.Object.DestroyImmediate(user);
        Users.Remove(player.userID);

        return true;
      }

      public void Init()
      {
        List<BasePlayer> players = BasePlayer.activePlayerList;

        Puts($"Creating user objects for {players.Count} players...");

        foreach (BasePlayer player in players)
          Add(player);

        Puts($"Created {Users.Count} user objects.");
      }

      public void Destroy()
      {
        var userObjects = UnityEngine.Object.FindObjectsOfType<User>();
        Puts($"Destroying {userObjects.Length} user objects.");

        if (userObjects != null)
        {
          foreach (var user in userObjects)
            UnityEngine.Object.DestroyImmediate(user);
        }

        Users.Clear();
        Puts("User objects destroyed.");
      }
    }
  }
}
