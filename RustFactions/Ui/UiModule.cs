namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    const string UI_TRANSPARENT_TEXTURE = "assets/content/textures/generic/fulltransparent.tga";
    const string UI_IMAGE_BASE_URL = "http://images.rustfactions.io.s3.amazonaws.com/";

    public static class UiElements
    {
      public const string Hud = "Hud";
      public const string LocationPanel = "RustFactions.LocationPanel";
      public const string LocationPanelText = "RustFactions.LocationPanel.Text";
      public const string Map = "RustFactions.Map";
      public const string MapCloseButton = "RustFactions.Map.CloseButton";
      public const string MapBackgroundImage = "RustFactions.Map.BackgroundImage";
      public const string MapClaimsImage = "RustFactions.Map.ClaimsImage";
      public const string MapOverlay = "RustFactions.Map.Overlay";
      public const string MapIcon = "RustFactions.Map.Icon";
      public const string MapLabel = "RustFactions.Map.Label";
    }

    [ChatCommand("map")]
    void OnMapCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);

      if (user == null)
      {
        PrintWarning("Player tried to toggle map but couldn't find their user object. This shouldn't happen.");
        return;
      }

      user.Map.Toggle();
    }

    [ConsoleCommand("rustfactions.map.toggle")]
    void OnMapConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);
      if (user == null)
      {
        PrintWarning("Player tried to toggle map but couldn't find their user object. This shouldn't happen.");
        return;
      }

      user.Map.Toggle();
    }
    
    void RefreshUiForAllPlayers()
    {
      foreach (User user in Users.GetAll())
      {
        user.LocationPanel.Refresh();
        user.Map.Refresh();
      }
    }
  }

}
