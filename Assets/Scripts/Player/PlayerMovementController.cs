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
    [SerializeField] float interactionRange = 1.65f;
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
        RefreshCurrentInteractable(interactionRange);

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        bool pressed = keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame)
            || gamepad != null && gamepad.buttonNorth.wasPressedThisFrame;

        if (pressed)
        {
            if (dialogueHud != null && dialogueHud.IsOpen)
            {
                dialogueHud.Advance();
                return;
            }

            SimpleInteractable interactable = currentInteractable;
            if (interactable == null)
            {
                interactable = FindNearestInteractable(interactionRange + 1f);
            }

            interactable?.Interact(this);
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

    void RefreshCurrentInteractable(float range)
    {
        currentInteractable = FindNearestInteractable(range);
    }

    SimpleInteractable FindNearestInteractable(float range)
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = interactionLayers,
            useTriggers = true
        };

        int hitCount = Physics2D.OverlapCircle(transform.position, range, filter, interactionHits);
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

            float distance = Vector2.SqrMagnitude(hit.ClosestPoint(transform.position) - (Vector2)transform.position);
            if (distance < nearestDistance)
            {
                nearest = interactable;
                nearestDistance = distance;
            }
        }

        return nearest;
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

        for (int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i].GetType() != typeof(SimpleInteractable))
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

    protected bool WasInteractPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame)
            || gamepad != null && gamepad.buttonNorth.wasPressedThisFrame;
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

    void OnTriggerStay2D(Collider2D other)
    {
        if (!WasInteractPressedThisFrame())
        {
            return;
        }

        PlayerInteractionController player = other.GetComponent<PlayerInteractionController>();
        if (player != null)
        {
            Interact(player);
        }
    }
}

[DisallowMultipleComponent]
public class HealerServiceInteractable : SimpleInteractable
{
    public override void Interact(PlayerInteractionController player)
    {
        PlayerInventoryHudController inventory = player == null ? null : player.GetComponent<PlayerInventoryHudController>();
        inventory?.HealAndRefill();
        player?.ShowDialogue("Healer Shrine", new[]
        {
            "Warm light settles into your armor.",
            "HP and stamina restored."
        });
    }
}

[DisallowMultipleComponent]
public class QuestBoardInteractable : SimpleInteractable
{
    public override void Interact(PlayerInteractionController player)
    {
        player?.ShowDialogue("Route Board", new[]
        {
            "Current combat route work:",
            RouteObjectiveManager.GetQuestBoardText()
        });
    }
}

[DisallowMultipleComponent]
public class RouteRewardChestInteractable : SimpleInteractable
{
    [SerializeField] string routeName = "Forest";

    public override void Interact(PlayerInteractionController player)
    {
        PlayerInventoryHudController inventory = player == null ? null : player.GetComponent<PlayerInventoryHudController>();
        if (RouteObjectiveManager.TryClaimRouteReward(routeName, inventory))
        {
            player?.ShowDialogue($"{routeName} Chest", new[]
            {
                "The route chest opens.",
                "You claimed bonus EXP, Pixicoins, and a route badge."
            });
            return;
        }

        if (RouteObjectiveManager.IsRouteComplete(routeName))
        {
            player?.ShowDialogue($"{routeName} Chest", new[] { "This route reward has already been claimed." });
        }
        else
        {
            player?.ShowDialogue($"{routeName} Chest", new[] { $"Clear the {routeName} route enemies first." });
        }
    }
}

[DisallowMultipleComponent]
public class HubServiceInteractable : SimpleInteractable
{
    [SerializeField] string serviceName = "Service";

    public override void Interact(PlayerInteractionController player)
    {
        player?.ShowDialogue(serviceName, new[]
        {
            $"{serviceName} is a placeholder service.",
            "This is where upgrades, storage, crafting, weapon merges, or elemental infusion can live later."
        });
    }
}

