using UnityEngine;

[DisallowMultipleComponent]
public class CharacterCreationController : MonoBehaviour
{
    [SerializeField] bool showOnStart = true;
    [SerializeField] GameObject gameplayRoot = null;
    [SerializeField] PlayerMovementController playerMovement = null;
    [SerializeField] PlayerCombatController playerCombat = null;
    [SerializeField] SpriteRenderer playerBodyRenderer = null;
    [SerializeField] SpriteRenderer playerWeaponRenderer = null;

    string characterName = "Aria";
    int genderIndex = 2;
    int elementIndex;
    int primaryColorIndex;
    int metalColorIndex;
    int boonIndex = 1;
    int baneIndex = 6;
    bool isCreating;

    GUIStyle titleStyle;
    GUIStyle labelStyle;
    GUIStyle warningStyle;

    readonly Color[] primaryColors =
    {
        new Color(0.52f, 0.18f, 0.85f),
        new Color(0.1f, 0.45f, 1f),
        new Color(0.1f, 0.75f, 0.35f),
        new Color(0.95f, 0.25f, 0.2f),
        new Color(1f, 0.8f, 0.2f),
        new Color(0.95f, 0.35f, 0.75f)
    };

    readonly string[] primaryColorNames =
    {
        "Royal Purple",
        "River Blue",
        "Wild Green",
        "Ember Red",
        "Sun Gold",
        "Rose Pink"
    };

    readonly Color[] metalColors =
    {
        new Color(0.72f, 0.68f, 0.58f),
        new Color(0.75f, 0.45f, 0.22f),
        new Color(0.85f, 0.72f, 0.32f),
        new Color(0.48f, 0.55f, 0.62f),
        new Color(0.22f, 0.22f, 0.26f)
    };

    readonly string[] metalColorNames =
    {
        "Iron",
        "Copper",
        "Gold",
        "Steel",
        "Darksteel"
    };

    void Start()
    {
        SetCreationVisible(showOnStart);
    }

    void OnGUI()
    {
        if (!isCreating)
        {
            return;
        }

        InitializeStyles();

        float panelWidth = Mathf.Min(760f, Screen.width - 32f);
        float panelHeight = Mathf.Min(560f, Screen.height - 32f);
        Rect panelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);

        GUI.Box(panelRect, "");

        GUILayout.BeginArea(new Rect(panelRect.x + 24f, panelRect.y + 18f, panelRect.width - 48f, panelRect.height - 36f));
        GUILayout.Label("Create Your Character", titleStyle);
        GUILayout.Space(12f);

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(panelWidth * 0.55f));
        DrawNameRow();
        DrawChoiceRow("Gender", ref genderIndex, System.Enum.GetNames(typeof(CharacterGender)));
        DrawChoiceRow("Element", ref elementIndex, System.Enum.GetNames(typeof(CharacterElement)));
        DrawChoiceRow("Primary", ref primaryColorIndex, primaryColorNames);
        DrawChoiceRow("Metal", ref metalColorIndex, metalColorNames);
        DrawChoiceRow("Boon", ref boonIndex, System.Enum.GetNames(typeof(CharacterStatType)));
        DrawChoiceRow("Bane", ref baneIndex, System.Enum.GetNames(typeof(CharacterStatType)));
        GUILayout.EndVertical();

        GUILayout.Space(24f);

        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        DrawPreview();
        DrawSummary();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Begin", GUILayout.Width(180f), GUILayout.Height(42f)))
        {
            ConfirmCharacter();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    void InitializeStyles()
    {
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        labelStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            normal = { textColor = Color.white }
        };

        warningStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.55f, 0.55f) }
        };
    }

    void DrawNameRow()
    {
        GUILayout.BeginHorizontal(GUILayout.Height(36f));
        GUILayout.Label("Name", labelStyle, GUILayout.Width(95f));
        characterName = GUILayout.TextField(characterName, 18, GUILayout.Width(260f), GUILayout.Height(30f));
        GUILayout.EndHorizontal();
    }

    void DrawChoiceRow(string label, ref int index, string[] options)
    {
        GUILayout.BeginHorizontal(GUILayout.Height(38f));
        GUILayout.Label(label, labelStyle, GUILayout.Width(95f));

        if (GUILayout.Button("<", GUILayout.Width(34f), GUILayout.Height(30f)))
        {
            index = WrapIndex(index - 1, options.Length);
        }

        GUILayout.Label(options[index], labelStyle, GUILayout.Width(170f));

        if (GUILayout.Button(">", GUILayout.Width(34f), GUILayout.Height(30f)))
        {
            index = WrapIndex(index + 1, options.Length);
        }

        GUILayout.EndHorizontal();
    }

    void DrawPreview()
    {
        GUILayout.Label("Preview", titleStyle);
        Rect previewRect = GUILayoutUtility.GetRect(220f, 180f);
        GUI.Box(previewRect, "");

        Rect bodyRect = new Rect(previewRect.center.x - 42f, previewRect.y + 28f, 84f, 124f);
        Rect armorRect = new Rect(previewRect.center.x - 58f, previewRect.y + 78f, 116f, 42f);

        Color oldColor = GUI.color;
        GUI.color = primaryColors[primaryColorIndex];
        GUI.DrawTexture(bodyRect, Texture2D.whiteTexture);
        GUI.color = metalColors[metalColorIndex];
        GUI.DrawTexture(armorRect, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    void DrawSummary()
    {
        CharacterElement element = (CharacterElement)elementIndex;
        CharacterMasteryElement mastery = CharacterElementRules.GetMasteryElement(element);

        GUILayout.Space(8f);
        GUILayout.Label($"Element: {element}", labelStyle);
        GUILayout.Label($"Future mastery: {mastery}", labelStyle);
        GUILayout.Label($"Boon: {(CharacterStatType)boonIndex}", labelStyle);
        GUILayout.Label($"Bane: {(CharacterStatType)baneIndex}", labelStyle);

        if (CharacterElementRules.IsSpecialElement(element))
        {
            GUILayout.Space(8f);
            GUILayout.Label("Death is a special path. Blood mastery is forbidden/later.", warningStyle);
        }
    }

    void ConfirmCharacter()
    {
        CharacterProfile profile = CreateProfileFromUi();
        CharacterProfileRuntime.SetProfile(profile);
        ApplyProfileToPlayer(profile);
        SetCreationVisible(false);
    }

    CharacterProfile CreateProfileFromUi()
    {
        return new CharacterProfile(
            characterName,
            (CharacterGender)genderIndex,
            (CharacterElement)elementIndex,
            primaryColors[primaryColorIndex],
            metalColors[metalColorIndex],
            (CharacterStatType)boonIndex,
            (CharacterStatType)baneIndex);
    }

    void ApplyProfileToPlayer(CharacterProfile profile)
    {
        if (playerBodyRenderer != null)
        {
            playerBodyRenderer.color = profile.PrimaryColor;
        }

        if (playerWeaponRenderer != null)
        {
            playerWeaponRenderer.color = profile.SecondaryMetalColor;
        }
    }

    void SetCreationVisible(bool visible)
    {
        isCreating = visible;

        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(!visible);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = !visible;
        }

        if (playerCombat != null)
        {
            playerCombat.enabled = !visible;
        }
    }

    int WrapIndex(int value, int count)
    {
        if (value < 0)
        {
            return count - 1;
        }

        if (value >= count)
        {
            return 0;
        }

        return value;
    }
}
