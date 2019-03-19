namespace Oxide.Plugins
{
  public partial class Imperium : RustPlugin
  {
    enum WarEndReason
    {
      Treaty,
      AttackerEliminatedDefender,
      DefenderEliminatedAttacker,
      CanceledByAdmin
    }
  }
}
