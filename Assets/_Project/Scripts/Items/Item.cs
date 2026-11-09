using System;
using UnityEngine;
using KrakenCache.Core;
using KrakenCache.Model;
using DG.Tweening;

namespace KrakenCache.Items
{
    /// <summary>
    /// Abstract base class for all board items following OOP polymorphism.
    /// Encapsulates cell binding, fall capability, and damage resolution.
    /// </summary>
    public abstract class Item
    {
        public Cell CurrentCell { get; set; }
        public GameObject ViewObject { get; set; }

        public abstract bool CanFall { get; }
        public virtual bool IsObstacle => false;
        public virtual bool IsSpecial => false;

        public event Action<Item> OnItemDestroyed;
        private bool destroyed;

        public virtual bool CanMatchWith(Item other) => false;

        public virtual bool TryTakeDamage(DamageSource source, int damageAmount, out int actualDamageDealt)
        {
            actualDamageDealt = 0;
            return false;
        }

        public virtual void DestroyItem()
        {
            if (destroyed) return;
            destroyed = true;
            OnItemDestroyed?.Invoke(this);
            if (CurrentCell != null && CurrentCell.CurrentItem == this)
            {
                CurrentCell.ClearItem();
            }

            if (ViewObject != null)
            {
                var view = ViewObject.GetComponent<KrakenCache.Controllers.ItemView>();
                if (view != null) view.AnimateBlast(null);
                else
                {
                    ViewObject.transform.DOKill();
                    UnityEngine.Object.Destroy(ViewObject);
                }
                ViewObject = null;
            }
        }
    }
}
