using UnityEngine;

public class ContextDebugTester : MonoBehaviour
{
    [SerializeField] private SignalSynchroniser signalSynchroniser;

    private GazeFeatureExtractor gazeExtractor;
    private BodyFeatureExtractor bodyExtractor;
    private HandFeatureExtractor handExtractor;
    private SpatialContextExtractor spatialExtractor;
    private ContextFusionEngine fusionEngine;
    private ContextStateMachine stateMachine;
    private ContextLogger logger;

    private long previousTimestamp;
    private bool hasPrev;

    private void Awake()
    {
        gazeExtractor = new GazeFeatureExtractor();
        bodyExtractor = new BodyFeatureExtractor();
        handExtractor = new HandFeatureExtractor();
        spatialExtractor = new SpatialContextExtractor();
        fusionEngine = new ContextFusionEngine();
        stateMachine = new ContextStateMachine();
        logger = new ContextLogger();
    }

    private void Start()
    {
        if (signalSynchroniser == null)
        {
            signalSynchroniser = FindObjectOfType<SignalSynchroniser>();
        }

        InvokeRepeating(nameof(EvaluateContext), 0.1f, 0.1f);
    }

    private void EvaluateContext()
    {
        if (signalSynchroniser == null)
        {
            return;
        }

        SignalFrame frame = signalSynchroniser.GetLatestFrame();
        Debug.Log(
            "[SYNC CHECK] timestamp=" + frame.timestamp_ms +
            ", AOI=" + frame.aoi_hit
        );

        var gaze = gazeExtractor.ExtractFeatures(frame);
        var body = bodyExtractor.ExtractFeatures(frame);
        var hand = handExtractor.ExtractFeatures(frame);
        var spatial = spatialExtractor.Extract(frame);

        var fusionResult = fusionEngine.Evaluate(gaze, body, hand, spatial);
        var result = stateMachine.Update(fusionResult);
        logger.Log(frame, result);

        Debug.Log(
            "[Context] state=" + result.state +
            ", confidence=" + result.confidence.ToString("F2") +
            ", timestamp=" + frame.timestamp_ms
        );

        previousTimestamp = frame.timestamp_ms;
        hasPrev = true;
    }
}
