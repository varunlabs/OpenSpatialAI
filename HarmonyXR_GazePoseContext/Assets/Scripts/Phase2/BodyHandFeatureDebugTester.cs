using UnityEngine;

public class BodyHandFeatureDebugTester : MonoBehaviour
{
    private BodyHandFeatureExtractor extractor;
    [SerializeField] 
    private SignalSynchroniser signalSynchroniser;

    private void Awake()
    {
        extractor = new BodyHandFeatureExtractor();
    }

    private void Start()
    {
        Debug.Log("[BodyHandFeatureDebugTester] STARTED");

        if (signalSynchroniser == null)
        {
            signalSynchroniser = FindObjectOfType<SignalSynchroniser>();
        }

        if (signalSynchroniser == null)
        {
            Debug.LogError("SignalSynchroniser not found");
            return;
        }

        InvokeRepeating("SampleAndReport", 0.1f, 0.1f);
    }

    private void SampleAndReport()
    {
        if (signalSynchroniser == null)
        {
            return;
        }

        SignalFrame frame = signalSynchroniser.GetLatestFrame();
        BodyHandFeatureVector features = extractor.ExtractFeatures(frame);

        Debug.Log(
            "[BodyHandFeatureDebugTester] " +
            "posture_class=" + features.posture_class +
            ", motion_state=" + features.motion_state +
            ", avg_joint_velocity=" + features.avg_joint_velocity.ToString("F3") +
            ", interaction_count_10s=" + features.interaction_count_10s +
            ", interaction_level=" + features.interaction_level +
            ", nearest_object_dist_m=" + features.nearest_object_dist_m.ToString("F3"));
    }
}