[DisallowMultipleComponent]
public class WeaponSmithInteractable : SimpleInteractable
{
    [SerializeField] string requiredMaterial = "Copper Ore";
    [SerializeField] int requiredCount = 3;
    [SerializeField] WeaponType practicedWeapon = WeaponType.Sword;

    public override void Interact(PlayerInteractionController player)
    {
        PlayerInventoryHudController inventory = player == null ? null : player.GetComponent<PlayerInventoryHudController>();
        if (inventory == null)
        {
            return;
        }

        if (inventory.TrySpendMaterial(requiredMaterial, requiredCount))
        {
            inventory.RewardWeaponPractice(practicedWeapon);
            player.ShowDialogue("Royal Blacksmith", new[]
            {
                $"I reforged your {practicedWeapon} drills with {requiredMaterial}.",
                "For now this raises the matching weapon rank as a prototype upgrade."
            });
            return;
        }

        player.ShowDialogue("Royal Blacksmith", new[]
        {
            $"Bring me {requiredCount} {requiredMaterial} from the forest.",
            "Copper first. Fancy elemental nonsense after the blade stops wobbling."
        });
    }
}

[DisallowMultipleComponent]
public class ForestLootChestInteractable : SimpleInteractable
{
    [SerializeField] bool opened;

    public void ResetLoot()
    {
        opened = false;
    }

    public override void Interact(PlayerInteractionController player)
    {
        if (opened)
        {
            player?.ShowDialogue("Forest Chest", new[] { "The chest is empty." });
            return;
        }

        opened = true;
        PlayerInventoryHudController inventory = player == null ? null : player.GetComponent<PlayerInventoryHudController>();
        if (inventory != null)
        {
            inventory.AddMaterial("Copper Ore", Random.Range(1, 4));
            inventory.AddMaterial("Life Moss", Random.Range(1, 3));
            inventory.AddPixicoins(Random.Range(8, 18));
        }

        player?.ShowDialogue("Forest Chest", new[]
        {
            "You found copper ore and life moss.",
            "The forest is already useful. Dangerous, but useful."
        });
    }
}

[DisallowMultipleComponent]
public class LostWoodsGateInteractable : SimpleInteractable
{
    [SerializeField] LostWoodsDungeonController dungeon = null;
    [SerializeField] bool chooseLeft = false;

    public override void Interact(PlayerInteractionController player)
    {
        if (dungeon != null)
        {
            dungeon.ChoosePath(player, chooseLeft);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!WasInteractPressedThisFrame())
        {
            return;
        }

        PlayerInteractionController player = other.GetComponent<PlayerInteractionController>();
        if (player != null)
        {
            Interact(player);
        }
    }
}

[DisallowMultipleComponent]
public class LostWoodsDungeonController : MonoBehaviour
{
    [SerializeField] Transform playerSpawn = null;
    [SerializeField] CameraAreaBounds2D cameraBounds = null;
    [SerializeField] GameObject[] treeProps = null;
    [SerializeField] GameObject[] enemies = null;
    [SerializeField] GameObject[] chests = null;
    [SerializeField] int targetDepth = 5;
    [SerializeField] int currentDepth;
    [SerializeField] bool leftIsCorrect;

    public void Begin(PlayerInteractionController player)
    {
        currentDepth = 0;
        RegenerateRoom(player);
        MovePlayerToRoom(player);
        ToastHudController.Show("Entered the Lost Woods");
    }

    public void ChoosePath(PlayerInteractionController player, bool choseLeft)
    {
        if (choseLeft == leftIsCorrect)
        {
            currentDepth++;
            if (currentDepth >= targetDepth)
            {
                RewardClear(player);
                currentDepth = 0;
            }
            else
            {
                ToastHudController.Show($"Correct path {currentDepth}/{targetDepth}");
            }
        }
        else
        {
            currentDepth = 0;
            ToastHudController.Show("The woods loop back...");
        }

        RegenerateRoom(player);
        MovePlayerToRoom(player);
    }

