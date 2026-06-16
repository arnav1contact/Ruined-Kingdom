using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target = null;
    [Min(0f)]
    [SerializeField] float followSmoothTime = 0.12f;
    [SerializeField] Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] bool useBounds;
    [SerializeField] Vector2 minimumPosition = new Vector2(-100f, -100f);
    [SerializeField] Vector2 maximumPosition = new Vector2(100f, 100f);

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

        Vector3 targetPosition = ClampToBounds(target.position + offset);
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

    public void SetBounds(Vector2 minimum, Vector2 maximum)
    {
        useBounds = true;
        minimumPosition = minimum;
        maximumPosition = maximum;
    }

    public void ClearBounds()
    {
        useBounds = false;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }

    void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.position = ClampToBounds(target.position + offset);
        currentVelocity = Vector3.zero;
    }

    Vector3 ClampToBounds(Vector3 position)
    {
        if (!useBounds)
        {
            return position;
        }

        position.x = Mathf.Clamp(position.x, minimumPosition.x, maximumPosition.x);
        position.y = Mathf.Clamp(position.y, minimumPosition.y, maximumPosition.y);
        return position;
    }
}

[DisallowMultipleComponent]
public class CameraAreaBounds2D : MonoBehaviour
{
    [SerializeField] Vector2 minimumPosition = new Vector2(-10f, -7f);
    [SerializeField] Vector2 maximumPosition = new Vector2(10f, 7f);

    public Vector2 MinimumPosition => minimumPosition;
    public Vector2 MaximumPosition => maximumPosition;

    public void ApplyTo(CameraFollow2D cameraFollow)
    {
        if (cameraFollow != null)
        {
            cameraFollow.SetBounds(minimumPosition, maximumPosition);
        }
    }
}
