using System;
using System.Collections;
using UnityEngine;

public class XRAppShellController : MonoBehaviour
{
    [SerializeField] private ContextDebugTester contextSource;
    [Header("Fallback Testing (when Phase1 source is unavailable)")]
    [SerializeField] private bool enableFallbackSimulation = true;
    [SerializeField] private ContextState simulatedState = ContextState.Idle;
    [SerializeField] private BoundaryType simulatedBoundary = BoundaryType.Stationary;
    [SerializeField] private PostureMode simulatedPosture = PostureMode.Sitting;
    [SerializeField, Range(0f, 1f)] private float simulatedConfidence = 0.8f;

    public event Action<XRContextSnapshot> ContextSnapshotUpdated;

    public XRContextSnapshot LatestSnapshot { get; private set; }
    private bool isUsingFallback;

    private void Start()
    {
        StartCoroutine(BindContextSourceWhenAvailable());
    }

    private void OnDestroy()
    {
        if (contextSource != null)
        {
            contextSource.ContextEvaluated -= OnContextEvaluated;
        }
    }

    private IEnumerator BindContextSourceWhenAvailable()
    {
        float elapsed = 0f;
        while (contextSource == null && elapsed < 3f)
        {
            contextSource = FindObjectOfType<ContextDebugTester>(true);
            if (contextSource == null)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
        }

        if (contextSource != null)
        {
            contextSource.ContextEvaluated -= OnContextEvaluated;
            contextSource.ContextEvaluated += OnContextEvaluated;
            isUsingFallback = false;
            Debug.Log("[Phase3] XRAppShellController bound to ContextDebugTester automatically.");
            yield break;
        }

        if (enableFallbackSimulation)
        {
            isUsingFallback = true;
            Debug.LogWarning("[Phase3] ContextDebugTester not found. Using fallback simulation (keys 1-4 states, Q/W/E/R boundaries).");
            PublishSimulatedSnapshot();
        }
    }

    private void Update()
    {
        if (!isUsingFallback)
        {
            return;
        }

        bool changed = false;
        if (Input.GetKeyDown(KeyCode.Alpha1)) { simulatedState = ContextState.Engaged; changed = true; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { simulatedState = ContextState.Distracted; changed = true; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { simulatedState = ContextState.Transitioning; changed = true; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { simulatedState = ContextState.Idle; changed = true; }

        if (Input.GetKeyDown(KeyCode.Q)) { simulatedBoundary = BoundaryType.Stationary; changed = true; }
        if (Input.GetKeyDown(KeyCode.W)) { simulatedBoundary = BoundaryType.RoomScale; changed = true; }
        if (Input.GetKeyDown(KeyCode.E)) { simulatedBoundary = BoundaryType.Custom; changed = true; }
        if (Input.GetKeyDown(KeyCode.R)) { simulatedBoundary = BoundaryType.Passthrough; changed = true; }

        if (changed)
        {
            PublishSimulatedSnapshot();
        }
    }

    private void PublishSimulatedSnapshot()
    {
        LatestSnapshot = new XRContextSnapshot
        {
            state = simulatedState,
            confidence = simulatedConfidence,
            boundaryType = simulatedBoundary,
            postureMode = simulatedPosture,
            timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            aoiHit = "simulation"
        };

        ContextSnapshotUpdated?.Invoke(LatestSnapshot);
        Debug.Log("[Phase3] Simulated context => state=" + simulatedState + ", boundary=" + simulatedBoundary);
    }

    private void OnContextEvaluated(ContextResult result, SpatialContextVector spatial, SignalFrame frame)
    {
        LatestSnapshot = new XRContextSnapshot
        {
            state = result.state,
            confidence = result.confidence,
            boundaryType = spatial.boundary_type,
            postureMode = spatial.posture_mode,
            timestampMs = frame.timestamp_ms,
            aoiHit = frame.aoi_hit
        };

        ContextSnapshotUpdated?.Invoke(LatestSnapshot);
    }
}
