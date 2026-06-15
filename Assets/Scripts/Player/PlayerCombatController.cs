using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerMovementController movementController = null;
    [SerializeField] HealthComponent health = null;
    [SerializeField] StaminaComponent stamina = null;
    [SerializeField] PlayerInventoryHudController inventory = null;
    [SerializeField] InputActionReference attackAction = null;
    [SerializeField] Transform weaponPivot = null;
    [SerializeField] Transform weaponBlade = null;
    [SerializeField] SpriteRenderer weaponRenderer = null;

    [Header("Light Attack")]
    [SerializeField] float lightDamage = 18f;
    [SerializeField] float lightStaminaCost = 12f;
    [SerializeField] float lightCooldown = 0.28f;

    [Header("Strong Attack")]
    [SerializeField] float strongDamage = 36f;
    [SerializeField] float strongStaminaCost = 28f;
    [SerializeField] float strongCooldown = 0.65f;

    [Header("Charge Attack")]
    [SerializeField] float chargeDamage = 55f;
    [SerializeField] float chargeStaminaCost = 40f;
    [SerializeField] float minimumChargeTime = 0.55f;
    [SerializeField] float maximumChargeTime = 1.5f;
    [SerializeField] float chargeCooldown = 0.9f;

    [Header("Dodge And Jump")]
    [SerializeField] float dodgeStaminaCost = 25f;
    [SerializeField] float dodgeDistance = 1.8f;
    [SerializeField] float dodgeDuration = 0.18f;
    [SerializeField] float dodgeCooldown = 0.35f;
    [SerializeField] float jumpStaminaCost = 18f;
    [SerializeField] float jumpDistance = 0.9f;
    [SerializeField] float jumpDuration = 0.32f;
    [SerializeField] float jumpCooldown = 0.55f;

    [Header("Sword Hitbox")]
    [SerializeField] Vector2 lightHitboxSize = new Vector2(1.1f, 0.65f);
    [SerializeField] Vector2 strongHitboxSize = new Vector2(1.35f, 0.8f);
    [SerializeField] Vector2 chargeHitboxSize = new Vector2(1.65f, 0.9f);
    [SerializeField] float hitboxForwardOffset = 0.85f;
    [SerializeField] LayerMask hitLayers = ~0;

    [Header("Read Only Debug Info")]
    [SerializeField] string currentAction = "None";
    [SerializeField] bool actionBusy;
    [SerializeField] bool isCharging;
    [SerializeField] bool isCrouching;

    Rigidbody2D rb;
    Vector3 originalScale;
    float nextAttackTime;
    float nextDodgeTime;
    float nextJumpTime;
    float chargeStartedTime;

    public bool IsActionBusy => actionBusy;
    public bool IsCharging => isCharging;
    public bool IsCrouching => isCrouching;
    public string CurrentAction => currentAction;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMovementController>();
        }

        if (health == null)
        {
            health = GetComponent<HealthComponent>();
        }

        if (stamina == null)
        {
            stamina = GetComponent<StaminaComponent>();
        }

        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventoryHudController>();
        }

        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        if (attackAction != null && attackAction.action != null)
        {
            attackAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (attackAction != null && attackAction.action != null)
        {
            attackAction.action.Disable();
        }
    }

    void Update()
    {
        UpdateWeaponPose();
        isCrouching = IsCrouchHeld();

        if (actionBusy)
        {
            return;
        }

        HandleChargeInput();

        if (WasDodgePressed())
        {
            TryStartDodge();
        }
        else if (WasJumpPressed())
        {
            TryStartJump();
        }
        else if (WasStrongAttackPressed())
        {
            TryStartWeaponAttack(CreateAttackProfile(true, isCrouching));
        }
        else if (WasLightAttackPressed())
        {
            TryStartWeaponAttack(CreateAttackProfile(false, isCrouching));
        }
    }

    void OnValidate()
    {
        lightDamage = Mathf.Max(0f, lightDamage);
        lightStaminaCost = Mathf.Max(0f, lightStaminaCost);
        lightCooldown = Mathf.Max(0f, lightCooldown);
        strongDamage = Mathf.Max(0f, strongDamage);
        strongStaminaCost = Mathf.Max(0f, strongStaminaCost);
        strongCooldown = Mathf.Max(0f, strongCooldown);
        chargeDamage = Mathf.Max(0f, chargeDamage);
        chargeStaminaCost = Mathf.Max(0f, chargeStaminaCost);
        minimumChargeTime = Mathf.Max(0f, minimumChargeTime);
        maximumChargeTime = Mathf.Max(minimumChargeTime, maximumChargeTime);
        chargeCooldown = Mathf.Max(0f, chargeCooldown);
        dodgeStaminaCost = Mathf.Max(0f, dodgeStaminaCost);
        dodgeDistance = Mathf.Max(0f, dodgeDistance);
        dodgeDuration = Mathf.Max(0.01f, dodgeDuration);
        dodgeCooldown = Mathf.Max(0f, dodgeCooldown);
        jumpStaminaCost = Mathf.Max(0f, jumpStaminaCost);
        jumpDistance = Mathf.Max(0f, jumpDistance);
        jumpDuration = Mathf.Max(0.01f, jumpDuration);
        jumpCooldown = Mathf.Max(0f, jumpCooldown);
    }

    void HandleChargeInput()
    {
        if (WasChargePressed())
        {
            isCharging = true;
            chargeStartedTime = Time.time;
            currentAction = "Charging";
            SetWeaponColor(new Color(1f, 0.75f, 0.15f));
        }

        if (isCharging && WasChargeReleased())
        {
            float chargeTime = Mathf.Clamp(Time.time - chargeStartedTime, minimumChargeTime, maximumChargeTime);
            isCharging = false;

            if (chargeTime >= minimumChargeTime)
            {
                TryStartSwordAttack("Charge Attack", chargeDamage, chargeStaminaCost, chargeCooldown, 0.32f, chargeHitboxSize, new Color(1f, 0.75f, 0.15f));
            }
            else
            {
                currentAction = "None";
                SetWeaponColor(Color.white);
            }
        }
    }

    bool WasLightAttackPressed()
    {
        return attackAction != null && attackAction.action != null && attackAction.action.WasPressedThisFrame()
            || Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame
            || Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
            || Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
    }

    bool WasStrongAttackPressed()
    {
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame
            || Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame
            || Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
    }

    bool WasDodgePressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        return keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.leftCtrlKey.wasPressedThisFrame)
            || gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
    }

    bool WasJumpPressed()
    {
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
            || Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
    }

    bool WasChargePressed()
    {
        return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame
            || Gamepad.current != null && gamepadRightTriggerPressed();
    }

    bool WasChargeReleased()
    {
        return Keyboard.current != null && Keyboard.current.cKey.wasReleasedThisFrame
            || Gamepad.current != null && gamepadRightTriggerReleased();
    }

    bool gamepadRightTriggerPressed()
    {
        return Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame;
    }

    bool gamepadRightTriggerReleased()
    {
        return Gamepad.current != null && Gamepad.current.rightTrigger.wasReleasedThisFrame;
    }

    bool IsCrouchHeld()
    {
        return Keyboard.current != null && Keyboard.current.xKey.isPressed
            || Gamepad.current != null && Gamepad.current.leftStickButton.isPressed;
    }

    AttackProfile CreateAttackProfile(bool strong, bool crouching)
    {
        WeaponType weapon = inventory == null ? WeaponType.Sword : inventory.SelectedWeapon;
        if (weapon == WeaponType.None)
        {
            weapon = WeaponType.Sword;
        }

        return weapon switch
        {
            WeaponType.Lance => CreateLanceProfile(strong, crouching),
            WeaponType.Axe => CreateAxeProfile(strong, crouching),
            _ => CreateSwordProfile(strong, crouching)
        };
    }

    AttackProfile CreateSwordProfile(bool strong, bool crouching)
    {
        if (crouching)
        {
            return new AttackProfile(strong ? "Crouch Sword Strong Poke" : "Crouch Sword Light Poke", strong ? 30f : 16f, strong ? 24f : 10f, strong ? 0.55f : 0.25f, strong ? 0.22f : 0.1f, new Vector2(strong ? 1.45f : 1.15f, 0.34f), strong ? 1f : 0.9f, strong ? Color.red : Color.white, strong ? 1.35f : 1.15f);
        }

        return new AttackProfile(strong ? "Sword Strong Slash" : "Sword Light Slash", strong ? strongDamage : lightDamage, strong ? strongStaminaCost : lightStaminaCost, strong ? strongCooldown : lightCooldown, strong ? 0.24f : 0.14f, strong ? strongHitboxSize : lightHitboxSize, 0.85f, strong ? Color.red : Color.white, strong ? 1.25f : 1.1f);
    }

    AttackProfile CreateLanceProfile(bool strong, bool crouching)
    {
        if (crouching)
        {
            return new AttackProfile(strong ? "Crouch Lance Brace" : "Crouch Lance Jab", strong ? 34f : 18f, strong ? 24f : 10f, strong ? 0.62f : 0.3f, strong ? 0.28f : 0.12f, new Vector2(strong ? 2.2f : 1.75f, 0.3f), strong ? 1.45f : 1.25f, new Color(0.65f, 0.9f, 1f), strong ? 1.8f : 1.55f);
        }

        return new AttackProfile(strong ? "Lance Heavy Thrust" : "Lance Thrust", strong ? 38f : 20f, strong ? 30f : 13f, strong ? 0.72f : 0.36f, strong ? 0.32f : 0.16f, new Vector2(strong ? 2.25f : 1.8f, 0.36f), strong ? 1.45f : 1.25f, new Color(0.65f, 0.9f, 1f), strong ? 1.9f : 1.6f);
    }

    AttackProfile CreateAxeProfile(bool strong, bool crouching)
    {
        if (crouching)
        {
            return new AttackProfile(strong ? "Crouch Axe Chop" : "Crouch Axe Hook", strong ? 48f : 24f, strong ? 34f : 16f, strong ? 0.9f : 0.5f, strong ? 0.45f : 0.24f, new Vector2(strong ? 1.55f : 1.15f, strong ? 1f : 0.75f), 0.75f, new Color(1f, 0.45f, 0.15f), strong ? 1.35f : 1.1f);
        }

        return new AttackProfile(strong ? "Axe Heavy Swing" : "Axe Swing", strong ? 62f : 30f, strong ? 42f : 20f, strong ? 1.05f : 0.58f, strong ? 0.5f : 0.28f, new Vector2(strong ? 1.75f : 1.35f, strong ? 1.15f : 0.9f), 0.7f, new Color(1f, 0.45f, 0.15f), strong ? 1.4f : 1.15f);
    }

    void TryStartSwordAttack(string actionName, float damage, float staminaCost, float cooldown, float windup, Vector2 hitboxSize, Color weaponColor)
    {
        TryStartWeaponAttack(new AttackProfile(actionName, damage, staminaCost, cooldown, windup, hitboxSize, hitboxForwardOffset, weaponColor, 1.25f));
    }

    void TryStartWeaponAttack(AttackProfile profile)
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (stamina != null && !stamina.TrySpend(profile.StaminaCost))
        {
            return;
        }

        nextAttackTime = Time.time + profile.Cooldown;
        StartCoroutine(WeaponAttackRoutine(profile));
    }

    IEnumerator WeaponAttackRoutine(AttackProfile profile)
    {
        actionBusy = true;
        currentAction = profile.Name;
        movementController?.SetMovementSuppressed(true);
        SetWeaponColor(profile.Color);
        SetWeaponReach(profile.ReachScale);

        yield return new WaitForSeconds(profile.Windup);

        HitWithWeapon(profile.Damage, profile.HitboxSize, profile.ForwardOffset);

        yield return new WaitForSeconds(0.08f);

        SetWeaponReach(1f);
        SetWeaponColor(Color.white);
        movementController?.SetMovementSuppressed(false);
        currentAction = "None";
        actionBusy = false;
    }

    void TryStartDodge()
    {
        if (Time.time < nextDodgeTime || stamina != null && !stamina.TrySpend(dodgeStaminaCost))
        {
            return;
        }

        nextDodgeTime = Time.time + dodgeCooldown;
        StartCoroutine(DodgeRoutine());
    }

    IEnumerator DodgeRoutine()
    {
        actionBusy = true;
        currentAction = "Dodge Roll";
        health?.SetInvulnerable(true);
        movementController?.SetMovementSuppressed(true);

        Vector2 direction = GetActionDirection();
        float elapsed = 0f;

        while (elapsed < dodgeDuration)
        {
            float step = dodgeDistance / dodgeDuration * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + direction * step);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        health?.SetInvulnerable(false);
        movementController?.SetMovementSuppressed(false);
        currentAction = "None";
        actionBusy = false;
    }

    void TryStartJump()
    {
        if (Time.time < nextJumpTime || stamina != null && !stamina.TrySpend(jumpStaminaCost))
        {
            return;
        }

        nextJumpTime = Time.time + jumpCooldown;
        StartCoroutine(JumpRoutine());
    }

    IEnumerator JumpRoutine()
    {
        actionBusy = true;
        currentAction = "Jump";
        health?.SetInvulnerable(true);
        movementController?.SetMovementSuppressed(true);

        Vector2 direction = GetActionDirection();
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            float progress = elapsed / jumpDuration;
            float arc = Mathf.Sin(progress * Mathf.PI);
            transform.localScale = originalScale * (1f + arc * 0.28f);
            rb.MovePosition(rb.position + direction * (jumpDistance / jumpDuration * Time.fixedDeltaTime));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        transform.localScale = originalScale;
        health?.SetInvulnerable(false);
        movementController?.SetMovementSuppressed(false);
        currentAction = "None";
        actionBusy = false;
    }

    void HitWithWeapon(float damage, Vector2 hitboxSize, float forwardOffset)
    {
        Vector2 direction = GetFacingDirection();
        Vector2 center = (Vector2)transform.position + direction * forwardOffset;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitboxSize, angle, hitLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == gameObject)
            {
                continue;
            }

            HealthComponent targetHealth = hit.GetComponentInParent<HealthComponent>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }
        }
    }

    Vector2 GetActionDirection()
    {
        if (movementController != null && movementController.CurrentMovementDirection.sqrMagnitude > 0.0001f)
        {
            return movementController.CurrentMovementDirection.normalized;
        }

        return GetFacingDirection();
    }

    Vector2 GetFacingDirection()
    {
        if (movementController == null || movementController.LastNonZeroFacingDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector2.down;
        }

        return movementController.LastNonZeroFacingDirection.normalized;
    }

    void UpdateWeaponPose()
    {
        if (weaponPivot == null)
        {
            return;
        }

        Vector2 direction = GetFacingDirection();
        weaponPivot.localPosition = direction * 0.48f;
        weaponPivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    void SetWeaponReach(float reachScale)
    {
        if (weaponBlade != null)
        {
            weaponBlade.localScale = new Vector3(0.85f * reachScale, 0.16f, 1f);
        }
    }

    readonly struct AttackProfile
    {
        public readonly string Name;
        public readonly float Damage;
        public readonly float StaminaCost;
        public readonly float Cooldown;
        public readonly float Windup;
        public readonly Vector2 HitboxSize;
        public readonly float ForwardOffset;
        public readonly Color Color;
        public readonly float ReachScale;

        public AttackProfile(string name, float damage, float staminaCost, float cooldown, float windup, Vector2 hitboxSize, float forwardOffset, Color color, float reachScale)
        {
            Name = name;
            Damage = damage;
            StaminaCost = staminaCost;
            Cooldown = cooldown;
            Windup = windup;
            HitboxSize = hitboxSize;
            ForwardOffset = forwardOffset;
            Color = color;
            ReachScale = reachScale;
        }
    }

    void SetWeaponColor(Color color)
    {
        if (weaponRenderer != null)
        {
            weaponRenderer.color = color;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector2 direction = Application.isPlaying ? GetFacingDirection() : Vector2.down;
        Vector2 center = (Vector2)transform.position + direction * hitboxForwardOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, lightHitboxSize);
    }
}

