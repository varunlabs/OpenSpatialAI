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
    private List<ContextRule> rules;

    public ContextFusionEngine()
    {
        rules = new List<ContextRule>
        {
            new ContextRule((gaze, body, hand, spatial) =>
                gaze.fixation_on_aoi &&
                gaze.aoi_dwell_ratio > 0.6f &&
                gaze.saccade_rate_per_s < 1.2f &&
                (body.posture_label == PostureLabel.Upright || body.posture_label == PostureLabel.LeaningForward),
                ContextState.Engaged
            ),

            new ContextRule((gaze, body, hand, spatial) =>
                // Transitional attention: gaze leaves stable focus, but not yet fully distracted/idle.
                (!gaze.fixation_on_aoi && gaze.aoi_dwell_ratio > 0.10f && gaze.aoi_dwell_ratio <= 0.60f) ||
                (gaze.saccade_rate_per_s >= 0.8f && gaze.saccade_rate_per_s < 2.2f &&
                 gaze.aoi_dwell_ratio > 0.10f && gaze.aoi_dwell_ratio < 0.35f),
                ContextState.Transitioning
            ),

            new ContextRule((gaze, body, hand, spatial) =>
                !gaze.fixation_on_aoi &&
                gaze.aoi_dwell_ratio <= 0.10f &&
                (gaze.saccade_rate_per_s >= 1.0f || gaze.fixation_duration_s >= 0.4f),
                ContextState.Distracted
            ),

            new ContextRule((gaze, body, hand, spatial) =>
                body.avg_joint_velocity < 0.05f &&
                hand.interaction_frequency == 0f &&
                !gaze.fixation_on_aoi,
                ContextState.Idle
            )
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
                    confidence = 0.8f
                };
            }
        }

        return new ContextResult
        {
            state = ContextState.Transitioning,
            confidence = 0.5f
        };
    }
}
