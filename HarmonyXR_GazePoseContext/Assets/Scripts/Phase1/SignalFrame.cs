using System;
using UnityEngine;

[Serializable]
public struct SignalFrame
{
    public long timestamp_ms; // Unix timestamp in milliseconds

    public Vector3 gaze_origin; // Gaze ray origin in world space
    public Vector3 gaze_direction; // Gaze ray direction in world space
    public bool is_fixation; // True when gaze is currently fixated
    public float fixation_duration_s; // Ongoing fixation duration in seconds
    public string aoi_hit; // Area-of-interest identifier hit by gaze

    public Vector3 head_position; // Head position in world space
    public Vector3 head_forward; // Head forward vector in world space
    public float head_gaze_divergence_deg; // Angle between head forward and gaze direction

    public string posture_class; // Classified body posture label
    public float spine_angle_deg; // Spine bend angle in degrees
    public float avg_joint_velocity; // Average velocity across tracked joints

    public bool left_pinch; // Left hand pinch state
    public bool right_pinch; // Right hand pinch state
    public int interaction_count_10s; // Interaction count within last 10 seconds
    public float nearest_object_dist_m; // Distance to nearest interactable object in meters

    public string posture_mode; // Spatially inferred posture mode
    public string boundary_type; // Current boundary/environment type
    public float locomotion_delta_m; // Displacement over current sample window in meters

    public string context_state; // Derived context state label
    public float context_confidence; // Confidence score for context_state
}