using System.Collections.Generic;
using KrakenCache.Items;
using KrakenCache.Model;

namespace KrakenCache.Logic
{
    /// <summary>
    /// BFS-based flood fill matcher for cubes and special items.
    /// Clean, deterministic, and allocation-efficient.
    /// </summary>
    public static class MatchFinder
    {
        public static List<Cell> FindMatchingCubeGroup(GridModel grid, Cell startCell)
        {
            var result = new List<Cell>();
            if (startCell == null || !(startCell.CurrentItem is CubeItem startCube))
            {
                return result;
            }

            var visited = new HashSet<Cell>();
            var queue = new Queue<Cell>();

            queue.Enqueue(startCell);
            visited.Add(startCell);

            while (queue.Count > 0)
            {
                Cell current = queue.Dequeue();
                result.Add(current);

                var neighbors = grid.GetOrthogonalNeighbors(current.Col, current.Row);
                foreach (Cell neighbor in neighbors)
                {
                    if (visited.Contains(neighbor)) continue;

                    if (neighbor.CurrentItem is CubeItem neighborCube && neighborCube.Color == startCube.Color)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return result;
        }

        public static List<Cell> FindConnectedSpecialItemGroup(GridModel grid, Cell startCell)
        {
            var result = new List<Cell>();
            if (startCell == null || !(startCell.CurrentItem is SpecialItem))
            {
                return result;
            }

            var visited = new HashSet<Cell>();
            var queue = new Queue<Cell>();

            queue.Enqueue(startCell);
            visited.Add(startCell);

            while (queue.Count > 0)
            {
                Cell current = queue.Dequeue();
                result.Add(current);

                var neighbors = grid.GetOrthogonalNeighbors(current.Col, current.Row);
                foreach (Cell neighbor in neighbors)
                {
                    if (visited.Contains(neighbor)) continue;

                    if (neighbor.CurrentItem is SpecialItem)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return result;
        }

        public static List<List<Cell>> FindAllCubeGroups(GridModel grid)
        {
            var allGroups = new List<List<Cell>>();
            var visited = new HashSet<Cell>();

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    Cell cell = grid.GetCell(x, y);
                    if (visited.Contains(cell) || !(cell.CurrentItem is CubeItem))
                    {
                        continue;
                    }

                    List<Cell> group = FindMatchingCubeGroup(grid, cell);
                    foreach (Cell c in group)
                    {
                        visited.Add(c);
                    }

                    if (group.Count >= 2)
                    {
                        allGroups.Add(group);
                    }
                }
            }

            return allGroups;
        }
    }
}
