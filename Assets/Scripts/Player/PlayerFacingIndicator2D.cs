using UnityEngine;

[DisallowMultipleComponent]
public class PlayerFacingIndicator2D : MonoBehaviour
{
    [SerializeField] PlayerMovementController movementController = null;
    [SerializeField] Transform indicator = null;
    [SerializeField] float distanceFromPlayer = 0.65f;

    void Reset()
    {
        movementController = GetComponentInParent<PlayerMovementController>();
        indicator = transform;
    }

    void LateUpdate()
    {
        if (movementController == null || indicator == null)
        {
            return;
        }

        Vector2 facingDirection = movementController.LastNonZeroFacingDirection;
        if (facingDirection.sqrMagnitude <= 0.0001f)
        {
            facingDirection = Vector2.down;
        }

        indicator.localPosition = facingDirection.normalized * distanceFromPlayer;
    }
}
