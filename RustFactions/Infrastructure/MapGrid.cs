namespace Oxide.Plugins
{
  using System;

  public partial class RustFactions
  {
    public class MapGrid
    {
      public int MapSize { get; private set; }
      public int Width { get; private set; }

      string[] RowIds;
      string[] ColumnIds;
      string[,] AreaIds;

      public MapGrid(int mapSize)
      {
        MapSize = mapSize;
        Width = (int)Math.Ceiling(mapSize / 150f);
        RowIds = new string[Width];
        ColumnIds = new string[Width];
        AreaIds = new string[Width, Width];
        Build();
      }

      public string GetRowId(int row)
      {
        return RowIds[row];
      }

      public string GetColumnId(int col)
      {
        return ColumnIds[col];
      }

      public string GetAreaId(int row, int col)
      {
        return AreaIds[row, col];
      }

      void Build()
      {
        string prefix = "";
        char letter = 'A';

        for (int row = 0; row < Width; row++)
        {
          RowIds[row] = prefix + letter;
          if (letter == 'Z')
          {
            prefix = "A";
            letter = 'A';
          }
          else
          {
            letter++;
          }
        }

        for (int col = 0; col < Width; col++)
          ColumnIds[col] = col.ToString();

        for (int row = 0; row < Width; row++)
        {
          for (int col = 0; col < Width; col++)
            AreaIds[row, col] = RowIds[row] + ColumnIds[col];
        }
      }
    }
  }
}
