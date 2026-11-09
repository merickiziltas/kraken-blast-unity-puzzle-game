using System;
using System.Collections.Generic;
using UnityEngine;
using KrakenCache.Core;
using KrakenCache.Model;

namespace KrakenCache.Items
{
    public abstract class ObstacleItem : Item
    {
        public override bool IsObstacle => true;
        public abstract ObstacleType ObstacleType { get; }
        public int CurrentHealth { get; protected set; }
        public bool IsCleared => CurrentHealth <= 0;

        public event Action<ObstacleItem, int> OnDamageReceived;

        protected void NotifyDamage(int damage)
        {
            OnDamageReceived?.Invoke(this, damage);
        }
    }

    /// <summary>
    /// Vase: 2 HP, falls down to empty cells.
    /// Takes damage from adjacent blast or special items.
    /// Maximum 1 damage from a single blast event.
    /// </summary>
    public class VaseItem : ObstacleItem
    {
        public override ObstacleType ObstacleType => ObstacleType.Vase;
        public override bool CanFall => true;

        public VaseItem()
        {
            CurrentHealth = 2;
        }

        public override bool TryTakeDamage(DamageSource source, int damageAmount, out int actualDamageDealt)
        {
            actualDamageDealt = 0;
            if (IsCleared || damageAmount <= 0) return false;

            if (source == DamageSource.AdjacentBlast)
            {
                // Takes no more than one damage from a single blast
                actualDamageDealt = 1;
            }
            else // SpecialItem
            {
                actualDamageDealt = Mathf.Min(damageAmount, CurrentHealth);
            }

            CurrentHealth -= actualDamageDealt;
            NotifyDamage(actualDamageDealt);

            if (CurrentHealth <= 0)
            {
                DestroyItem();
            }

            return true;
        }
    }

    /// <summary>
    /// Stone: 1 HP, does NOT fall down.
    /// Takes damage ONLY from special item explosions (immune to adjacent blasts).
    /// </summary>
    public class StoneItem : ObstacleItem
    {
        public override ObstacleType ObstacleType => ObstacleType.Stone;
        public override bool CanFall => false;

        public StoneItem()
        {
            CurrentHealth = 1;
        }

        public override bool TryTakeDamage(DamageSource source, int damageAmount, out int actualDamageDealt)
        {
            actualDamageDealt = 0;
            if (IsCleared || damageAmount <= 0) return false;

            // Only damaged by special items!
            if (source == DamageSource.SpecialItem)
            {
                actualDamageDealt = 1;
                CurrentHealth = 0;
                NotifyDamage(actualDamageDealt);
                DestroyItem();
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Chalice Box: 2x2 multi-cell obstacle, does NOT fall.
    /// Phase 1 (Door): 1 damage per source.
    /// Phase 2 (Chalice): Deals damage equal to hit cells / adjacent cubes count. Total goal: 10 chalices.
    /// </summary>
    public class ChaliceBoxItem : ObstacleItem
    {
        public override ObstacleType ObstacleType => ObstacleType.ChaliceBox;
        public override bool CanFall => false;

        public bool IsDoorPhase { get; private set; } = true;
        public int ChalicesCollected { get; private set; } = 0;
        public const int TargetChalices = 10;

        public List<Cell> OccupiedCells { get; } = new List<Cell>(4);

        public event Action OnDoorBroken;
        public event Action<int> OnChaliceCollected;

        public ChaliceBoxItem()
        {
            CurrentHealth = TargetChalices; // Track total chalices as health in phase 2
        }

        public void RegisterCell(Cell cell)
        {
            if (!OccupiedCells.Contains(cell))
            {
                OccupiedCells.Add(cell);
            }
        }

        public override bool TryTakeDamage(DamageSource source, int damageAmount, out int actualDamageDealt)
        {
            actualDamageDealt = 0;
            if (IsCleared || damageAmount <= 0) return false;

            if (IsDoorPhase)
            {
                // Door Phase: Each damage source deals exactly 1 damage
                IsDoorPhase = false;
                actualDamageDealt = 1;
                OnDoorBroken?.Invoke();
                NotifyDamage(1);
                return true;
            }
            else
            {
                // Chalice Phase: Collect chalices according to damageAmount
                int needed = TargetChalices - ChalicesCollected;
                actualDamageDealt = Mathf.Clamp(damageAmount, 1, needed);
                ChalicesCollected += actualDamageDealt;
                CurrentHealth = TargetChalices - ChalicesCollected;

                OnChaliceCollected?.Invoke(actualDamageDealt);
                NotifyDamage(actualDamageDealt);

                if (ChalicesCollected >= TargetChalices)
                {
                    DestroyItem();
                }

                return true;
            }
        }

        public override void DestroyItem()
        {
            foreach (var cell in OccupiedCells)
            {
                if (cell != null && cell.CurrentItem == this)
                {
                    cell.ClearItem();
                }
            }
            base.DestroyItem();
        }
    }
}
