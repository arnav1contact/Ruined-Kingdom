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
