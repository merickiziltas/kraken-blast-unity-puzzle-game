using System.Collections.Generic;
using KrakenCache.Items;

namespace KrakenCache.Model
{
    /// <summary>
    /// Pure 2D grid matrix managing Cells and spatial queries.
    /// Decoupled from Unity Scene rendering.
    /// </summary>
    public class GridModel
    {
        public int Width { get; }
        public int Height { get; }

        private readonly Cell[,] cells;

        public GridModel(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new Cell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cells[x, y] = new Cell(x, y);
                }
            }
        }

        public bool IsValid(int col, int row)
        {
            return col >= 0 && col < Width && row >= 0 && row < Height;
        }

        public Cell GetCell(int col, int row)
        {
            if (!IsValid(col, row)) return null;
            return cells[col, row];
        }

        public List<Cell> GetOrthogonalNeighbors(int col, int row)
        {
            var neighbors = new List<Cell>(4);

            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = col + dx[i];
                int ny = row + dy[i];

                if (IsValid(nx, ny))
                {
                    neighbors.Add(cells[nx, ny]);
                }
            }

            return neighbors;
        }
    }
}
