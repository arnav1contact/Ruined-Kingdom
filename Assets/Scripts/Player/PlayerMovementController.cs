using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] InputActionReference moveAction = null;
    [SerializeField] bool useDirectInputFallback = true;

    [Header("Read Only Debug Info")]
    [SerializeField] Vector2 currentMovementInput;
    [SerializeField] Vector2 currentMovementDirection;
    [SerializeField] Vector2 lastNonZeroFacingDirection = Vector2.down;
    [SerializeField] Vector2 currentVelocity;
    [SerializeField] bool isMoving;
    [SerializeField] bool movementSuppressed;

    Rigidbody2D rb;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    public Vector2 CurrentMovementInput => currentMovementInput;
    public Vector2 CurrentMovementDirection => currentMovementDirection;
    public Vector2 LastNonZeroFacingDirection => lastNonZeroFacingDirection;
    public Vector2 CurrentVelocity => currentVelocity;
    public bool IsMoving => isMoving;
    public bool IsMovementSuppressed => movementSuppressed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        ApplyTopDownRigidbodyDefaults();
    }

    void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }

    void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Disable();
        }
    }

    void Update()
    {
        ReadMovementInput();
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (movementSuppressed)
        {
            currentVelocity = Vector2.zero;
            return;
        }

        currentVelocity = currentMovementDirection * moveSpeed;
        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
    }

    public void SetMovementSuppressed(bool isSuppressed)
    {
        movementSuppressed = isSuppressed;
    }

    void ReadMovementInput()
    {
        Vector2 actionInput = moveAction == null || moveAction.action == null
            ? Vector2.zero
            : moveAction.action.ReadValue<Vector2>();

        Vector2 directInput = useDirectInputFallback ? ReadDirectMovementInput() : Vector2.zero;
        currentMovementInput = actionInput.sqrMagnitude > 0.0001f ? actionInput : directInput;
        currentMovementDirection = Vector2.ClampMagnitude(currentMovementInput, 1f);
        isMoving = currentMovementDirection.sqrMagnitude > 0.0001f;

        if (isMoving)
        {
            lastNonZeroFacingDirection = currentMovementDirection.normalized;
        }
    }

    Vector2 ReadDirectMovementInput()
    {
        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stickInput = gamepad.leftStick.ReadValue();
            if (stickInput.sqrMagnitude > input.sqrMagnitude)
            {
                input = stickInput;
            }
        }

        return input;
    }

    void ApplyTopDownRigidbodyDefaults()
    {
        if (rb == null)
        {
            return;
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
}

[DisallowMultipleComponent]
public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] float interactionRange = 1.05f;
    [SerializeField] LayerMask interactionLayers = ~0;
    [SerializeField] SimpleInteractable currentInteractable = null;
    [SerializeField] DialogueHudController dialogueHud = null;

    readonly List<Collider2D> interactionHits = new List<Collider2D>(16);

    public SimpleInteractable CurrentInteractable => currentInteractable;

    void Awake()
    {
        if (dialogueHud == null)
        {
            dialogueHud = FindFirstObjectByType<DialogueHudController>();
        }
    }

    void Update()
    {
        RefreshCurrentInteractable();

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        bool pressed = keyboard != null && keyboard.eKey.wasPressedThisFrame
            || gamepad != null && gamepad.buttonNorth.wasPressedThisFrame;

        if (pressed)
        {
            if (dialogueHud != null && dialogueHud.IsOpen)
            {
                dialogueHud.Advance();
                return;
            }

            currentInteractable?.Interact(this);
        }
    }

    void OnGUI()
    {
        if (currentInteractable == null || dialogueHud != null && dialogueHud.IsOpen)
        {
            return;
        }

        Rect promptRect = new Rect(Screen.width * 0.5f - 150f, Screen.height - 164f, 300f, 34f);
        GUI.Box(promptRect, $"E  {currentInteractable.PromptText}");
    }

    public void ShowDialogue(string speakerName, string[] lines)
    {
        if (dialogueHud != null)
        {
            dialogueHud.Show(speakerName, lines);
        }
    }

    void RefreshCurrentInteractable()
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = interactionLayers,
            useTriggers = true
        };

        int hitCount = Physics2D.OverlapCircle(transform.position, interactionRange, filter, interactionHits);
        SimpleInteractable nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = interactionHits[i];
            if (hit == null)
            {
                continue;
            }

            SimpleInteractable interactable = FindBestInteractable(hit);
            if (interactable == null || !interactable.isActiveAndEnabled)
            {
                continue;
            }

            float distance = Vector2.SqrMagnitude(interactable.transform.position - transform.position);
            if (distance < nearestDistance)
            {
                nearest = interactable;
                nearestDistance = distance;
            }
        }

        currentInteractable = nearest;
    }

    SimpleInteractable FindBestInteractable(Collider2D hit)
    {
        SimpleInteractable[] interactables = hit.GetComponentsInParent<SimpleInteractable>();
        if (interactables == null || interactables.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i] is ZoneGateInteractable)
            {
                return interactables[i];
            }
        }

        return interactables[0];
    }
}

