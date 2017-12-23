namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Drawing;
  using Oxide.Game.Rust.Cui;
  using UnityEngine;
  using Color = System.Drawing.Color;
  using Graphics = System.Drawing.Graphics;
  using Font = System.Drawing.Font;
  using FontStyle = System.Drawing.FontStyle;
  using System.Drawing.Drawing2D;

  public partial class RustFactions
  {
    const string UI_CLAIM_PANEL_BGCOLOR_NORMAL = "1 0.95 0.875 0.025";
    const string UI_CLAIM_PANEL_BGCOLOR_RED = "0.77 0.25 0.17 1";
    const string UI_TRANSPARENT_TEXTURE = "assets/content/textures/generic/fulltransparent.tga";
    const string UI_IMAGE_BASE_URL = "http://images.rustfactions.io.s3.amazonaws.com/";

    uint CurrentMapOverlayImageId;

    public static class UiElements
    {
      public const string Hud = "Hud";
      public const string AreaInfoPanel = "RustFactions.AreaInfoPanel";
      public const string AreaInfoPanelText = "RustFactions.AreaInfoPanel.Text";
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

    void UpdateInfoPanel(BasePlayer player, Area area, Claim claim)
    {
      RemoveInfoPanel(player);

      string text;
      string backgroundColor = UI_CLAIM_PANEL_BGCOLOR_NORMAL;

      if (Options.EnableBadlands && Badlands.Contains(area.Id))
      {
        text = $"{area.Id}: Badlands (+{Options.BadlandsGatherBonus}% bonus)";
        backgroundColor = UI_CLAIM_PANEL_BGCOLOR_RED;
      }
      else if (claim == null)
      {
        text = $"{area.Id}: Unclaimed";
      }
      else
      {
        Faction faction = GetFaction(claim.FactionId);
        TaxPolicy policy = Taxes.Get(claim);
        if (policy != null)
          text = $"{area.Id}: {faction.Id} ({policy.TaxRate}% tax)";
        else
          text = $"{area.Id}: {faction.Id}";
      }

      var label = new CuiLabel {
        Text = {
          Align = TextAnchor.MiddleLeft,
          Color = "0.85 0.85 0.85 0.5",
          FontSize = 14,
          Text = text
        },
        RectTransform = {
          AnchorMin = "0 0",
          AnchorMax = "1 1",
          OffsetMin = "6 0",
          OffsetMax = "6 0"
        }
      };

      var panel = new CuiPanel {
        Image = { Color = backgroundColor },
        RectTransform = {
          AnchorMin = "0 0",
          AnchorMax = "0.124 0.036",
          OffsetMin = "16 44",
          OffsetMax = "16 44"
        }
      };

      var container = new CuiElementContainer();
      container.Add(panel, UiElements.Hud, UiElements.AreaInfoPanel);
      container.Add(label, UiElements.AreaInfoPanel, UiElements.AreaInfoPanelText);

      CuiHelper.AddUi(player, container);
    }

    void UpdateInfoPanelForAllPlayers()
    {
      foreach (User user in Users.GetAll())
      {
        if (user.CurrentArea != null)
        {
          Claim claim = Claims.Get(user.CurrentArea);
          UpdateInfoPanel(user.Player, user.CurrentArea, claim);
        }
      }
    }

    void RemoveInfoPanel(BasePlayer player)
    {
      CuiHelper.DestroyUi(player, UiElements.AreaInfoPanel);
    }

    void RemoveInfoPanelForAllPlayers()
    {
      foreach (var player in BasePlayer.activePlayerList)
        RemoveInfoPanel(player);
    }

    void GenerateMapOverlayImage()
    {
      Puts("Generating new map overlay image...");

      using (var image = new Bitmap(Options.MapImageSize, Options.MapImageSize))
      using (var graphics = Graphics.FromImage(image))
      {
        var mapSize = ConVar.Server.worldsize;
        var tileSize = (int)(Options.MapImageSize / (mapSize / 150f));
        var grid = new MapGrid(mapSize);

        var colorPicker = new FactionColorPicker();
        var textBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

        for (int row = 0; row < grid.Width; row++)
        {
          for (int col = 0; col < grid.Width; col++)
          {
            var x = (col * tileSize);
            var y = (row * tileSize);
            var areaId = grid.GetAreaId(row, col);
            var rect = new Rectangle(x, y, tileSize, tileSize);

            if (Badlands.Contains(areaId))
            {
              // If the tile is badlands, color it in black.
              var brush = new HatchBrush(HatchStyle.BackwardDiagonal, Color.FromArgb(32, 0, 0, 0), Color.FromArgb(255, 0, 0, 0));
              graphics.FillRectangle(brush, rect);
            }
            else
            {
              // If the tile is claimed, fill it with a color indicating the faction.
              Claim claim = Claims.Get(areaId);
              if (claim != null)
              {
                var brush = new SolidBrush(colorPicker.GetColorForFaction(claim.FactionId));
                graphics.FillRectangle(brush, rect);
              }
            }
          }
        }

        var gridLabelFont = new Font("Consolas", 14, FontStyle.Bold);
        var gridLabelOffset = 5;
        var gridLinePen = new Pen(Color.FromArgb(192, 0, 0, 0), 2);

        for (int row = 0; row < grid.Width; row++)
        {
          graphics.DrawLine(gridLinePen, 0, (row * tileSize), (grid.Width * tileSize), (row * tileSize));
          graphics.DrawString(grid.GetRowId(row), gridLabelFont, textBrush, gridLabelOffset, (row * tileSize) + gridLabelOffset);
        }

        for (int col = 1; col < grid.Width; col++)
        {
          graphics.DrawLine(gridLinePen, (col * tileSize), 0, (col * tileSize), (grid.Width * tileSize));
          graphics.DrawString(grid.GetColumnId(col), gridLabelFont, textBrush, (col * tileSize) + gridLabelOffset, gridLabelOffset);
        }

        var converter = new ImageConverter();
        var imageData = (byte[])converter.ConvertTo(image, typeof(byte[]));

        uint previousId = CurrentMapOverlayImageId;
        uint newId = FileStorage.server.Store(imageData, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, 0);

        Puts($"Created new map overlay image {newId}.");
        CurrentMapOverlayImageId = newId;

        FileStorage.server.RemoveEntityNum(previousId, 0);
        Puts($"Removed previous map overlay image {previousId}.");
      }
    }

    class HeadquartersLocation
    {
      public string FactionId;
      public int Row;
      public int Col;

      public HeadquartersLocation(string factionId, int row, int col)
      {
        FactionId = factionId;
        Row = row;
        Col = col;
      }
    }

    class FactionColorPicker
    {
      static string[] Colors = new[]
      {
        "#00FF00", "#0000FF", "#FF0000", "#01FFFE", "#FFA6FE",
        "#FFDB66", "#006401", "#010067", "#95003A", "#007DB5",
        "#FF00F6", "#FFEEE8", "#774D00", "#90FB92", "#0076FF",
        "#D5FF00", "#FF937E", "#6A826C", "#FF029D", "#FE8900",
        "#7A4782", "#7E2DD2", "#85A900", "#FF0056", "#A42400",
        "#00AE7E", "#683D3B", "#BDC6FF", "#263400", "#BDD393",
        "#00B917", "#9E008E", "#001544", "#C28C9F", "#FF74A3",
        "#01D0FF", "#004754", "#E56FFE", "#788231", "#0E4CA1",
        "#91D0CB", "#BE9970", "#968AE8", "#BB8800", "#43002C",
        "#DEFF74", "#00FFC6", "#FFE502", "#620E00", "#008F9C",
        "#98FF52", "#7544B1", "#B500FF", "#00FF78", "#FF6E41",
        "#005F39", "#6B6882", "#5FAD4E", "#A75740", "#A5FFD2",
        "#FFB167", "#009BFF", "#E85EBE"
      };

      Dictionary<string, Color> AssignedColors;
      int NextColor = 0;

      public FactionColorPicker()
      {
        AssignedColors = new Dictionary<string, Color>();
      }

      public Color GetColorForFaction(string factionId)
      {
        Color color;

        if (!AssignedColors.TryGetValue(factionId, out color))
        {
          color = Color.FromArgb(96, ColorTranslator.FromHtml(Colors[NextColor]));
          AssignedColors.Add(factionId, color);
          NextColor = (NextColor + 1) % Colors.Length;
        }

        return color;
      }
    }

  }

}
