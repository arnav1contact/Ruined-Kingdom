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

    Rigidbody2D rb;
    Vector3 originalScale;
    float nextAttackTime;
    float nextDodgeTime;
    float nextJumpTime;
    float chargeStartedTime;

    public bool IsActionBusy => actionBusy;
    public bool IsCharging => isCharging;
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
            TryStartSwordAttack("Strong Attack", strongDamage, strongStaminaCost, strongCooldown, 0.24f, strongHitboxSize, Color.red);
        }
        else if (WasLightAttackPressed())
        {
            TryStartSwordAttack("Light Attack", lightDamage, lightStaminaCost, lightCooldown, 0.14f, lightHitboxSize, Color.white);
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

    void TryStartSwordAttack(string actionName, float damage, float staminaCost, float cooldown, float windup, Vector2 hitboxSize, Color weaponColor)
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (stamina != null && !stamina.TrySpend(staminaCost))
        {
            return;
        }

        nextAttackTime = Time.time + cooldown;
        StartCoroutine(SwordAttackRoutine(actionName, damage, windup, hitboxSize, weaponColor));
    }

    IEnumerator SwordAttackRoutine(string actionName, float damage, float windup, Vector2 hitboxSize, Color weaponColor)
    {
        actionBusy = true;
        currentAction = actionName;
        movementController?.SetMovementSuppressed(true);
        SetWeaponColor(weaponColor);
        SetWeaponReach(1.25f);

        yield return new WaitForSeconds(windup);

        HitWithSword(damage, hitboxSize);

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

    void HitWithSword(float damage, Vector2 hitboxSize)
    {
        Vector2 direction = GetFacingDirection();
        Vector2 center = (Vector2)transform.position + direction * hitboxForwardOffset;
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
