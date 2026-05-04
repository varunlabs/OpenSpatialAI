using UnityEngine;

public struct BodyHandFeatureVector
{
    public string posture_class;
    public float avg_joint_velocity;
    public string motion_state;
    public int interaction_count_10s;
    public float nearest_object_dist_m;
    public string interaction_level;
}

public sealed class BodyHandFeatureExtractor
{
    private const string StandingPosture = "standing";
    private const string SittingPosture = "sitting";
    private const string LeaningPosture = "leaning";
    private const string ReachingPosture = "reaching";

    public BodyHandFeatureVector ExtractFeatures(SignalFrame frame)
    {
        string normalizedPosture = NormalizePosture(frame.posture_class);
        string motionState = GetMotionState(frame.avg_joint_velocity);
        string interactionLevel = GetInteractionLevel(frame.interaction_count_10s);

        return new BodyHandFeatureVector
        {
            posture_class = normalizedPosture,
            avg_joint_velocity = frame.avg_joint_velocity,
            motion_state = motionState,
            interaction_count_10s = frame.interaction_count_10s,
            nearest_object_dist_m = frame.nearest_object_dist_m,
            interaction_level = interactionLevel
        };
    }

    private static string NormalizePosture(string postureClass)
    {
        if (string.IsNullOrWhiteSpace(postureClass))
        {
            return StandingPosture;
        }

        string normalized = postureClass.Trim().ToLowerInvariant();
        if (normalized == StandingPosture ||
            normalized == SittingPosture ||
            normalized == LeaningPosture ||
            normalized == ReachingPosture)
        {
            return normalized;
        }

        return StandingPosture;
    }

    private static string GetMotionState(float avgJointVelocity)
    {
        if (avgJointVelocity < 0.01f)
        {
            return "idle";
        }

        if (avgJointVelocity < 0.1f)
        {
            return "low_motion";
        }

        return "active_motion";
    }

    private static string GetInteractionLevel(int interactionCount10s)
    {
        if (interactionCount10s <= 0)
        {
            return "none";
        }

        if (interactionCount10s <= 3)
        {
            return "low";
        }

        return "high";
    }
}
