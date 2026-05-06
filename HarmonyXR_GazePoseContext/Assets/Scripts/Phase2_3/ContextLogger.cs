using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ContextLogger
{
    [Serializable]
    private struct ContextLogEntry
    {
        // Full SignalFrame payload
        public long timestamp_ms;
        public Vector3 gaze_origin;
        public Vector3 gaze_direction;
        public bool is_fixation;
        public float fixation_duration_s;
        public string aoi_hit;
        public Vector3 head_position;
        public Vector3 head_forward;
        public float head_gaze_divergence_deg;
        public string posture_class;
        public float spine_angle_deg;
        public float avg_joint_velocity;
        public bool left_pinch;
        public bool right_pinch;
        public int interaction_count_10s;
        public float nearest_object_dist_m;
        public string posture_mode;
        public string boundary_type;
        public float locomotion_delta_m;

        // Context output
        public string context_state;
        public float context_confidence;

        // Intent seed fields
        public string prev_context_state;
        public int state_hold_duration_ms;
        public float state_transition_count;
        public string nearest_interactable;
    }

    private readonly string logPath;

    private ContextState previousState;
    private bool hasPreviousState;
    private long currentStateStartTimestampMs;
    private readonly Queue<long> transitionTimestampsMs = new Queue<long>();

    public ContextLogger()
    {
        string filename = "context_log_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".jsonl";
        logPath = Path.Combine(Application.persistentDataPath, filename);
    }

    public void Log(SignalFrame frame, ContextResult result)
    {
        long timestamp = frame.timestamp_ms;
        string currentState = result.state.ToString();

        if (!hasPreviousState)
        {
            previousState = result.state;
            hasPreviousState = true;
            currentStateStartTimestampMs = timestamp;
        }

        string prevStateString = previousState.ToString();

        if (result.state != previousState)
        {
            transitionTimestampsMs.Enqueue(timestamp);
            prevStateString = previousState.ToString();
            previousState = result.state;
            currentStateStartTimestampMs = timestamp;
        }

        long windowStart = timestamp - 60000L;
        while (transitionTimestampsMs.Count > 0 && transitionTimestampsMs.Peek() < windowStart)
        {
            transitionTimestampsMs.Dequeue();
        }

        int holdDurationMs = 0;
        long holdDelta = timestamp - currentStateStartTimestampMs;
        if (holdDelta > 0L)
        {
            holdDurationMs = holdDelta > int.MaxValue ? int.MaxValue : (int)holdDelta;
        }

        ContextLogEntry entry = new ContextLogEntry
        {
            timestamp_ms = frame.timestamp_ms,
            gaze_origin = frame.gaze_origin,
            gaze_direction = frame.gaze_direction,
            is_fixation = frame.is_fixation,
            fixation_duration_s = frame.fixation_duration_s,
            aoi_hit = frame.aoi_hit,
            head_position = frame.head_position,
            head_forward = frame.head_forward,
            head_gaze_divergence_deg = frame.head_gaze_divergence_deg,
            posture_class = frame.posture_class,
            spine_angle_deg = frame.spine_angle_deg,
            avg_joint_velocity = frame.avg_joint_velocity,
            left_pinch = frame.left_pinch,
            right_pinch = frame.right_pinch,
            interaction_count_10s = frame.interaction_count_10s,
            nearest_object_dist_m = frame.nearest_object_dist_m,
            posture_mode = frame.posture_mode,
            boundary_type = frame.boundary_type,
            locomotion_delta_m = frame.locomotion_delta_m,
            context_state = currentState,
            context_confidence = result.confidence,
            prev_context_state = prevStateString,
            state_hold_duration_ms = holdDurationMs,
            state_transition_count = transitionTimestampsMs.Count,
            nearest_interactable = string.IsNullOrWhiteSpace(frame.aoi_hit) ? "none" : frame.aoi_hit
        };

        string line = JsonUtility.ToJson(entry) + Environment.NewLine;
        File.AppendAllText(logPath, line);
    }
}
