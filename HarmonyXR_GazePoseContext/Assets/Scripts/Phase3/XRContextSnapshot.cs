using UnityEngine;

public struct XRContextSnapshot
{
    public ContextState state;
    public float confidence;
    public BoundaryType boundaryType;
    public PostureMode postureMode;
    public long timestampMs;
    public string aoiHit;
}
