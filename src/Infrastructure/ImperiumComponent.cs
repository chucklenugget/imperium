namespace Oxide.Plugins
{
  public partial class Imperium
  {
    public abstract class ImperiumComponent
    {
      protected Imperium Core { get; private set; }

      public ImperiumComponent(Imperium core)
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
