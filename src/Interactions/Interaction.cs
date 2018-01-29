namespace Oxide.Plugins
{
  public partial class Imperium
  {
    abstract class Interaction
    {
      public User User { get; set; }
      public abstract bool TryComplete(HitInfo hit);
    }
  }
}
