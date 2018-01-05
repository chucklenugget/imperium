namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    public abstract class RustFactionsManager
    {
      protected RustFactions Core { get; private set; }

      public RustFactionsManager(RustFactions core)
      {
        Core = core;
      }

      protected void Puts(string format, params object[] args)
      {
        Core.Puts(format, args);
      }
    }
  }
}