public enum WeaponType
{
    None,
    Sword,
    Lance,
    Axe
}

public enum WeaponRank
{
    None,
    E,
    D,
    C,
    B,
    A,
    S
}

[DisallowMultipleComponent]
public class PlayerInventoryHudController : MonoBehaviour
{
    [SerializeField] HealthComponent health = null;
    [SerializeField] StaminaComponent stamina = null;
    [SerializeField] WeaponType[] hotbar = new WeaponType[8];
    [SerializeField] WeaponType[] inventory = new WeaponType[20];
    [SerializeField] int selectedHotbarIndex;
    [SerializeField] int level = 11;
    [SerializeField] float experience = 0f;
    [SerializeField] float experienceToNextLevel = 100f;
    [SerializeField] int pixicoins = 250;
    [SerializeField] WeaponRank swordRank = WeaponRank.None;
    [SerializeField] WeaponRank lanceRank = WeaponRank.None;
    [SerializeField] WeaponRank axeRank = WeaponRank.None;

    bool inventoryOpen;
    WeaponType heldItem;

    readonly Key[] hotbarKeys =
    {
        Key.Digit1,
        Key.Digit2,
        Key.Digit3,
        Key.Digit4,
        Key.Digit5,
        Key.Digit6,
        Key.Digit7,
        Key.Digit8
    };

