using System;
using UnityEngine;

public class SignalSynchroniser : MonoBehaviour
{
    [Header("Layer 1 Inputs")]
    [SerializeField] private GazeCapture gazeCapture;
    [SerializeField] private BodyPoseCapture bodyPoseCapture;
    [SerializeField] private HandCapture handCapture;
    [SerializeField] private SpatialContextDetector spatialContextDetector;
    [SerializeField] private long staleSignalThresholdMs = 200L;

    private SignalFrame latestFrame;
    private long lastGazeFreshTimestampMs = -1L;
    private long lastBodyFreshTimestampMs = -1L;
    private long lastHandFreshTimestampMs = -1L;
    private long lastSpatialFreshTimestampMs = -1L;
    private Vector3 previousHeadPosition;
    private bool hasPreviousHeadPosition;

    private void Awake()
    {
        AutoResolveInputs();
        latestFrame = CreateDefaultFrame();
    }

    private void Start()
    {
        AutoResolveInputs();
        SampleAndSynchronise();
        InvokeRepeating(nameof(SampleAndSynchronise), 0.1f, 0.1f);
    }

    public SignalFrame GetLatestFrame()
    {
        return latestFrame;
    }

    private void SampleAndSynchronise()
    {
        AutoResolveInputs();
        SignalFrame frame = latestFrame;

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        frame.timestamp_ms = nowMs;

        bool gazeAvailable = IsSourceAvailable(gazeCapture);
        bool bodyAvailable = IsSourceAvailable(bodyPoseCapture);
        bool handAvailable = IsSourceAvailable(handCapture);
        bool spatialAvailable = IsSourceAvailable(spatialContextDetector);

        if (gazeAvailable)
        {
            frame.gaze_origin = ChooseVector(gazeCapture.gaze_origin, frame.gaze_origin);
            frame.gaze_direction = ChooseDirection(gazeCapture.gaze_direction, frame.gaze_direction);
            frame.is_fixation = gazeCapture.is_fixation;
            frame.fixation_duration_s = IsFinite(gazeCapture.fixation_duration_s) ? gazeCapture.fixation_duration_s : frame.fixation_duration_s;
            frame.aoi_hit = ChooseString(gazeCapture.aoi_hit, frame.aoi_hit);
            lastGazeFreshTimestampMs = nowMs;
        }

        if (bodyPoseCapture != null && IsUsableDirection(frame.gaze_direction))
        {
            bodyPoseCapture.SetGazeDirection(frame.gaze_direction);
        }

        if (bodyAvailable)
        {
            frame.head_position = ChooseVector(bodyPoseCapture.head_position, frame.head_position);
            frame.head_forward = ChooseDirection(bodyPoseCapture.head_forward, frame.head_forward);
            frame.head_gaze_divergence_deg = IsFinite(bodyPoseCapture.head_gaze_divergence_deg)
                ? bodyPoseCapture.head_gaze_divergence_deg
                : frame.head_gaze_divergence_deg;

            frame.posture_class = ChooseString(bodyPoseCapture.posture_class, frame.posture_class);
            frame.spine_angle_deg = IsFinite(bodyPoseCapture.spine_angle_deg) ? bodyPoseCapture.spine_angle_deg : frame.spine_angle_deg;
            frame.avg_joint_velocity = IsFinite(bodyPoseCapture.avg_joint_velocity) ? bodyPoseCapture.avg_joint_velocity : frame.avg_joint_velocity;
            lastBodyFreshTimestampMs = nowMs;
        }

        if (IsUsableDirection(frame.head_forward) && IsUsableDirection(frame.gaze_direction))
        {
            frame.head_gaze_divergence_deg = Vector3.Angle(frame.head_forward.normalized, frame.gaze_direction.normalized);
        }

        frame.locomotion_delta_m = CalculateLocomotionDelta(frame.head_position);

        if (handAvailable)
        {
            frame.left_pinch = handCapture.left_pinch;
            frame.right_pinch = handCapture.right_pinch;
            frame.interaction_count_10s = handCapture.interaction_count_10s >= 0 ? handCapture.interaction_count_10s : frame.interaction_count_10s;
            frame.nearest_object_dist_m = IsFinite(handCapture.nearest_object_dist_m)
                ? handCapture.nearest_object_dist_m
                : frame.nearest_object_dist_m;
            lastHandFreshTimestampMs = nowMs;
        }

        if (spatialAvailable)
        {
            frame.posture_mode = ChooseString(spatialContextDetector.posture_mode, frame.posture_mode);
            frame.boundary_type = ChooseString(spatialContextDetector.boundary_type, frame.boundary_type);
            lastSpatialFreshTimestampMs = nowMs;
        }

        if (IsUnknownPosture(frame.posture_class) && !IsUnknownPosture(frame.posture_mode))
        {
            frame.posture_class = frame.posture_mode;
        }

        frame.gaze_stale = IsSignalStale(lastGazeFreshTimestampMs, nowMs);
        frame.body_stale = IsSignalStale(lastBodyFreshTimestampMs, nowMs);
        frame.hand_stale = IsSignalStale(lastHandFreshTimestampMs, nowMs);
        frame.spatial_stale = IsSignalStale(lastSpatialFreshTimestampMs, nowMs);

        frame.context_state = ChooseString(frame.context_state, "unknown");
        frame.context_confidence = IsFinite(frame.context_confidence) ? frame.context_confidence : 0f;

        latestFrame = frame;
    }