    void RewardClear(PlayerInteractionController player)
    {
        PlayerInventoryHudController inventory = player == null ? null : player.GetComponent<PlayerInventoryHudController>();
        if (inventory != null)
        {
            inventory.AddExperience(55f);
            inventory.AddPixicoins(75);
            inventory.AddMaterial("Copper Ore", 4);
            inventory.AddMaterial("Life Moss", 3);
            inventory.AddMaterial("Forest Sigil", 1);
        }

        player?.ShowDialogue("Lost Woods", new[]
        {
            "You found the end of the winding forest path.",
            "Copper, life moss, and a forest sigil are yours."
        });
    }

    void MovePlayerToRoom(PlayerInteractionController player)
    {
        if (player == null || playerSpawn == null)
        {
            return;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = playerSpawn.position;
            rb.linearVelocity = Vector2.zero;
        }

        player.transform.position = playerSpawn.position;

        CameraFollow2D follow = Camera.main == null ? null : Camera.main.GetComponent<CameraFollow2D>();
        cameraBounds?.ApplyTo(follow);
    }

    void RegenerateRoom(PlayerInteractionController player)
    {
        leftIsCorrect = Random.value > 0.5f;
        RandomizeProps(treeProps, 18, new Vector2(-4.8f, 4.8f), new Vector2(-2.8f, 3.2f));
        RandomizeProps(chests, Random.value > 0.45f ? 1 : 0, new Vector2(-3.7f, 3.7f), new Vector2(-1.7f, 2.35f));
        RandomizeEnemies(player == null ? null : player.transform);
    }

    void RandomizeProps(GameObject[] props, int activeCount, Vector2 xRange, Vector2 yRange)
    {
        if (props == null)
        {
            return;
        }

        for (int i = 0; i < props.Length; i++)
        {
            bool active = i < activeCount;
            props[i].SetActive(active);
            if (active)
            {
                props[i].transform.localPosition = new Vector3(Random.Range(xRange.x, xRange.y), Random.Range(yRange.x, yRange.y), 0f);
                ForestLootChestInteractable chest = props[i].GetComponent<ForestLootChestInteractable>();
                chest?.ResetLoot();
            }
        }
    }

    void RandomizeEnemies(Transform player)
    {
        if (enemies == null)
        {
            return;
        }

        int activeCount = Mathf.Clamp(1 + currentDepth / 2 + Random.Range(0, 2), 1, enemies.Length);
        for (int i = 0; i < enemies.Length; i++)
        {
            bool active = i < activeCount;
            enemies[i].SetActive(active);
            if (!active)
            {
                continue;
            }

            enemies[i].transform.localPosition = new Vector3(Random.Range(-3.9f, 3.9f), Random.Range(-1.7f, 2.35f), 0f);

            HealthComponent health = enemies[i].GetComponent<HealthComponent>();
            health?.Refill();

            EnemyCombatController enemy = enemies[i].GetComponent<EnemyCombatController>();
            if (enemy != null && player != null)
            {
                enemy.SetTarget(player);
            }
        }
    }
}

[DisallowMultipleComponent]
public class LostWoodsEntranceInteractable : SimpleInteractable
{
    [SerializeField] LostWoodsDungeonController dungeon = null;

    public override void Interact(PlayerInteractionController player)
    {
        dungeon?.Begin(player);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!WasInteractPressedThisFrame())
        {
            return;
        }

        PlayerInteractionController player = other.GetComponent<PlayerInteractionController>();
        if (player != null)
        {
            Interact(player);
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

[DisallowMultipleComponent]
public class ToastHudController : MonoBehaviour
{
    static ToastHudController instance;

    readonly List<ToastMessage> messages = new List<ToastMessage>();
    GUIStyle toastStyle;

    void Awake()
    {
        instance = this;
    }

    public static void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (instance == null)
        {
            GameObject hud = new GameObject("Toast HUD");
            instance = hud.AddComponent<ToastHudController>();
        }

        instance.messages.Add(new ToastMessage(message, Time.unscaledTime + 3f));
    }

    void OnGUI()
    {
        InitializeStyle();

        for (int i = messages.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime > messages[i].ExpiresAt)
            {
                messages.RemoveAt(i);
            }
        }

        float y = 118f;
        for (int i = messages.Count - 1; i >= 0 && i >= messages.Count - 5; i--)
        {
            GUI.Box(new Rect(Screen.width - 292f, y, 270f, 30f), messages[i].Text, toastStyle);
            y += 34f;
        }
    }

