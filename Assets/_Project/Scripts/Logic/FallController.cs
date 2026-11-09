using System.Collections.Generic;
using UnityEngine;
using KrakenCache.Core;
using KrakenCache.Items;
using KrakenCache.Model;

namespace KrakenCache.Logic
{
    public class MoveAction
    {
        public Item Item { get; set; }
        public Cell FromCell { get; set; }
        public Cell ToCell { get; set; }
        public int FallDistance => FromCell != null ? (FromCell.Row - ToCell.Row) : 0;
        public bool IsNewSpawn => FromCell == null;
    }

    /// <summary>
    /// Pure algorithmic vertical gravity controller.
    /// Strictly complies with the constraint: "Fall mechanics and animations should be
    /// implemented by coding, not Physics or Unity animations."
    /// Non-falling obstacles (Stone, ChaliceBox) properly block falling paths.
    /// </summary>
    public static class FallController
    {
        public static List<MoveAction> CalculateFalls(GridModel grid)
        {
            var moves = new List<MoveAction>();

            for (int col = 0; col < grid.Width; col++)
            {
                CalculateColumnFalls(grid, col, moves);
            }

            return moves;
        }

        private static void CalculateColumnFalls(GridModel grid, int col, List<MoveAction> moves)
        {
            // Iterate from bottom row to top row
            for (int row = 0; row < grid.Height; row++)
            {
                Cell targetCell = grid.GetCell(col, row);

                // If this cell is empty, find the first fallable item above it that isn't blocked
                if (targetCell.IsEmpty)
                {
                    int foundRow = -1;
                    for (int checkRow = row + 1; checkRow < grid.Height; checkRow++)
                    {
                        Cell checkCell = grid.GetCell(col, checkRow);
                        if (!checkCell.IsEmpty)
                        {
                            if (checkCell.CurrentItem.CanFall)
                            {
                                foundRow = checkRow;
                                break;
                            }
                            else
                            {
                                // Non-falling obstacle blocks anything above it from falling into targetCell
                                break;
                            }
                        }
                    }

                    if (foundRow != -1)
                    {
                        Cell sourceCell = grid.GetCell(col, foundRow);
                        Item item = sourceCell.ClearItem();
                        targetCell.SetItem(item);

                        moves.Add(new MoveAction
                        {
                            Item = item,
                            FromCell = sourceCell,
                            ToCell = targetCell
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Identifies cells that are still empty and have an unblocked vertical path to the top of the grid.
        /// Returns the cells ordered by column and row from bottom to top.
        /// </summary>
        public static List<Cell> GetUnblockedEmptyCellsForSpawn(GridModel grid)
        {
            var spawnCells = new List<Cell>();

            for (int col = 0; col < grid.Width; col++)
            {
                // Find all empty cells in this column that are NOT beneath a non-falling obstacle
                for (int row = 0; row < grid.Height; row++)
                {
                    Cell cell = grid.GetCell(col, row);
                    if (cell.IsEmpty)
                    {
                        // Check if path to the top (all rows above this) is clear of non-falling obstacles
                        bool blockedFromTop = false;
                        for (int checkRow = row + 1; checkRow < grid.Height; checkRow++)
                        {
                            Cell checkCell = grid.GetCell(col, checkRow);
                            if (!checkCell.IsEmpty && !checkCell.CurrentItem.CanFall)
                            {
                                blockedFromTop = true;
                                break;
                            }
                        }

                        if (!blockedFromTop)
                        {
                            spawnCells.Add(cell);
                        }
                    }
                }
            }

            return spawnCells;
        }

        public static CubeColor GetRandomCubeColor()
        {
            CubeColor[] colors = { CubeColor.Red, CubeColor.Green, CubeColor.Blue, CubeColor.Yellow };
            return colors[Random.Range(0, colors.Length)];
        }
    }
}
