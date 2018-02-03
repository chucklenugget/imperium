namespace Oxide.Plugins
{
  using System;
  using UnityEngine;

  public partial class Imperium
  {
    public class MapGrid
    {
      public const int GridCellSize = 150;

      public int MapSize { get; private set; }
      public int NumberOfCells { get; private set; }

      string[] RowIds;
      string[] ColumnIds;
      string[,] AreaIds;
      Vector3[,] Positions;

      public MapGrid(int mapSize)
      {
        MapSize = mapSize;
        NumberOfCells = (int)Math.Ceiling(mapSize / (float)GridCellSize);
        RowIds = new string[NumberOfCells];
        ColumnIds = new string[NumberOfCells];
        AreaIds = new string[NumberOfCells, NumberOfCells];
        Positions = new Vector3[NumberOfCells, NumberOfCells];
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

      public Vector3 GetPosition(int row, int col)
      {
        return Positions[row, col];
      }

      void Build()
      {
        string prefix = "";
        char letter = 'A';

        for (int row = 0; row < NumberOfCells; row++)
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

        for (int col = 0; col < NumberOfCells; col++)
          ColumnIds[col] = col.ToString();

        int z = (MapSize / 2) - GridCellSize;
        for (int row = 0; row < NumberOfCells; row++)
        {
          int x = -(MapSize / 2);
          for (int col = 0; col < NumberOfCells; col++)
          {
            var areaId = RowIds[row] + ColumnIds[col];
            AreaIds[row, col] = areaId;
            Positions[row, col] = new Vector3(x + (GridCellSize / 2), 0, z + (GridCellSize / 2));
            x += GridCellSize;
          }
          z -= GridCellSize;
        }
      }
    }
  }
}
