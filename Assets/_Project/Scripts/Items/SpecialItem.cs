using KrakenCache.Core;

namespace KrakenCache.Items
{
    public abstract class SpecialItem : Item
    {
        public abstract SpecialType Type { get; }
        public override bool CanFall => true;
        public override bool IsSpecial => true;
    }

    public class RocketItem : SpecialItem
    {
        public bool IsVertical { get; }

        public override SpecialType Type => IsVertical ? SpecialType.VerticalRocket : SpecialType.HorizontalRocket;

        public RocketItem(bool isVertical)
        {
            IsVertical = isVertical;
        }
    }

    public class TntItem : SpecialItem
    {
        public override SpecialType Type => SpecialType.TNT;
    }
}
