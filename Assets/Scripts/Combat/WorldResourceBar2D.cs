using UnityEngine;

[DisallowMultipleComponent]
public class WorldResourceBar2D : MonoBehaviour
{
    public enum ResourceType
    {
        Health,
        Stamina
    }

    [SerializeField] HealthComponent healthSource = null;
    [SerializeField] StaminaComponent staminaSource = null;
    [SerializeField] ResourceType resourceType = ResourceType.Health;
    [SerializeField] Transform fill = null;

    void LateUpdate()
    {
        if (fill == null)
        {
            return;
        }

        float percent = GetResourcePercent();
        fill.localScale = new Vector3(percent, 1f, 1f);
    }

    float GetResourcePercent()
    {
        return resourceType == ResourceType.Health
            ? healthSource == null ? 0f : healthSource.HealthPercent
            : staminaSource == null ? 0f : staminaSource.StaminaPercent;
    }
}
