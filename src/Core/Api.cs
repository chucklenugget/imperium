namespace Oxide.Plugins
{
  using Oxide.Core.Plugins;

  public partial class Imperium
  {
    [HookMethod(nameof(GetFactionName))]
    public object GetFactionName(BasePlayer player)
    {
      User user = Users.Get(player);

      if (user == null || user.Faction == null)
        return null;
      else
        return user.Faction.Id;
    }
  }
}
