using System;
using UnityEngine;

public class XRAppShellController : MonoBehaviour
{
    [SerializeField] private ContextDebugTester contextSource;

    public event Action<XRContextSnapshot> ContextSnapshotUpdated;

    public XRContextSnapshot LatestSnapshot { get; private set; }

    private void Start()
    {
        if (contextSource == null)
        {
            contextSource = FindObjectOfType<ContextDebugTester>(true);
        }

        if (contextSource == null)
        {
            Debug.LogWarning("[Phase3] XRAppShellController could not find ContextDebugTester.");
            return;
        }

        contextSource.ContextEvaluated += OnContextEvaluated;
    }

    private void OnDestroy()
    {
        if (contextSource != null)
        {
            contextSource.ContextEvaluated -= OnContextEvaluated;
        }
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
