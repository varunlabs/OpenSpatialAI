using UnityEngine;

// Layer 1 sensor script — detects spatial context only, no context inference logic
public class SpatialContextDetector : MonoBehaviour
{
    [Header("Tracking Reference")]
    [SerializeField] private OVRCameraRig ovrCameraRig;

    [Header("Posture Calibration")]
    [SerializeField] private float defaultStandingHeightM = 1.6f;
    [SerializeField] private float sittingDeltaThresholdM = 0.25f;

    public string posture_mode { get; private set; }
    public string boundary_type { get; private set; }

    private float calibratedStandingHeightM;

    private void Awake()
    {
        posture_mode = "standing";
        boundary_type = "custom";
        calibratedStandingHeightM = defaultStandingHeightM;
    }

    private void Start()
    {
        // Standing height calibrated at session start.
        CalibrateStandingHeight();
        UpdateSpatialContext();
        InvokeRepeating(nameof(UpdateSpatialContext), 5f, 5f);
    }

    private void CalibrateStandingHeight()
    {
        float currentHeadY = GetHeadsetHeightY();
        if (currentHeadY > 0f)
        {
            calibratedStandingHeightM = currentHeadY;
        }
        else
        {
            calibratedStandingHeightM = defaultStandingHeightM;
        }
    }

    private void UpdateSpatialContext()
    {
        UpdateBoundaryType();
        UpdatePostureMode();
    }

    private void UpdateBoundaryType()
    {
        OVRBoundary boundary = OVRManager.boundary;
        if (boundary == null || !boundary.GetConfigured())
        {
            // No guardian boundary -> passthrough mode.
            boundary_type = "passthrough";
            return;
        }

        Vector3[] points = boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
        float area = ComputeBoundaryArea(points);

        if (area < 1f)
        {
            boundary_type = "stationary";
        }
        else if (area > 2f)
        {
            boundary_type = "room_scale";
        }
        else
        {
            boundary_type = "custom";
        }
    }

    private void UpdatePostureMode()
    {
        // Room-scale boundary implies user can move freely -> override posture mode.
        if (boundary_type == "room_scale")
        {
            posture_mode = "room_scale";
            return;
        }

        float headY = GetHeadsetHeightY();
        if (headY <= 0f || calibratedStandingHeightM <= 0f)
        {
            posture_mode = "standing";
            return;
        }

        float delta = calibratedStandingHeightM - headY;
        posture_mode = delta >= sittingDeltaThresholdM ? "sitting" : "standing";
    }

    private float GetHeadsetHeightY()
    {
        Transform anchor = ResolveHeadAnchor();
        if (anchor == null)
        {
            return -1f;
        }

        float y = anchor.position.y;
        if (float.IsNaN(y) || float.IsInfinity(y))
        {
            return -1f;
        }

        return y;
    }

    private Transform ResolveHeadAnchor()
    {
        if (ovrCameraRig == null)
        {
            return null;
        }

        if (ovrCameraRig.centerEyeAnchor != null)
        {
            return ovrCameraRig.centerEyeAnchor;
        }

        return ovrCameraRig.transform;
    }

    private static float ComputeBoundaryArea(Vector3[] points)
    {
        if (points == null || points.Length < 3)
        {
            return 0f;
        }

        float area = 0f;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[(i + 1) % points.Length];

            if (!IsFinite(a) || !IsFinite(b))
            {
                continue;
            }

            area += (a.x * b.z) - (b.x * a.z);
        }

        return Mathf.Abs(area) * 0.5f;
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
