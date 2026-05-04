public enum PostureLabel
{
    Upright,
    LeaningForward,
    LeaningBack,
    Slouched
}

public struct BodyFeatureVector
{
    public PostureLabel posture_label;
    public float avg_joint_velocity;
}

public class BodyFeatureExtractor
{
    public BodyFeatureVector ExtractFeatures(SignalFrame frame)
    {
        float spineAngle = frame.spine_angle_deg;
        PostureLabel postureLabel;

        if (spineAngle < 10f)
        {
            postureLabel = PostureLabel.Upright;
        }
        else if (spineAngle < 30f)
        {
            postureLabel = PostureLabel.LeaningForward;
        }
        else if (spineAngle <= 60f)
        {
            postureLabel = PostureLabel.Slouched;
        }
        else
        {
            postureLabel = PostureLabel.LeaningBack;
        }

        return new BodyFeatureVector
        {
            posture_label = postureLabel,
            avg_joint_velocity = frame.avg_joint_velocity
        };
    }
}
