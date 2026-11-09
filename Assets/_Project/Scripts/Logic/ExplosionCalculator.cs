using System.Collections.Generic;
using KrakenCache.Core;
using KrakenCache.Items;
using KrakenCache.Model;

namespace KrakenCache.Logic
{
    public enum ComboType
    {
        None,
        RocketRocket,
        TntTnt,
        TntRocket
    }

    /// <summary>
    /// Calculates affected cells for single special items and combo explosions.
    /// Pure calculation decoupled from visual rendering.
    /// </summary>
    public static class ExplosionCalculator
    {
        public static HashSet<Cell> CalculateSingleSpecialExplosion(GridModel grid, Cell originCell, SpecialItem item)
        {
            var affectedCells = new HashSet<Cell>();
            if (originCell == null || item == null) return affectedCells;

            if (item is RocketItem rocket)
            {
                if (rocket.IsVertical)
                {
                    // Vertical Rocket: Damages entire column
                    for (int y = 0; y < grid.Height; y++)
                    {
                        affectedCells.Add(grid.GetCell(originCell.Col, y));
                    }
                }
                else
                {
                    // Horizontal Rocket: Damages entire row
                    for (int x = 0; x < grid.Width; x++)
                    {
                        affectedCells.Add(grid.GetCell(x, originCell.Row));
                    }
                }
            }
            else if (item is TntItem)
            {
                // TNT: 5x5 area centered at cell
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        int nx = originCell.Col + dx;
                        int ny = originCell.Row + dy;
                        if (grid.IsValid(nx, ny))
                        {
                            affectedCells.Add(grid.GetCell(nx, ny));
                        }
                    }
                }
            }

            return affectedCells;
        }

        public static ComboType DetermineComboType(List<Cell> comboCells)
        {
            if (comboCells == null || comboCells.Count < 2) return ComboType.None;

            int rocketCount = 0;
            int tntCount = 0;

            foreach (var cell in comboCells)
            {
                if (cell.CurrentItem is RocketItem) rocketCount++;
                else if (cell.CurrentItem is TntItem) tntCount++;
            }

            if (tntCount == 0 && rocketCount >= 2)
            {
                return ComboType.RocketRocket;
            }
            if (tntCount >= 2)
            {
                return ComboType.TntTnt;
            }
            if (tntCount >= 1 && rocketCount >= 1)
            {
                return ComboType.TntRocket;
            }

            return ComboType.None;
        }

        public static HashSet<Cell> CalculateComboExplosion(GridModel grid, Cell tappedCell, ComboType comboType)
        {
            var affectedCells = new HashSet<Cell>();
            if (tappedCell == null) return affectedCells;

            switch (comboType)
            {
                case ComboType.RocketRocket:
                    // + shape (entire row and entire column)
                    for (int x = 0; x < grid.Width; x++)
                    {
                        affectedCells.Add(grid.GetCell(x, tappedCell.Row));
                    }
                    for (int y = 0; y < grid.Height; y++)
                    {
                        affectedCells.Add(grid.GetCell(tappedCell.Col, y));
                    }
                    break;

                case ComboType.TntTnt:
                    // 7x7 area centered at cell
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        for (int dy = -3; dy <= 3; dy++)
                        {
                            int nx = tappedCell.Col + dx;
                            int ny = tappedCell.Row + dy;
                            if (grid.IsValid(nx, ny))
                            {
                                affectedCells.Add(grid.GetCell(nx, ny));
                            }
                        }
                    }
                    break;

                case ComboType.TntRocket:
                    // 3x3 array of rockets firing in a + shape:
                    // Affects 3 columns (col-1, col, col+1) and 3 rows (row-1, row, row+1)
                    for (int offset = -1; offset <= 1; offset++)
                    {
                        int targetCol = tappedCell.Col + offset;
                        if (targetCol >= 0 && targetCol < grid.Width)
                        {
                            for (int y = 0; y < grid.Height; y++)
                            {
                                affectedCells.Add(grid.GetCell(targetCol, y));
                            }
                        }

                        int targetRow = tappedCell.Row + offset;
                        if (targetRow >= 0 && targetRow < grid.Height)
                        {
                            for (int x = 0; x < grid.Width; x++)
                            {
                                affectedCells.Add(grid.GetCell(x, targetRow));
                            }
                        }
                    }
                    break;
            }

            return affectedCells;
        }
    }
}
