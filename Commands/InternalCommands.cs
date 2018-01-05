namespace Oxide.Plugins
{
  public partial class RustFactions
  {

    [ConsoleCommand("imperium.images.refresh")]
    void OnRefreshImagesConsoleCommand(ConsoleSystem.Arg arg)
    {
      if (!arg.IsAdmin) return;
      arg.ReplyWith("Refreshing images...");
      Ui.RefreshAllImages();
    }

  }
}