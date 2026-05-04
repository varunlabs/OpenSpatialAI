using UnityEngine;

public enum PostureMode
{
    Sitting,
    Standing,
    RoomScale
}

public enum BoundaryType
{
    Stationary,
    RoomScale,
    Custom,
    Passthrough
}

public enum LocomotionActivity
{
    None,
    Low,
    Active
}

public struct SpatialContextVector
{
    public PostureMode posture_mode;
    public BoundaryType boundary_type;
    public LocomotionActivity locomotion_activity;
}

public class SpatialContextExtractor
{
    private Vector3 previousPosition;
    private bool hasPrev;

    public SpatialContextVector Extract(SignalFrame frame)
    {
        PostureMode postureMode;

        if (frame.posture_class == "standing")
        {
            postureMode = PostureMode.Standing;
        }
        else if (frame.posture_class == "room_scale")
        {
            postureMode = PostureMode.RoomScale;
        }
        else
        {
            postureMode = PostureMode.Sitting;
        }

        BoundaryType boundaryType;

        switch (frame.boundary_type)
        {
            case "room_scale":
                boundaryType = BoundaryType.RoomScale;
                break;
            case "custom":
                boundaryType = BoundaryType.Custom;
                break;
            case "passthrough":
                boundaryType = BoundaryType.Passthrough;
                break;
            default:
                boundaryType = BoundaryType.Stationary;
                break;
        }

        float movement = 0f;

        if (hasPrev)
        {
            movement = Vector3.Distance(previousPosition, frame.head_position);
        }

        previousPosition = frame.head_position;
        hasPrev = true;

        LocomotionActivity locomotion;

        if (movement < 0.1f)
        {
            locomotion = LocomotionActivity.None;
        }
        else if (movement < 0.5f)
        {
            locomotion = LocomotionActivity.Low;
        }
        else
        {
            locomotion = LocomotionActivity.Active;
        }

        return new SpatialContextVector
        {
            posture_mode = postureMode,
            boundary_type = boundaryType,
            locomotion_activity = locomotion
        };
    }
}
