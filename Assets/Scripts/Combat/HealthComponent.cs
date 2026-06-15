using UnityEngine;

[DisallowMultipleComponent]
public class HealthComponent : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float currentHealth = 100f;
    [SerializeField] float regenerationPerSecond = 0f;
    [SerializeField] bool destroyWhenEmpty = false;
    [SerializeField] bool isInvulnerable;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
    public bool IsEmpty => currentHealth <= 0f;
    public bool IsInvulnerable => isInvulnerable;

    void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    void Update()
    {
        if (regenerationPerSecond > 0f && currentHealth > 0f && currentHealth < maxHealth)
        {
            Heal(regenerationPerSecond * Time.deltaTime);
        }
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f || isInvulnerable)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (currentHealth <= 0f && destroyWhenEmpty)
        {
            gameObject.SetActive(false);
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void Refill()
    {
        currentHealth = maxHealth;
    }

    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
    }
}