    private void AutoResolveInputs()
    {
        if (gazeCapture == null)
        {
            gazeCapture = FindAnyObjectByType<GazeCapture>(FindObjectsInactive.Include);
        }

        if (bodyPoseCapture == null)
        {
            bodyPoseCapture = FindAnyObjectByType<BodyPoseCapture>(FindObjectsInactive.Include);
        }

        if (handCapture == null)
        {
            handCapture = FindAnyObjectByType<HandCapture>(FindObjectsInactive.Include);
        }

        if (spatialContextDetector == null)
        {
            spatialContextDetector = FindAnyObjectByType<SpatialContextDetector>(FindObjectsInactive.Include);
        }
    }

    private static SignalFrame CreateDefaultFrame()
    {
        return new SignalFrame
        {
            timestamp_ms = 0L,
            gaze_origin = Vector3.zero,
            gaze_direction = Vector3.forward,
            is_fixation = false,
            fixation_duration_s = 0f,
            aoi_hit = "none",
            head_position = Vector3.zero,
            head_forward = Vector3.forward,
            head_gaze_divergence_deg = 0f,
            posture_class = "unknown",
            spine_angle_deg = 0f,
            avg_joint_velocity = 0f,
            left_pinch = false,
            right_pinch = false,
            interaction_count_10s = 0,
            nearest_object_dist_m = 0f,
            posture_mode = "standing",
            boundary_type = "stationary",
            locomotion_delta_m = 0f,
            gaze_stale = true,
            body_stale = true,
            hand_stale = true,
            spatial_stale = true,
            context_state = "unknown",
            context_confidence = 0f
        };
    }

    private float CalculateLocomotionDelta(Vector3 currentHeadPosition)
    {
        if (!IsUsablePosition(currentHeadPosition))
        {
            hasPreviousHeadPosition = false;
            return 0f;
        }

        float delta = hasPreviousHeadPosition
            ? Vector3.Distance(previousHeadPosition, currentHeadPosition)
            : 0f;

        previousHeadPosition = currentHeadPosition;
        hasPreviousHeadPosition = true;
        return delta;
    }

    private static Vector3 ChooseVector(Vector3 candidate, Vector3 fallback)
    {
        return IsFinite(candidate) ? candidate : fallback;
    }

    private static Vector3 ChooseDirection(Vector3 candidate, Vector3 fallback)
    {
        if (!IsFinite(candidate) || candidate.sqrMagnitude < 1e-8f)
        {
            return fallback;
        }

        return candidate.normalized;
    }

    private static string ChooseString(string candidate, string fallback)
    {
        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
    }

    private static bool IsSourceAvailable(Behaviour source)
    {
        return source != null && source.isActiveAndEnabled && source.gameObject.activeInHierarchy;
    }

    private static bool IsUnknownPosture(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Equals("unknown", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSignalStale(long lastFreshTimestampMs, long nowMs)
    {
        return lastFreshTimestampMs < 0L || nowMs - lastFreshTimestampMs > staleSignalThresholdMs;
    }

    private static bool IsUsableDirection(Vector3 v)
    {
        return IsFinite(v) && v.sqrMagnitude > 1e-8f;
    }

    private static bool IsUsablePosition(Vector3 v)
    {
        return IsFinite(v) && v.sqrMagnitude > 1e-8f;
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
