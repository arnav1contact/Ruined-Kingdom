using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target = null;
    [Min(0f)]
    [SerializeField] float followSmoothTime = 0.12f;
    [SerializeField] Vector3 offset = new Vector3(0f, 0f, -10f);

    Vector3 currentVelocity;

    void Start()
    {
        SnapToTarget();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            followSmoothTime);
    }

    void OnValidate()
    {
        followSmoothTime = Mathf.Max(0f, followSmoothTime);
    }

    void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.position + offset;
        currentVelocity = Vector3.zero;
    }
}
