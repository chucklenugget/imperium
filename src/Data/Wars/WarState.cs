namespace Oxide.Plugins
{
  public partial class Imperium : RustPlugin
  {
    enum WarState
    {
      Declared,
      Started,
      AttackerOfferingPeace,
      DefenderOfferingPeace,
      Ended
    }
  }
}
