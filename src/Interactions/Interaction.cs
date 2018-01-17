namespace Oxide.Plugins
{
  public partial class Imperium
  {
    abstract class Interaction
    {
      public Imperium Core { get; set; }
      public User User { get; set; }

      public abstract bool TryComplete(HitInfo hit);
    }
  }
}
