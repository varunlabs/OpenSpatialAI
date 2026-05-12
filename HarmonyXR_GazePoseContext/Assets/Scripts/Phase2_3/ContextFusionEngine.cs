using System.Collections.Generic;
using UnityEngine;

public class ContextRule
{
    public System.Func<GazeFeatureVector, BodyFeatureVector, HandFeatureVector, SpatialContextVector, bool> condition;
    public ContextState targetState;

    public ContextRule(System.Func<GazeFeatureVector, BodyFeatureVector, HandFeatureVector, SpatialContextVector, bool> cond, ContextState state)
    {
        condition = cond;
        targetState = state;
    }
}

public class ContextFusionEngine
{
    private readonly List<ContextRule> rules;

    public ContextFusionEngine()
    {
        rules = new List<ContextRule>
        {
            new ContextRule(IsEngaged, ContextState.Engaged),
            new ContextRule(IsDistracted, ContextState.Distracted),
            new ContextRule(IsIdle, ContextState.Idle),
            new ContextRule(IsTransitioning, ContextState.Transitioning)
        };
    }

    public ContextResult Evaluate(
        GazeFeatureVector gaze,
        BodyFeatureVector body,
        HandFeatureVector hand,
        SpatialContextVector spatial)
    {
        foreach (ContextRule rule in rules)
        {
            if (rule.condition(gaze, body, hand, spatial))
            {
                return new ContextResult
                {
                    state = rule.targetState,
                    confidence = CalculateConfidence(rule.targetState, gaze, body, hand, spatial)
                };
            }
        }

        return new ContextResult
        {
            state = ContextState.Transitioning,
            confidence = 0.55f
        };
    }

    private static bool IsEngaged(GazeFeatureVector gaze, BodyFeatureVector body, HandFeatureVector hand, SpatialContextVector spatial)
    {
        bool postureSupportsEngagement =
            body.posture_label == PostureLabel.Upright ||
            body.posture_label == PostureLabel.LeaningForward;

        bool activeInteraction =
            hand.pinch_active ||
            hand.interaction_frequency >= 0.05f ||
            hand.object_proximity;

        bool currentTaskFocus = gaze.currently_on_task_aoi;
        bool recentTaskFocus =
            gaze.aoi_dwell_ratio >= 0.18f &&
            hand.pinch_active;

        bool roomScaleEngagement =
            spatial.boundary_type == BoundaryType.RoomScale &&
            spatial.locomotion_activity != LocomotionActivity.None &&
            currentTaskFocus;

        bool engagedAttentionState =
            (currentTaskFocus && (gaze.fixation_on_aoi || activeInteraction || postureSupportsEngagement || roomScaleEngagement)) ||
            (recentTaskFocus && postureSupportsEngagement);

        return engagedAttentionState &&
               gaze.head_gaze_divergence_deg <= 18f &&
               gaze.saccade_rate_per_s <= 2.5f;
    }

    private static bool IsDistracted(GazeFeatureVector gaze, BodyFeatureVector body, HandFeatureVector hand, SpatialContextVector spatial)
    {
        bool clearlyAwayFromTask =
            !gaze.currently_on_task_aoi &&
            !gaze.fixation_on_aoi &&
            gaze.aoi_dwell_ratio < 0.20f;

        bool activeAwaySignal =
            gaze.head_gaze_divergence_deg > 12f ||
            gaze.saccade_rate_per_s > 0.15f ||
            body.avg_joint_velocity >= 0.03f;

        return clearlyAwayFromTask &&
               activeAwaySignal &&
               !hand.pinch_active &&
               hand.interaction_frequency < 0.05f;
    }

    private static bool IsIdle(GazeFeatureVector gaze, BodyFeatureVector body, HandFeatureVector hand, SpatialContextVector spatial)
    {
        return body.avg_joint_velocity < 0.05f &&
               hand.interaction_frequency <= 0.01f &&
               !hand.object_proximity &&
               !hand.pinch_active &&
               !gaze.currently_on_task_aoi &&
               !gaze.fixation_on_aoi &&
               gaze.aoi_dwell_ratio < 0.10f;
    }

    private static bool IsTransitioning(GazeFeatureVector gaze, BodyFeatureVector body, HandFeatureVector hand, SpatialContextVector spatial)
    {
        bool shiftingAttention =
            gaze.head_gaze_divergence_deg > 15f ||
            (gaze.currently_on_task_aoi && !gaze.fixation_on_aoi) ||
            (gaze.fixation_duration_s > 0.08f && gaze.fixation_duration_s < 0.35f) ||
            (gaze.aoi_dwell_ratio >= 0.10f && gaze.aoi_dwell_ratio < 0.18f);

        bool preparingInteraction =
            hand.object_proximity &&
            hand.interaction_frequency < 0.10f;

        return shiftingAttention || preparingInteraction;
    }

    private static float CalculateConfidence(
        ContextState state,
        GazeFeatureVector gaze,
        BodyFeatureVector body,
        HandFeatureVector hand,
        SpatialContextVector spatial)
    {
        float confidence = 0.5f;

        switch (state)
        {
            case ContextState.Engaged:
                if (gaze.currently_on_task_aoi) confidence += 0.14f;
                if (gaze.fixation_on_aoi) confidence += 0.18f;
                if (gaze.aoi_dwell_ratio >= 0.18f && hand.pinch_active) confidence += 0.08f;
                if (hand.pinch_active || hand.interaction_frequency >= 0.05f || hand.object_proximity) confidence += 0.12f;
                if (body.posture_label == PostureLabel.Upright || body.posture_label == PostureLabel.LeaningForward) confidence += 0.08f;
                break;

            case ContextState.Distracted:
                if (!gaze.currently_on_task_aoi && !gaze.fixation_on_aoi) confidence += 0.12f;
                if (gaze.aoi_dwell_ratio < 0.20f) confidence += 0.12f;
                if (gaze.head_gaze_divergence_deg > 12f || gaze.saccade_rate_per_s > 0.15f || body.avg_joint_velocity >= 0.03f) confidence += 0.16f;
                break;

            case ContextState.Idle:
                if (body.avg_joint_velocity < 0.05f) confidence += 0.12f;
                if (hand.interaction_frequency <= 0.01f && !hand.object_proximity && !hand.pinch_active) confidence += 0.12f;
                if (!gaze.currently_on_task_aoi && !gaze.fixation_on_aoi && gaze.aoi_dwell_ratio < 0.10f) confidence += 0.16f;
                break;

            case ContextState.Transitioning:
            default:
                if (gaze.head_gaze_divergence_deg > 15f) confidence += 0.12f;
                if (gaze.aoi_dwell_ratio >= 0.10f && gaze.aoi_dwell_ratio < 0.45f) confidence += 0.08f;
                if (hand.object_proximity) confidence += 0.08f;
                break;
        }

        return Mathf.Clamp01(confidence);
    }
}