    void InitializeStyle()
    {
        toastStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 15,
            padding = new RectOffset(12, 8, 4, 4),
            normal = { textColor = Color.white }
        };
    }

    readonly struct ToastMessage
    {
        public readonly string Text;
        public readonly float ExpiresAt;

        public ToastMessage(string text, float expiresAt)
        {
            Text = text;
            ExpiresAt = expiresAt;
        }
    }
}

[DisallowMultipleComponent]
public class FloatingCombatTextHud : MonoBehaviour
{
    static FloatingCombatTextHud instance;

    readonly List<FloatingText> texts = new List<FloatingText>();
    GUIStyle style;

    void Awake()
    {
        instance = this;
    }

    public static void Show(Vector3 worldPosition, string text, Color color)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (instance == null)
        {
            GameObject hud = new GameObject("Floating Combat Text HUD");
            instance = hud.AddComponent<FloatingCombatTextHud>();
        }

        instance.texts.Add(new FloatingText(worldPosition, text, color, Time.time));
    }

    void OnGUI()
    {
        if (Camera.main == null)
        {
            return;
        }

        InitializeStyle();

        for (int i = texts.Count - 1; i >= 0; i--)
        {
            FloatingText item = texts[i];
            float age = Time.time - item.StartTime;
            if (age > 0.9f)
            {
                texts.RemoveAt(i);
                continue;
            }

            Vector3 screen = Camera.main.WorldToScreenPoint(item.WorldPosition + Vector3.up * age * 0.75f);
            if (screen.z < 0f)
            {
                continue;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(item.Color.r, item.Color.g, item.Color.b, 1f - age / 0.9f);
            GUI.Label(new Rect(screen.x - 45f, Screen.height - screen.y - 20f, 90f, 26f), item.Text, style);
            GUI.color = oldColor;
        }
    }

    void InitializeStyle()
    {
        style ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
    }

    readonly struct FloatingText
    {
        public readonly Vector3 WorldPosition;
        public readonly string Text;
        public readonly Color Color;
        public readonly float StartTime;

        public FloatingText(Vector3 worldPosition, string text, Color color, float startTime)
        {
            WorldPosition = worldPosition;
            Text = text;
            Color = color;
            StartTime = startTime;
        }
    }
}

[DisallowMultipleComponent]
public class ControlHelpHudController : MonoBehaviour
{
    [SerializeField] bool showHelp;

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
        {
            showHelp = !showHelp;
        }
    }

    void OnGUI()
    {
        if (!showHelp)
        {
            GUI.Box(new Rect(16f, Screen.height - 34f, 164f, 24f), "H: Controls");
            return;
        }

        Rect panel = new Rect(18f, Screen.height * 0.5f - 150f, 360f, 300f);
        GUI.Box(panel, "Controls");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 34f, 330f, 24f), "Move: WASD / Arrows / Left Stick");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 62f, 330f, 24f), "Interact / dialogue: E / Y");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 90f, 330f, 24f), "Jump: Q / A");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 118f, 330f, 24f), "Light attack: Space / X");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 146f, 330f, 24f), "Strong attack: F / RB");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 174f, 330f, 24f), "Dodge: Shift or Ctrl / B");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 202f, 330f, 24f), "Charge: hold C / Right Trigger");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 230f, 330f, 24f), "Crouch: hold X / Left Stick Press");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 258f, 330f, 24f), "Inventory: I or Tab");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 282f, 330f, 24f), "Prototype save/load: F5 / F9");
    }
}
