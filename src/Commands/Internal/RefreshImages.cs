namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ConsoleCommand("imperium.images.refresh")]
    void OnRefreshImagesConsoleCommand(ConsoleSystem.Arg arg)
    {
      if (!arg.IsAdmin) return;
      arg.ReplyWith("Refreshing images...");
      Hud.RefreshAllImages();
    }
  }
}
