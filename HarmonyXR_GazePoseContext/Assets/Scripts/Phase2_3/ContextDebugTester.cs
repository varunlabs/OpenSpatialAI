using UnityEngine;

public class ContextDebugTester : MonoBehaviour
{
    [SerializeField] private SignalSynchroniser signalSynchroniser;
    [SerializeField] private CubeStateDisplay cubeDisplay;
    [SerializeField] private ContextStateUIDisplay uiDisplay;
    [SerializeField] private GazeHighlight contextCubeHighlight;

    private GazeFeatureExtractor gazeExtractor;
    private BodyFeatureExtractor bodyExtractor;
    private HandFeatureExtractor handExtractor;
    private SpatialContextExtractor spatialExtractor;
    private ContextFusionEngine fusionEngine;
    private ContextStateMachine stateMachine;
    private ContextLogger logger;


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
        if (cubeDisplay == null)
        {
            cubeDisplay = FindObjectOfType<CubeStateDisplay>(true);
        }
        if (uiDisplay == null)
        {
            uiDisplay = FindObjectOfType<ContextStateUIDisplay>(true);
        }
        if (contextCubeHighlight == null)
        {
            contextCubeHighlight = FindObjectOfType<GazeHighlight>(true);
        }

        if (cubeDisplay == null)
        {
            Debug.LogWarning("[CTX] CubeStateDisplay reference is missing.");
        }
        if (uiDisplay == null)
        {
            Debug.LogWarning("[CTX] ContextStateUIDisplay reference is missing.");
        }
        if (contextCubeHighlight == null)
        {
            Debug.LogWarning("[CTX] contextCubeHighlight is missing. Cube will keep gaze red/green behavior.");
        }
        else if (!contextCubeHighlight.UsesContextStateColor)
        {
            Debug.LogWarning("[CTX] contextCubeHighlight has 'Use Context State Color' OFF. Cube will show only gaze red/green.");
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
            "[CTX] raw=" + fusionResult.state +
            " -> final=" + result.state +
            " | dwell=" + gaze.aoi_dwell_ratio.ToString("F3") +
            ", sacc=" + gaze.saccade_rate_per_s.ToString("F3") +
            ", fixOnAOI=" + gaze.fixation_on_aoi +
            ", fixDur=" + gaze.fixation_duration_s.ToString("F3") +
            ", bodyVel=" + body.avg_joint_velocity.ToString("F3") +
            ", handFreq=" + hand.interaction_frequency.ToString("F3")
        );

        cubeDisplay?.UpdateState(result);
        uiDisplay?.UpdateState(result);
        contextCubeHighlight?.SetContextState(result.state);

    }
}
