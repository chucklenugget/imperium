namespace Oxide.Plugins
{
  using System.Collections;
  using System.Drawing;
  using System.Drawing.Drawing2D;

  public partial class Imperium
  {
    class MapOverlayGenerator : UnityEngine.MonoBehaviour
    {
      public bool IsGenerating { get; private set; }

      public void Generate()
      {
        if (!IsGenerating)
          StartCoroutine(GenerateOverlayImage());
      }

      IEnumerator GenerateOverlayImage()
      {
        IsGenerating = true;
        Instance.Puts("Generating new map overlay image...");

        using (var bitmap = new Bitmap(Instance.Options.Map.ImageSize, Instance.Options.Map.ImageSize))
        using (var graphics = Graphics.FromImage(bitmap))
        {
          var grid = Instance.Areas.MapGrid;
          var tileSize = (int)(Instance.Options.Map.ImageSize / grid.CellSizeRatio);

          var colorPicker = new FactionColorPicker();
          var textBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

          for (int row = 0; row < grid.NumberOfCells; row++)
          {
            for (int col = 0; col < grid.NumberOfCells; col++)
            {
              Area area = Instance.Areas.Get(row, col);
              var x = (col * tileSize);
              var y = (row * tileSize);
              var rect = new Rectangle(x, y, tileSize, tileSize);

              if (area.Type == AreaType.Badlands)
              {
                // If the tile is badlands, color it in black.
                var brush = new HatchBrush(HatchStyle.BackwardDiagonal, Color.FromArgb(32, 0, 0, 0), Color.FromArgb(255, 0, 0, 0));
                graphics.FillRectangle(brush, rect);
              }
              else if (area.Type != AreaType.Wilderness)
              {
                // If the tile is claimed, fill it with a color indicating the faction.
                var brush = new SolidBrush(colorPicker.GetColorForFaction(area.FactionId));
                graphics.FillRectangle(brush, rect);
              }

              yield return null;
            }
          }

          var gridLabelFont = new Font("Consolas", 14, FontStyle.Bold);
          var gridLabelOffset = 5;
          var gridLinePen = new Pen(Color.FromArgb(192, 0, 0, 0), 2);

          for (int row = 0; row < grid.NumberOfCells; row++)
          {
            if (row > 0) graphics.DrawLine(gridLinePen, 0, (row * tileSize), (grid.NumberOfCells * tileSize), (row * tileSize));
            graphics.DrawString(grid.GetRowId(row), gridLabelFont, textBrush, gridLabelOffset, (row * tileSize) + gridLabelOffset);
          }

          for (int col = 1; col < grid.NumberOfCells; col++)
          {
            graphics.DrawLine(gridLinePen, (col * tileSize), 0, (col * tileSize), (grid.NumberOfCells * tileSize));
            graphics.DrawString(grid.GetColumnId(col), gridLabelFont, textBrush, (col * tileSize) + gridLabelOffset, gridLabelOffset);
          }

          var converter = new ImageConverter();
          var imageData = (byte[])converter.ConvertTo(bitmap, typeof(byte[]));

          Image image = Instance.Hud.RegisterImage(Ui.MapOverlayImageUrl, imageData, true);

          Instance.Puts($"Generated new map overlay image {image.Id}.");
          Instance.Log($"Created new map overlay image {image.Id}.");

          IsGenerating = false;
        }
      }
    }
  }
}