    public WeaponType SelectedWeapon => hotbar == null || hotbar.Length == 0 ? WeaponType.Sword : hotbar[selectedHotbarIndex];

    void Awake()
    {
        if (health == null)
        {
            health = GetComponent<HealthComponent>();
        }

        if (stamina == null)
        {
            stamina = GetComponent<StaminaComponent>();
        }

        InitializeDefaultItems();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.iKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)
        {
            SetInventoryOpen(!inventoryOpen);
        }

        for (int i = 0; i < hotbar.Length && i < hotbarKeys.Length; i++)
        {
            if (keyboard[hotbarKeys[i]].wasPressedThisFrame)
            {
                selectedHotbarIndex = i;
            }
        }
    }

    void OnGUI()
    {
        DrawResourceBars();
        DrawPixicoins();
        DrawHotbar();
        DrawExperienceBar();

        if (inventoryOpen)
        {
            DrawInventory();
        }
    }

    void InitializeDefaultItems()
    {
        if (hotbar == null || hotbar.Length != 8)
        {
            hotbar = new WeaponType[8];
        }

        if (inventory == null || inventory.Length != 20)
        {
            inventory = new WeaponType[20];
        }

        if (hotbar[0] == WeaponType.None && hotbar[1] == WeaponType.None && hotbar[2] == WeaponType.None)
        {
            hotbar[0] = WeaponType.Sword;
            hotbar[1] = WeaponType.Lance;
            hotbar[2] = WeaponType.Axe;
            swordRank = WeaponRank.E;
        }
    }

    void DrawResourceBars()
    {
        DrawBar(new Rect(18f, Screen.height - 132f, 220f, 22f), "HP", health == null ? 0f : health.HealthPercent, new Color(0.86f, 0.08f, 0.1f));
        DrawBar(new Rect(18f, Screen.height - 104f, 220f, 22f), "STA", stamina == null ? 0f : stamina.StaminaPercent, new Color(0.1f, 0.5f, 1f));
    }

    void DrawExperienceBar()
    {
        float slotSize = 52f;
        float totalWidth = hotbar.Length * slotSize;
        float startX = (Screen.width - totalWidth) * 0.5f;
        DrawBar(new Rect(startX, Screen.height - 74f, totalWidth, 14f), $"LV {level} EXP", experienceToNextLevel <= 0f ? 0f : experience / experienceToNextLevel, new Color(0.35f, 0.9f, 0.35f));
    }

    void DrawPixicoins()
    {
        GUI.Box(new Rect(Screen.width - 170f, 16f, 150f, 32f), $"Pixicoins: {pixicoins}");
    }

    void DrawBar(Rect rect, string label, float percent, Color fillColor)
    {
        GUI.Box(rect, "");
        Color oldColor = GUI.color;
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * Mathf.Clamp01(percent), rect.height - 4f), Texture2D.whiteTexture);
        GUI.color = oldColor;
        GUI.Label(new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, rect.height), label);
    }

    void DrawHotbar()
    {
        float slotSize = 52f;
        float totalWidth = hotbar.Length * slotSize;
        float startX = (Screen.width - totalWidth) * 0.5f;
        float y = Screen.height - slotSize - 12f;

        for (int i = 0; i < hotbar.Length; i++)
        {
            Rect slot = new Rect(startX + i * slotSize, y, slotSize, slotSize);
            GUI.color = i == selectedHotbarIndex ? new Color(1f, 0.85f, 0.25f) : Color.white;
            if (GUI.Button(slot, FormatItem(hotbar[i])))
            {
                selectedHotbarIndex = i;
            }
        }

        GUI.color = Color.white;
    }

    void DrawInventory()
    {
        Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 190f, 520f, 380f);
        GUI.Box(panel, "Inventory");
        GUI.Label(new Rect(panel.x + 20f, panel.y + 28f, 240f, 24f), $"Pixicoins: {pixicoins}");
        GUI.Label(new Rect(panel.x + 300f, panel.y + 28f, 180f, 24f), $"Level {level}  EXP {experience:0}/{experienceToNextLevel:0}");
        GUI.Label(new Rect(panel.x + 300f, panel.y + 54f, 180f, 24f), $"Ranks  Sword:{swordRank} Lance:{lanceRank} Axe:{axeRank}");

        if (heldItem != WeaponType.None)
        {
            GUI.Label(new Rect(panel.x + 20f, panel.y + 54f, 220f, 24f), $"Holding: {heldItem}");
        }

        DrawInventoryGrid(panel.x + 42f, panel.y + 88f);
    }

    void DrawInventoryGrid(float startX, float startY)
    {
        float slotSize = 58f;
        for (int i = 0; i < inventory.Length; i++)
        {
            int x = i % 5;
            int y = i / 5;
            Rect slot = new Rect(startX + x * slotSize, startY + y * slotSize, slotSize, slotSize);

            if (GUI.Button(slot, FormatItem(inventory[i])))
            {
                SwapWithHeld(ref inventory[i]);
            }
        }

        for (int i = 0; i < hotbar.Length; i++)
        {
            Rect slot = new Rect(startX + 320f + i % 4 * slotSize, startY + i / 4 * slotSize, slotSize, slotSize);

            if (GUI.Button(slot, FormatItem(hotbar[i])))
            {
                SwapWithHeld(ref hotbar[i]);
            }
        }
    }

    void SwapWithHeld(ref WeaponType slot)
    {
        WeaponType oldSlot = slot;
        slot = heldItem;
        heldItem = oldSlot;
    }

    void SetInventoryOpen(bool open)
    {
        inventoryOpen = open;
        Time.timeScale = open ? 0f : 1f;
    }

    string FormatItem(WeaponType item)
    {
        return item == WeaponType.None ? "" : item.ToString();
    }

    void OnDisable()
    {
        if (inventoryOpen)
        {
            Time.timeScale = 1f;
        }
    }

    public void ApplyStartingClass(CharacterClass characterClass)
    {
        InitializeDefaultItems();

        for (int i = 0; i < hotbar.Length; i++)
        {
            hotbar[i] = WeaponType.None;
        }

        swordRank = WeaponRank.None;
        lanceRank = WeaponRank.None;
        axeRank = WeaponRank.None;

        WeaponType startingWeapon = characterClass switch
        {
            CharacterClass.Knight => WeaponType.Lance,
            CharacterClass.Brute => WeaponType.Axe,
            _ => WeaponType.Sword
        };

        hotbar[0] = startingWeapon;
        selectedHotbarIndex = 0;

        switch (startingWeapon)
        {
            case WeaponType.Lance:
                lanceRank = WeaponRank.E;
                break;
            case WeaponType.Axe:
                axeRank = WeaponRank.E;
                break;
            default:
                swordRank = WeaponRank.E;
                break;
        }
    }
}
