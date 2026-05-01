using UnityEngine;

public class GazeFeatureDebugTester : MonoBehaviour
{
    [SerializeField] private SignalSynchroniser signalSynchroniser;
    [SerializeField] private bool logToConsole = true;

    private GazeFeatureExtractor gazeFeatureExtractor;
    private long previousTimestampMs;
    private bool hasPreviousTimestamp;

    private void Awake()
    {
        gazeFeatureExtractor = new GazeFeatureExtractor();
    }

    private void Start()
    {
        if (signalSynchroniser == null)
        {
            signalSynchroniser = FindObjectOfType<SignalSynchroniser>();
        }

        InvokeRepeating(nameof(SampleAndReport), 0.1f, 0.1f);
    }

    private void SampleAndReport()
    {
        if (signalSynchroniser == null)
        {
            return;
        }

        SignalFrame frame = signalSynchroniser.GetLatestFrame();
        GazeFeatureVector features = gazeFeatureExtractor.ExtractFeatures(frame);

        long deltaMs = 0L;
        if (hasPreviousTimestamp)
        {
            deltaMs = frame.timestamp_ms - previousTimestampMs;
        }

        previousTimestampMs = frame.timestamp_ms;
        hasPreviousTimestamp = true;

        if (logToConsole)
        {
            Debug.Log(
                "[GazeFeatureDebugTester] " +
                "fixation_on_aoi=" + features.fixation_on_aoi +
                ", fixation_duration_s=" + features.fixation_duration_s.ToString("F3") +
                ", saccade_rate_per_s=" + features.saccade_rate_per_s.ToString("F3") +
                ", aoi_dwell_ratio=" + features.aoi_dwell_ratio.ToString("F3") +
                ", head_gaze_divergence_deg=" + features.head_gaze_divergence_deg.ToString("F2") +
                ", sample_delta_ms=" + deltaMs +
                ", frame_timestamp_ms=" + frame.timestamp_ms);
        }
    }
}
