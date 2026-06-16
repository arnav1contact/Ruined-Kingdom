using UnityEngine;
using System;

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

    public event Action<HealthComponent, float> Damaged;
    public event Action<HealthComponent, float> Healed;
    public event Action<HealthComponent> Emptied;

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

    public bool TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f || isInvulnerable)
        {
            return false;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Damaged?.Invoke(this, amount);

        if (currentHealth <= 0f && destroyWhenEmpty)
        {
            Emptied?.Invoke(this);
            gameObject.SetActive(false);
        }

        return true;
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Healed?.Invoke(this, amount);
    }

    public void Refill()
    {
        currentHealth = maxHealth;
        Healed?.Invoke(this, maxHealth);
    }

    public void IncreaseMaxHealth(float amount, bool refillAddedHealth)
    {
        if (amount <= 0f)
        {
            return;
        }

        maxHealth += amount;
        if (refillAddedHealth)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
    }

    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
    }
}

[DisallowMultipleComponent]
public class HitFlashOnDamage : MonoBehaviour
{
    [SerializeField] HealthComponent health = null;
    [SerializeField] SpriteRenderer[] renderers = null;
    [SerializeField] Color flashColor = Color.white;
    [SerializeField] float flashSeconds = 0.08f;

    Color[] originalColors;
    Coroutine flashRoutine;

    void Awake()
    {
        if (health == null)
        {
            health = GetComponentInParent<HealthComponent>();
        }

        RefreshRenderers();
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
        }
    }

    public void RefreshRenderers()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i] == null ? Color.white : renderers[i].color;
        }
    }

    void OnDamaged(HealthComponent source, float amount)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    System.Collections.IEnumerator FlashRoutine()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = flashColor;
            }
        }

        yield return new WaitForSeconds(flashSeconds);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalColors != null && i < originalColors.Length)
            {
                renderers[i].color = originalColors[i];
            }
        }

        flashRoutine = null;
    }
}

[DisallowMultipleComponent]
public class DamageNumberEmitter : MonoBehaviour
{
    [SerializeField] HealthComponent health = null;
    [SerializeField] Color damageColor = new Color(1f, 0.88f, 0.32f);

    void Awake()
    {
        if (health == null)
        {
            health = GetComponentInParent<HealthComponent>();
        }
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
        }
    }

    void OnDamaged(HealthComponent source, float amount)
    {
        FloatingCombatTextHud.Show(transform.position + Vector3.up * 0.75f, Mathf.RoundToInt(amount).ToString(), damageColor);
    }
}
