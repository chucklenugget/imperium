namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    abstract class Interaction
    {
      public RustFactions Core { get; set; }
      public User User { get; set; }

      public abstract bool TryComplete(HitInfo hit);
    }
  }
}
