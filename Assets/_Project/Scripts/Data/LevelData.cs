using System;
using UnityEngine;

namespace KrakenCache.Data
{
    [Serializable]
    public class LevelData
    {
        public int level_number;
        public int grid_width;
        public int grid_height;
        public int move_count;
        public string[] grid;

        /// <summary>
        /// Grid array starts bottom-left (row 0, col 0) and continues horizontally to top-right.
        /// </summary>
        public string GetToken(int col, int row)
        {
            if (col < 0 || col >= grid_width || row < 0 || row >= grid_height)
                return string.Empty;

            int index = row * grid_width + col;
            if (grid != null && index >= 0 && index < grid.Length)
            {
                return grid[index];
            }
            return string.Empty;
        }
    }
}
