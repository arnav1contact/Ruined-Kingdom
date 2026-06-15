using System;
using UnityEngine;

[Serializable]
public class CharacterProfile
{
    [SerializeField] string characterName = "New Hero";
    [SerializeField] CharacterGender gender = CharacterGender.Androgynous;
    [SerializeField] CharacterElement startingElement = CharacterElement.Life;
    [SerializeField] Color primaryColor = new Color(0.52f, 0.18f, 0.85f);
    [SerializeField] Color secondaryMetalColor = new Color(0.72f, 0.68f, 0.58f);
    [SerializeField] CharacterStatType boon = CharacterStatType.Strength;
    [SerializeField] CharacterStatType bane = CharacterStatType.Resistance;

    public string CharacterName => characterName;
    public CharacterGender Gender => gender;
    public CharacterElement StartingElement => startingElement;
    public CharacterMasteryElement MasteryElement => CharacterElementRules.GetMasteryElement(startingElement);
    public Color PrimaryColor => primaryColor;
    public Color SecondaryMetalColor => secondaryMetalColor;
    public CharacterStatType Boon => boon;
    public CharacterStatType Bane => bane;

    public CharacterProfile(
        string characterName,
        CharacterGender gender,
        CharacterElement startingElement,
        Color primaryColor,
        Color secondaryMetalColor,
        CharacterStatType boon,
        CharacterStatType bane)
    {
        this.characterName = string.IsNullOrWhiteSpace(characterName) ? "New Hero" : characterName.Trim();
        this.gender = gender;
        this.startingElement = startingElement;
        this.primaryColor = primaryColor;
        this.secondaryMetalColor = secondaryMetalColor;
        this.boon = boon;
        this.bane = bane;
    }
}
