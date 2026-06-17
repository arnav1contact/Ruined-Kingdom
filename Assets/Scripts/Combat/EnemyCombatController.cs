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
            if (targetHealth.TakeDamage(damage))
            {
                Rigidbody2D targetBody = targetHealth.GetComponent<Rigidbody2D>();
                if (targetBody != null)
                {
                    Vector2 direction = targetBody.position - rb.position;
                    if (direction.sqrMagnitude <= 0.0001f)
                    {
                        direction = Vector2.down;
                    }

                    targetBody.MovePosition(targetBody.position + direction.normalized * 0.28f);
                }
            }
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

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
}

[DisallowMultipleComponent]
public class EnemyRewardComponent : MonoBehaviour
{
    [SerializeField] HealthComponent health = null;
    [SerializeField] string routeName = "Training";
    [SerializeField] string archetypeName = "Skirmisher";
    [SerializeField] int experienceReward = 12;
    [SerializeField] int pixicoinReward = 8;
    [SerializeField] string materialReward = "Training Scrap";
    [SerializeField] int materialCount = 1;

    bool claimed;

    void Awake()
    {
        if (health == null)
        {
            health = GetComponent<HealthComponent>();
        }
    }

    void OnEnable()
    {
        claimed = false;
        if (health != null)
        {
            health.Emptied += OnEmptied;
        }

        RouteObjectiveManager.RegisterEnemy(routeName, this);
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.Emptied -= OnEmptied;
        }
    }

    void OnEmptied(HealthComponent source)
    {
        if (claimed)
        {
            return;
        }

        claimed = true;
        PlayerInventoryHudController inventory = FindFirstObjectByType<PlayerInventoryHudController>();
        if (inventory != null)
        {
            inventory.AddExperience(experienceReward);
            inventory.AddPixicoins(pixicoinReward);
            inventory.AddMaterial(materialReward, materialCount);
        }

        RouteObjectiveManager.ReportEnemyDefeated(routeName, this);
        ToastHudController.Show($"{archetypeName} defeated");
    }
}

public static class RouteObjectiveManager
{
    static readonly System.Collections.Generic.Dictionary<string, RouteProgress> routes = new System.Collections.Generic.Dictionary<string, RouteProgress>();

    public static void RegisterEnemy(string routeName, EnemyRewardComponent enemy)
    {
        if (string.IsNullOrWhiteSpace(routeName) || enemy == null)
        {
            return;
        }

        RouteProgress route = GetOrCreate(routeName);
        route.TotalEnemies++;
    }

    public static void ReportEnemyDefeated(string routeName, EnemyRewardComponent enemy)
    {
        if (string.IsNullOrWhiteSpace(routeName))
        {
            return;
        }

        RouteProgress route = GetOrCreate(routeName);
        route.DefeatedEnemies = Mathf.Min(route.TotalEnemies, route.DefeatedEnemies + 1);

        if (!route.Completed && route.TotalEnemies > 0 && route.DefeatedEnemies >= route.TotalEnemies)
        {
            route.Completed = true;
            route.RewardClaimed = false;
            ToastHudController.Show($"{routeName} route clear");
        }
    }

    public static void MarkRouteComplete(string routeName)
    {
        if (string.IsNullOrWhiteSpace(routeName))
        {
            return;
        }

        RouteProgress route = GetOrCreate(routeName);
        if (route.Completed)
        {
            return;
        }

        route.Completed = true;
        route.RewardClaimed = false;
        route.TotalEnemies = Mathf.Max(route.TotalEnemies, route.DefeatedEnemies);
        ToastHudController.Show($"{routeName} route clear");
    }

    public static bool IsRouteComplete(string routeName)
    {
        return routes.TryGetValue(routeName, out RouteProgress route) && route.Completed;
    }

    public static bool TryClaimRouteReward(string routeName, PlayerInventoryHudController inventory)
    {
        if (inventory == null || !routes.TryGetValue(routeName, out RouteProgress route) || !route.Completed || route.RewardClaimed)
        {
            return false;
        }

        route.RewardClaimed = true;
        inventory.AddExperience(25f);
        inventory.AddPixicoins(40);
        inventory.AddMaterial($"{routeName} Route Badge", 1);
        ToastHudController.Show($"{routeName} reward claimed");
        return true;
    }

    public static string GetQuestBoardText()
    {
        if (routes.Count == 0)
        {
            return "Forest: enter the Lost Woods and find the correct path to clear the first route.";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        foreach (System.Collections.Generic.KeyValuePair<string, RouteProgress> pair in routes)
        {
            RouteProgress route = pair.Value;
            string status = route.Completed ? route.RewardClaimed ? "Reward claimed" : "Clear - claim chest" : $"{route.DefeatedEnemies}/{route.TotalEnemies} defeated";
            builder.AppendLine($"{pair.Key}: {status}");
        }

        return builder.ToString();
    }

    static RouteProgress GetOrCreate(string routeName)
    {
        if (!routes.TryGetValue(routeName, out RouteProgress route))
        {
            route = new RouteProgress();
            routes[routeName] = route;
        }

        return route;
    }

    sealed class RouteProgress
    {
        public int TotalEnemies;
        public int DefeatedEnemies;
        public bool Completed;
        public bool RewardClaimed;
    }
}
