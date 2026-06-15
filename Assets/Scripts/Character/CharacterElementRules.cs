public static class CharacterElementRules
{
    public static CharacterMasteryElement GetMasteryElement(CharacterElement element)
    {
        return element switch
        {
            CharacterElement.Life => CharacterMasteryElement.Poison,
            CharacterElement.Water => CharacterMasteryElement.Ice,
            CharacterElement.Fire => CharacterMasteryElement.Lava,
            CharacterElement.Air => CharacterMasteryElement.Lightning,
            CharacterElement.Earth => CharacterMasteryElement.Metal,
            CharacterElement.Death => CharacterMasteryElement.Blood,
            _ => CharacterMasteryElement.Poison
        };
    }

    public static bool IsSpecialElement(CharacterElement element)
    {
        return element == CharacterElement.Death;
    }
}
