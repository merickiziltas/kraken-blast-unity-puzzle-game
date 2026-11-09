namespace KrakenCache.Core
{
    public enum CubeColor
    {
        None = 0,
        Red,
        Green,
        Blue,
        Yellow
    }

    public enum SpecialType
    {
        None = 0,
        HorizontalRocket,
        VerticalRocket,
        TNT
    }

    public enum ObstacleType
    {
        None = 0,
        Vase,
        Stone,
        ChaliceBox
    }

    public enum HintType
    {
        None = 0,
        Rocket,
        TNT
    }

    public enum GameState
    {
        Initializing,
        Idle,           // Waiting for user input
        Processing,     // Resolving matches, explosions, falling
        LevelWon,
        LevelLost
    }

    public enum DamageSource
    {
        AdjacentBlast,  // From neighboring cube blast
        SpecialItem     // From Rocket or TNT explosion
    }
}
