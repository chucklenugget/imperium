using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Oxide.Plugins;
using System.Linq;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace GenerateMapImage
{
  class Program
  {
    public static void Main()
    {
      var json = File.ReadAllText(@"C:\Users\nkohari\Work\RustFactions\RustFactions.json");
      var data = JsonConvert.DeserializeObject<RustFactions.PersistentData>(json);
      var claims = data.Claims.ToDictionary(claim => claim.AreaId);
      DrawMapOverlay(4000, 1440, data);
    }

    class MapHeadquartersLocation
    {
      public string FactionId;
      public int X;
      public int Y;

      public MapHeadquartersLocation(string factionId, int x, int y)
      {
        FactionId = factionId;
        X = x;
        Y = y;
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

      Dictionary<string, Color> AssignedColors = new Dictionary<string, Color>();
      int NextColor = 0;

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

    public static void DrawMapOverlay(int mapSize, int imageSize, RustFactions.PersistentData data)
    {
      var claims = data.Claims.ToDictionary(claim => claim.AreaId);
      var badlandsAreas = new HashSet<string>(data.BadlandsAreas);
      //var overlayImage = Image.FromFile(@"C:\Users\nkohari\Work\RustFactions\RustFactionsMap.jpg");
      var overlayImage = new Bitmap(imageSize, imageSize);
      var graphics = Graphics.FromImage(overlayImage);
      var grid = new RustFactions.MapGrid(mapSize);
      var colorMap = new FactionColorPicker();
      var tileSize = (int)(imageSize / (mapSize / 150f));
      var headquarters = new List<MapHeadquartersLocation>();

      overlayImage.MakeTransparent();

      var textBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

      for (int row = 0; row < grid.Width; row++)
      {
        for (int col = 0; col < grid.Width; col++)
        {
          var x = (col * tileSize);
          var y = (row * tileSize);
          var areaId = grid.GetAreaId(row, col);
          var rect = new Rectangle(x, y, tileSize, tileSize);

          if (badlandsAreas.Contains(areaId))
          {
            // If the tile is badlands, color it in black.
            var brush = new SolidBrush(Color.FromArgb(192, 0, 0, 0));
            graphics.FillRectangle(brush, rect);
          }
          else
          {
            // If the tile is claimed, fill it with a color indicating the faction.
            RustFactions.Claim claim;
            if (claims.TryGetValue(areaId, out claim))
            {
              var brush = new SolidBrush(colorMap.GetColorForFaction(claim.FactionId));
              graphics.FillRectangle(brush, rect);

              if (claim.IsHeadquarters)
                headquarters.Add(new MapHeadquartersLocation(claim.FactionId, x, y));
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
        var textBoundary = new Rectangle(hq.X - tileSize, hq.Y - tileSize, tileSize * 3, tileSize * 3);
        var centerTextFormat = new StringFormat
        {
          Alignment = StringAlignment.Center,
          LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(hq.FactionId, hqFont, textBrush, textBoundary, centerTextFormat);
      }

      overlayImage.Save(@"C:\Users\nkohari\Work\RustFactions\RustFactionsOverlay.png", ImageFormat.Png);
    }

  }
}