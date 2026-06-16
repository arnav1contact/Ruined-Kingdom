using UnityEngine;

[DisallowMultipleComponent]
public class StaminaComponent : MonoBehaviour
{
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float currentStamina = 100f;
    [SerializeField] float regenerationPerSecond = 25f;
    [SerializeField] float regenerationDelayAfterSpend = 0.4f;

    float lastSpendTime;

    public float MaxStamina => maxStamina;
    public float CurrentStamina => currentStamina;
    public float StaminaPercent => maxStamina <= 0f ? 0f : currentStamina / maxStamina;

    void Awake()
    {
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    void Update()
    {
        if (Time.time < lastSpendTime + regenerationDelayAfterSpend)
        {
            return;
        }

        if (regenerationPerSecond > 0f && currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + regenerationPerSecond * Time.deltaTime);
        }
    }

    void OnValidate()
    {
        maxStamina = Mathf.Max(1f, maxStamina);
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
        regenerationDelayAfterSpend = Mathf.Max(0f, regenerationDelayAfterSpend);
    }

    public bool TrySpend(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (currentStamina < amount)
        {
            return false;
        }

        currentStamina -= amount;
        lastSpendTime = Time.time;
        return true;
    }

    public void Refill()
    {
        currentStamina = maxStamina;
    }

    public void IncreaseMaxStamina(float amount, bool refillAddedStamina)
    {
        if (amount <= 0f)
        {
            return;
        }

        maxStamina += amount;
        if (refillAddedStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        }
    }
}
