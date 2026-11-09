using UnityEngine;
using KrakenCache.Items;

namespace KrakenCache.Model
{
    /// <summary>
    /// Represents a single discrete cell on the grid board.
    /// Pure data model decoupling logic from GameObject hierarchy.
    /// </summary>
    public class Cell
    {
        public int Col { get; }
        public int Row { get; }

        public Item CurrentItem { get; private set; }

        public bool IsEmpty => CurrentItem == null;

        public Cell(int col, int row)
        {
            Col = col;
            Row = row;
        }

        public void SetItem(Item item)
        {
            CurrentItem = item;
            if (item != null)
            {
                item.CurrentCell = this;
            }
        }

        public Item ClearItem()
        {
            Item item = CurrentItem;
            if (item != null)
            {
                item.CurrentCell = null;
            }
            CurrentItem = null;
            return item;
        }

        public override string ToString() => $"Cell({Col}, {Row}) [Item: {CurrentItem?.GetType().Name ?? "Empty"}]";
    }
}
