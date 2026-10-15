public static class GameState
{
    public static bool IsFrozen { get; private set; }

    public static void Freeze()
    {
        IsFrozen = true;
    }

    public static void Unfreeze()
    {
        IsFrozen = false;
    }
}
