using System;
using KrakenCache.Core;

namespace KrakenCache.Items
{
    public class CubeItem : Item
    {
        public CubeColor Color { get; }
        public HintType CurrentHint { get; private set; } = HintType.None;

        public override bool CanFall => true;

        public event Action<HintType> OnHintChanged;

        public CubeItem(CubeColor color)
        {
            Color = color;
        }

        public override bool CanMatchWith(Item other)
        {
            return other is CubeItem otherCube && otherCube.Color == this.Color;
        }

        public void SetHint(HintType hint)
        {
            if (CurrentHint != hint)
            {
                CurrentHint = hint;
                OnHintChanged?.Invoke(CurrentHint);
            }
        }
    }
}
