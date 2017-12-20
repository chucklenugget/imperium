namespace Oxide.Plugins
{
  using Oxide.Game.Rust.Cui;
  using System.Collections.Generic;
  using System.Drawing;
  using UnityEngine;
  using Color = System.Drawing.Color;
  using Graphics = System.Drawing.Graphics;
  using Font = System.Drawing.Font;
  using FontStyle = System.Drawing.FontStyle;

  public partial class RustFactions
  {
    CuiElementContainer MapUi;
    uint CurrentMapOverlayImageId;

    const string UI_CLAIM_PANEL = "RustFactionsClaimPanel";
    const string UI_CLAIM_PANEL_TEXT = "RustFactionsClaimPanelText";
    const string UI_CLAIM_PANEL_BGCOLOR_NORMAL = "1 0.95 0.875 0.025";
    const string UI_CLAIM_PANEL_BGCOLOR_RED = "0.77 0.25 0.17 1";
    const string UI_MAP_PANEL = "RustFactionsMapPanel";
    const string UI_MAP_CLOSE_BUTTON = "RustFactionsMapCloseButton";
    const string UI_MAP_BACKGROUND_IMAGE = "RustFactionsMapImage";
    const string UI_MAP_OVERLAY_IMAGE = "RustFactionsMapOverlay";
    const string UI_TRANSPARENT_TEXTURE = "assets/content/textures/generic/fulltransparent.tga";

    [ChatCommand("map")]
    void OnMapCommand(BasePlayer player, string command, string[] args)
    {
      ToggleMap(player);
    }

    [ConsoleCommand("rustfactions.map.toggle")]
    void OnMapConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;
      ToggleMap(player);
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
        TaxPolicy policy = TaxPolicies.Get(claim);
        if (policy != null)
          text = $"{area.Id}: {faction.Id} ({policy.TaxRate}% tax)";
        else
          text = $"{area.Id}: {faction.Id}";
      }

      var label = new CuiLabel
      {
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

      var panel = new CuiPanel
      {
        Image = { Color = backgroundColor },
        RectTransform = {
          AnchorMin = "0 0",
          AnchorMax = "0.124 0.036",
          OffsetMin = "16 44",
          OffsetMax = "16 44"
        }
      };

      var container = new CuiElementContainer();
      container.Add(panel, "Hud", UI_CLAIM_PANEL);
      container.Add(label, UI_CLAIM_PANEL, UI_CLAIM_PANEL_TEXT);

      CuiHelper.AddUi(player, container);
    }

    void UpdateInfoPanelForAllPlayers()
    {
      foreach (var player in BasePlayer.activePlayerList)
      {
        Area area;
        if (!PlayersInAreas.TryGetValue(player.userID, out area))
        {
          Puts($"Couldn't update info panel for player because they were outside of a known area. This shouldn't happen.");
          continue;
        }
        Claim claim = Claims.Get(area);
        UpdateInfoPanel(player, area, claim);
      }
    }

    void RemoveInfoPanel(BasePlayer player)
    {
      CuiHelper.DestroyUi(player, UI_CLAIM_PANEL);
    }

    void RemoveInfoPanelForAllPlayers()
    {
      foreach (var player in BasePlayer.activePlayerList)
        RemoveInfoPanel(player);
    }

    void ShowMapPanel(BasePlayer player)
    {
      CuiHelper.AddUi(player, MapUi);
    }

    void RemoveMapPanel(BasePlayer player)
    {
      CuiHelper.DestroyUi(player, UI_MAP_PANEL);
    }

    void RemoveMapPanelForAllPlayers()
    {
      foreach (var player in BasePlayer.activePlayerList)
        RemoveInfoPanel(player);
    }

    void ToggleMap(BasePlayer player)
    {
      PlayerMapState mapState = PlayerMapStates.Get(player);

      if (mapState == PlayerMapState.Hidden)
      {
        ShowMapPanel(player);
        PlayerMapStates.Set(player, PlayerMapState.Visible);
      }
      else
      {
        RemoveMapPanel(player);
        PlayerMapStates.Set(player, PlayerMapState.Hidden);
      }
    }

    void RebuildMapUi()
    {
      var panel = new CuiPanel
      {
        Image = { Color = "0 0 0 1" },
        RectTransform = { AnchorMin = "0.2271875 0.015", AnchorMax = "0.7728125 0.985" },
        CursorEnabled = true
      };

      var container = new CuiElementContainer();
      container.Add(panel, "Hud", UI_MAP_PANEL);

      container.Add(new CuiElement
      {
        Name = UI_MAP_BACKGROUND_IMAGE,
        Parent = UI_MAP_PANEL,
        Components = {
          new CuiRawImageComponent { Url = Options.MapImageUrl, Sprite = UI_TRANSPARENT_TEXTURE },
          new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
        }
      });

      container.Add(new CuiElement
      {
        Name = UI_MAP_OVERLAY_IMAGE,
        Parent = UI_MAP_PANEL,
        Components = {
          new CuiRawImageComponent { Png = CurrentMapOverlayImageId.ToString(), Sprite = UI_TRANSPARENT_TEXTURE },
          new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
        }
      });

      container.Add(new CuiButton
      {
        Button = { Color = "0 0 0 1", Command = "rustfactions.map.toggle", FadeIn = 0 },
        RectTransform = { AnchorMin = "0.95 0.961", AnchorMax = "0.999 0.999" },
        Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter }
      }, UI_MAP_PANEL, UI_MAP_CLOSE_BUTTON);

      MapUi = container;
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
        var headquarters = new List<HeadquartersLocation>();
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
              var brush = new SolidBrush(Color.FromArgb(192, 0, 0, 0));
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

                if (claim.IsHeadquarters)
                  headquarters.Add(new HeadquartersLocation(claim.FactionId, row, col));
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

        var hqFont = new Font("Arial", 18, FontStyle.Bold);
        foreach (var hq in headquarters)
        {
          var x = (hq.Col == 0 ? 0 : hq.Col - 1) * tileSize;
          var y = (hq.Row == 0 ? 0 : hq.Row - 1) * tileSize;
          var textBoundary = new Rectangle(x, y, tileSize * 3, tileSize * 3);
          var centerTextFormat = new StringFormat
          {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
          };
          graphics.DrawString(hq.FactionId, hqFont, textBrush, textBoundary, centerTextFormat);
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