[DisallowMultipleComponent]
public class DialogueHudController : MonoBehaviour
{
    [SerializeField] string speakerName = "";
    [SerializeField] string[] lines = null;
    [SerializeField] int lineIndex;
    [SerializeField] bool isOpen;

    GUIStyle speakerStyle;
    GUIStyle bodyStyle;

    public bool IsOpen => isOpen;

    public void Show(string newSpeakerName, string[] newLines)
    {
        if (newLines == null || newLines.Length == 0)
        {
            return;
        }

        speakerName = newSpeakerName;
        lines = newLines;
        lineIndex = 0;
        isOpen = true;
    }

    public void Advance()
    {
        if (!isOpen)
        {
            return;
        }

        lineIndex++;
        if (lines == null || lineIndex >= lines.Length)
        {
            Close();
        }
    }

    public void Close()
    {
        isOpen = false;
    }

    void OnGUI()
    {
        if (!isOpen || lines == null || lineIndex >= lines.Length)
        {
            return;
        }

        InitializeStyles();

        Rect panel = new Rect(24f, Screen.height - 156f, Screen.width - 48f, 124f);
        GUI.Box(panel, "");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, panel.width - 36f, 26f), speakerName, speakerStyle);
        GUI.Label(new Rect(panel.x + 18f, panel.y + 42f, panel.width - 36f, 54f), lines[lineIndex], bodyStyle);
        GUI.Label(new Rect(panel.xMax - 172f, panel.yMax - 26f, 152f, 20f), "E / Y to continue");
    }

    void InitializeStyles()
    {
        speakerStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        bodyStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
    }
}

[DisallowMultipleComponent]
public class SimpleInteractable : MonoBehaviour
{
    [SerializeField] string displayName = "Sign";
    [SerializeField] string promptText = "Inspect";
    [SerializeField] string[] dialogueLines = null;

    public string DisplayName => displayName;
    public string PromptText => promptText;

    public virtual void Interact(PlayerInteractionController player)
    {
        player?.ShowDialogue(displayName, dialogueLines);
    }
}

[DisallowMultipleComponent]
public class ZoneGateInteractable : SimpleInteractable
{
    [SerializeField] Transform destination = null;
    [SerializeField] CameraAreaBounds2D destinationBounds = null;
    [SerializeField] string arrivalMessage = "";

    public override void Interact(PlayerInteractionController player)
    {
        base.Interact(player);

        if (player == null || destination == null)
        {
            return;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = destination.position;
            rb.linearVelocity = Vector2.zero;
        }

        player.transform.position = destination.position;

        CameraFollow2D follow = Camera.main == null ? null : Camera.main.GetComponent<CameraFollow2D>();
        if (destinationBounds != null)
        {
            destinationBounds.ApplyTo(follow);
        }

        if (!string.IsNullOrWhiteSpace(arrivalMessage))
        {
            player.ShowDialogue("Ruined Kingdom", new[] { arrivalMessage });
        }
    }
}

[DisallowMultipleComponent]
public class HubWorldClockController : MonoBehaviour
{
    [SerializeField] int day = 1;
    [SerializeField] float minutesSinceMidnight = 6f * 60f;
    [SerializeField] float realSecondsPerGameMinute = 1.5f;
    [SerializeField] bool pauseWhenTimeScaleIsZero = true;

    public int Day => day;
    public int Hour => Mathf.FloorToInt(minutesSinceMidnight / 60f) % 24;
    public int Minute => Mathf.FloorToInt(minutesSinceMidnight % 60f);

    void Update()
    {
        if (pauseWhenTimeScaleIsZero && Time.timeScale <= 0f)
        {
            return;
        }

        minutesSinceMidnight += Time.deltaTime / Mathf.Max(0.1f, realSecondsPerGameMinute);
        if (minutesSinceMidnight >= 24f * 60f)
        {
            minutesSinceMidnight -= 24f * 60f;
            day++;
        }
    }

    void OnGUI()
    {
        DrawClock();
        DrawTimeTint();
    }

    void DrawClock()
    {
        Rect clockRect = new Rect(Screen.width - 170f, 56f, 150f, 48f);
        GUI.Box(clockRect, $"Day {day}\n{Hour:00}:{Minute:00}");
    }

    void DrawTimeTint()
    {
        float hour = minutesSinceMidnight / 60f;
        Color tint = Color.clear;

        if (hour < 5f || hour >= 21f)
        {
            tint = new Color(0.02f, 0.04f, 0.12f, 0.28f);
        }
        else if (hour < 7f)
        {
            tint = new Color(1f, 0.55f, 0.22f, 0.08f);
        }
        else if (hour >= 18f)
        {
            tint = new Color(1f, 0.32f, 0.18f, 0.12f);
        }

        if (tint.a <= 0f)
        {
            return;
        }

        Color oldColor = GUI.color;
        GUI.color = tint;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = oldColor;
    }
}
