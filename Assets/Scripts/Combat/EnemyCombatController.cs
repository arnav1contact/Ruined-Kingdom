using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyCombatController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target = null;
    [SerializeField] HealthComponent targetHealth = null;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 2.25f;
    [SerializeField] float detectionRange = 6f;
    [SerializeField] float stopDistance = 1.1f;

    [Header("Attack")]
    [SerializeField] float damage = 10f;
    [SerializeField] float attackCooldown = 0.8f;
    [SerializeField] Transform weaponPivot = null;
    [SerializeField] Transform weaponBlade = null;
    [SerializeField] SpriteRenderer weaponRenderer = null;

    [Header("Read Only Debug Info")]
    [SerializeField] bool targetInRange;
    [SerializeField] bool canAttackTarget;

    Rigidbody2D rb;
    float nextAttackTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ApplyTopDownRigidbodyDefaults();
    }

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        ApplyTopDownRigidbodyDefaults();
    }

    void FixedUpdate()
    {
        if (target == null || rb == null)
        {
            targetInRange = false;
            canAttackTarget = false;
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;
        targetInRange = distance <= detectionRange;
        canAttackTarget = distance <= stopDistance;

        if (targetInRange && !canAttackTarget)
        {
            Vector2 movement = toTarget.normalized * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
        }
    }

    void Update()
    {
        UpdateWeaponPose();

        if (canAttackTarget && Time.time >= nextAttackTime)
        {
            AttackTarget();
        }
    }

    void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        detectionRange = Mathf.Max(0f, detectionRange);
        stopDistance = Mathf.Max(0f, stopDistance);
        damage = Mathf.Max(0f, damage);
        attackCooldown = Mathf.Max(0f, attackCooldown);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetHealth = newTarget == null ? null : newTarget.GetComponent<HealthComponent>();
    }

    void AttackTarget()
    {
        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(EnemySwingRoutine());

        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
        }
    }

    System.Collections.IEnumerator EnemySwingRoutine()
    {
        if (weaponBlade != null)
        {
            weaponBlade.localScale = new Vector3(0.85f, 0.16f, 1f);
        }

        if (weaponRenderer != null)
        {
            weaponRenderer.color = new Color(1f, 0.35f, 0.25f);
        }

        yield return new WaitForSeconds(0.12f);

        if (weaponBlade != null)
        {
            weaponBlade.localScale = new Vector3(0.65f, 0.14f, 1f);
        }

        if (weaponRenderer != null)
        {
            weaponRenderer.color = new Color(0.95f, 0.7f, 0.6f);
        }
    }

    void UpdateWeaponPose()
    {
        if (weaponPivot == null || target == null)
        {
            return;
        }

        Vector2 direction = target.position - transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.down;
        }

        direction.Normalize();
        weaponPivot.localPosition = direction * 0.46f;
        weaponPivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
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
