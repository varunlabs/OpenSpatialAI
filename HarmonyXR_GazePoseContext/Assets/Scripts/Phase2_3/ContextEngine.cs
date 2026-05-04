using UnityEngine;

public enum ContextState
{
    Engaged,
    Distracted,
    Idle,
    Transitioning
}

public struct ContextResult
{
    public ContextState state;
    public float confidence;
}

public class ContextEngine
{
    public ContextResult Evaluate(
        GazeFeatureVector gaze,
        BodyFeatureVector body,
        HandFeatureVector hand,
        SpatialContextVector spatial)
    {
        bool hasInteraction = hand.interaction_frequency > 0.2f || hand.object_proximity;

        float dwellThreshold = 0.5f;
        float saccadeThreshold = 2.0f;

        if (spatial.posture_mode == PostureMode.Standing)
        {
            dwellThreshold = 0.45f;
        }

        if (spatial.locomotion_activity == LocomotionActivity.Active)
        {
            saccadeThreshold = 3.0f;
        }

        bool isEngaged =
            (gaze.fixation_on_aoi || gaze.aoi_dwell_ratio > dwellThreshold) &&
            gaze.saccade_rate_per_s < saccadeThreshold;

        bool isDistracted =
            gaze.saccade_rate_per_s > 2.5f &&
            gaze.aoi_dwell_ratio < 0.3f;

        bool isIdle =
            body.avg_joint_velocity < 0.05f &&
            hand.interaction_frequency < 0.01f &&
            gaze.aoi_dwell_ratio < 0.3f;

        ContextState state;
        float confidence = 0.5f;

        if (isEngaged)
        {
            state = ContextState.Engaged;

            if (gaze.fixation_on_aoi)
            {
                confidence += 0.2f;
            }

            if (gaze.aoi_dwell_ratio > 0.5f)
            {
                confidence += 0.2f;
            }

            if (hasInteraction)
            {
                confidence += 0.1f;
            }
        }
        else if (isDistracted)
        {
            state = ContextState.Distracted;

            if (gaze.saccade_rate_per_s > 2.5f)
            {
                confidence += 0.2f;
            }

            if (gaze.aoi_dwell_ratio < 0.3f)
            {
                confidence += 0.2f;
            }
        }
        else if (isIdle)
        {
            state = ContextState.Idle;

            if (body.avg_joint_velocity < 0.05f)
            {
                confidence += 0.2f;
            }

            if (hand.interaction_frequency < 0.01f)
            {
                confidence += 0.2f;
            }
        }
        else
        {
            state = ContextState.Transitioning;
        }

        confidence = Mathf.Clamp(confidence, 0f, 1f);

        return new ContextResult
        {
            state = state,
            confidence = confidence
        };
    }
}
