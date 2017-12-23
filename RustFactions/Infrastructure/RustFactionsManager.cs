namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    public abstract class RustFactionsManager
    {
      public RustFactions Plugin { get; private set; }

      public RustFactionsManager(RustFactions plugin)
      {
        Plugin = plugin;
      }

      protected void Puts(string format, params object[] args)
      {
        Plugin.Puts(format, args);
      }
    }
  }
}
