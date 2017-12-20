namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    public abstract class RustFactionsComponent
    {
      public RustFactions Plugin { get; private set; }

      public RustFactionsComponent(RustFactions plugin)
      {
        Plugin = plugin;
      }
    }
  }
}
