using UnityEngine;

public class GazeCapture : MonoBehaviour
{
    [Header("Gaze Source")]
    [SerializeField] private Transform centerEyeAnchor;

    [Header("Raycast")]
    [SerializeField] private float maxRayDistance = 50f;

    [Header("Fixation Placeholder")]
    [SerializeField] private float fixationAngleThresholdDeg = 1.5f;

    public Vector3 gaze_origin { get; private set; }
    public Vector3 gaze_direction { get; private set; }
    public bool is_fixation { get; private set; }
    public float fixation_duration_s { get; private set; }
    public string aoi_hit { get; private set; }

    private Vector3 previousDirection;
    private bool hasPreviousDirection;
    private GameObject[] cachedAoiObjects;

    private void Awake()
    {
        CacheAoiObjects();
    }

    private void OnEnable()
    {
        ResetOutputs();
    }

    // Use Update to follow headset eye-tracking refresh (~60-90Hz) rather than fixed physics ticks.
    private void Update()
    {
        Transform gazeSource = centerEyeAnchor != null ? centerEyeAnchor : transform;

        Vector3 origin = gazeSource.position;
        Vector3 direction = gazeSource.forward;

        if (!IsFinite(origin))
        {
            origin = transform.position;
        }

        if (!IsFinite(direction) || direction.sqrMagnitude < 1e-8f)
        {
            direction = transform.forward;
        }

        direction = SafeNormalize(direction);
        if (direction == Vector3.zero)
        {
            direction = Vector3.forward;
        }

        gaze_origin = origin;
        gaze_direction = direction;

        Debug.DrawRay(origin, direction * maxRayDistance, Color.yellow);

        UpdateFixation(direction);
        UpdateAoiHit(origin, direction);
    }

    private void UpdateAoiHit(Vector3 origin, Vector3 direction)
    {
        aoi_hit = "none";

        // Red = no gaze
        SetAllAoiRed();

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance))
        {
            Debug.Log("Hit: " + hit.collider.name);

            if (hit.collider.CompareTag("AOI"))
            {
                aoi_hit = hit.collider.gameObject.name;

                // Green = gaze detected
                SetRendererColor(hit.collider.gameObject, Color.green);
            }
            else
            {
                Debug.Log("Hit but not AOI: " + hit.collider.tag);
            }
        }
    }

    private void UpdateFixation(Vector3 currentDirection)
    {
        // Placeholder - will be replaced by I-VT algorithm in Layer 2.
        if (!hasPreviousDirection)
        {
            previousDirection = currentDirection;
            hasPreviousDirection = true;
            is_fixation = false;
            fixation_duration_s = 0f;
            return;
        }

        float angleDelta = Vector3.Angle(previousDirection, currentDirection);
        bool stable = angleDelta <= fixationAngleThresholdDeg;

        if (stable)
        {
            fixation_duration_s += Time.deltaTime;
            is_fixation = true;
        }
        else
        {
            fixation_duration_s = 0f;
            is_fixation = false;
        }

        previousDirection = currentDirection;
    }

    private void SetAllAoiRed()
    {
        EnsureAoiCache();

        for (int i = 0; i < cachedAoiObjects.Length; i++)
        {
            SetRendererColor(cachedAoiObjects[i], Color.red);
        }
    }

    private void CacheAoiObjects()
    {
        cachedAoiObjects = GameObject.FindGameObjectsWithTag("AOI");
    }

    private void EnsureAoiCache()
    {
        if (cachedAoiObjects == null || cachedAoiObjects.Length == 0)
        {
            CacheAoiObjects();
            return;
        }

        for (int i = 0; i < cachedAoiObjects.Length; i++)
        {
            if (cachedAoiObjects[i] == null)
            {
                CacheAoiObjects();
                return;
            }
        }
    }

    private void SetRendererColor(GameObject target, Color color)
    {
        if (target == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        // Uses renderer.material intentionally for per-object debug color (instanced material).
        renderer.material.color = color;
    }

    private void ResetOutputs()
    {
        gaze_origin = Vector3.zero;
        gaze_direction = Vector3.zero;
        is_fixation = false;
        fixation_duration_s = 0f;
        aoi_hit = "none";
        hasPreviousDirection = false;
    }

    private static Vector3 SafeNormalize(Vector3 v)
    {
        if (!IsFinite(v))
        {
            return Vector3.zero;
        }

        float sqrMag = v.sqrMagnitude;
        if (sqrMag < 1e-8f)
        {
            return Vector3.zero;
        }

        return v / Mathf.Sqrt(sqrMag);
    }

    private static bool IsFinite(Vector3 v)
    {
        return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
