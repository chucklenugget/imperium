namespace Oxide.Plugins
{
  using System.Collections.Generic;

  public partial class RustFactions
  {
    class PlayerStateCollection<T>
    {
      Dictionary<ulong, T> states = new Dictionary<ulong, T>();

      public T Get(BasePlayer player)
      {
        T state;
        if (states.TryGetValue(player.userID, out state))
          return state;
        else
          return default(T);
      }

      public void Set(BasePlayer player, T state)
      {
        states[player.userID] = state;
      }

      public void Reset(BasePlayer player)
      {
        states.Remove(player.userID);
      }
    }
  }
}
