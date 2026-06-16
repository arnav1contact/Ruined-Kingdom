using UnityEngine;

[DisallowMultipleComponent]
public class MovementDebugOverlay : MonoBehaviour
{
    [SerializeField] PlayerMovementController playerMovement = null;
    [SerializeField] PlayerCombatController playerCombat = null;
    [SerializeField] bool showOverlay = true;

    GUIStyle labelStyle;

    void Reset()
    {
        playerMovement = FindFirstObjectByType<PlayerMovementController>();
        playerCombat = FindFirstObjectByType<PlayerCombatController>();
    }

    void OnGUI()
    {
        if (!showOverlay || playerMovement == null)
        {
            return;
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal =
                {
                    textColor = Color.white
                }
            };
        }

        GUI.Box(new Rect(12f, 12f, 430f, 250f), "Movement Debug");
        GUI.Label(new Rect(24f, 42f, 330f, 24f), $"Input: {FormatVector(playerMovement.CurrentMovementInput)}", labelStyle);
        GUI.Label(new Rect(24f, 66f, 330f, 24f), $"Direction: {FormatVector(playerMovement.CurrentMovementDirection)}", labelStyle);
        GUI.Label(new Rect(24f, 90f, 330f, 24f), $"Velocity: {FormatVector(playerMovement.CurrentVelocity)}", labelStyle);
        GUI.Label(new Rect(24f, 114f, 330f, 24f), $"Facing: {FormatVector(playerMovement.LastNonZeroFacingDirection)}", labelStyle);
        GUI.Label(new Rect(24f, 138f, 330f, 24f), $"Moving: {playerMovement.IsMoving}", labelStyle);
        GUI.Label(new Rect(24f, 162f, 330f, 24f), $"Action: {GetActionText()}", labelStyle);
        GUI.Label(new Rect(24f, 190f, 395f, 24f), "Keys: E interact, Q jump, Space light, F strong", labelStyle);
        GUI.Label(new Rect(24f, 214f, 395f, 24f), "Keys: Shift dodge, C charge, X crouch, I inventory", labelStyle);
        GUI.Label(new Rect(24f, 238f, 395f, 24f), "Pad: Y interact, A jump, X light, RB strong, B dodge", labelStyle);
    }

    static string FormatVector(Vector2 value)
    {
        return $"({value.x:0.00}, {value.y:0.00})";
    }

    string GetActionText()
    {
        return playerCombat == null ? "None" : playerCombat.CurrentAction;
    }
}
