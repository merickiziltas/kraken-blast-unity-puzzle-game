using System.Collections.Generic;
using KrakenCache.Core;
using KrakenCache.Items;
using KrakenCache.Model;

namespace KrakenCache.Logic
{
    /// <summary>
    /// Evaluates board state and dynamically applies Rocket or TNT hint logos
    /// to cubes that belong to qualifying groups.
    /// </summary>
    public static class HintManager
    {
        public static void UpdateHints(GridModel grid)
        {
            // Compute final hints first; unchanged groups never toggle through None.
            var hints = new Dictionary<CubeItem, HintType>();
            List<List<Cell>> groups = MatchFinder.FindAllCubeGroups(grid);

            foreach (var group in groups)
            {
                HintType targetHint = HintType.None;

                if (group.Count == 4 || group.Count == 5)
                {
                    targetHint = HintType.Rocket;
                }
                else if (group.Count >= 6)
                {
                    targetHint = HintType.TNT;
                }

                if (targetHint != HintType.None)
                {
                    foreach (Cell cell in group)
                    {
                        if (cell.CurrentItem is CubeItem cube)
                        {
                            hints[cube] = targetHint;
                        }
                    }
                }
            }
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                    if (grid.GetCell(x, y).CurrentItem is CubeItem cube)
                    {
                        hints.TryGetValue(cube, out HintType hint);
                        cube.SetHint(hint);
                    }
        }
    }
}
