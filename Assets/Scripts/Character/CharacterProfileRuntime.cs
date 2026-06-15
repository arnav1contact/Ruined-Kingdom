public static class CharacterProfileRuntime
{
    public static CharacterProfile CurrentProfile { get; private set; }
    public static bool HasProfile => CurrentProfile != null;

    public static void SetProfile(CharacterProfile profile)
    {
        CurrentProfile = profile;
    }

    public static void Clear()
    {
        CurrentProfile = null;
    }
}
