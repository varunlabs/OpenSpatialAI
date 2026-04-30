using System.Collections.Generic;
using UnityEngine;

// Layer 1 sensor script - no context inference logic
public class BodyPoseCapture : MonoBehaviour
{
    [Header("Tracking References")]
    [SerializeField] private OVRSkeleton bodySkeleton;
    [SerializeField] private OVRCameraRig ovrCameraRig;

    public Vector3 head_position { get; private set; }
    public Vector3 head_forward { get; private set; }
    public float head_gaze_divergence_deg { get; private set; }

    public string posture_class { get; private set; }
    public float spine_angle_deg { get; private set; }
    public float avg_joint_velocity { get; private set; }

    public Vector3 spine_base { get; private set; }
    public Vector3 spine_mid { get; private set; }
    public Vector3 neck { get; private set; }
    public Vector3 left_shoulder { get; private set; }
    public Vector3 right_shoulder { get; private set; }
    public Vector3 left_hip { get; private set; }
    public Vector3 right_hip { get; private set; }

    private readonly Dictionary<OVRSkeleton.BoneId, Vector3> previousJointPositions = new Dictionary<OVRSkeleton.BoneId, Vector3>();
    private Vector3 pendingGazeDirection;
    private bool hasPendingGazeDirection;

    private float qaLogTimer;

    private static readonly OVRSkeleton.BoneId[] RequiredJoints =
    {
        OVRSkeleton.BoneId.Body_Hips,
        OVRSkeleton.BoneId.Body_SpineMiddle,
        OVRSkeleton.BoneId.Body_Neck,
        OVRSkeleton.BoneId.Body_LeftShoulder,
        OVRSkeleton.BoneId.Body_RightShoulder,
        OVRSkeleton.BoneId.FullBody_LeftUpperLeg,
        OVRSkeleton.BoneId.FullBody_RightUpperLeg
    };

    private void Awake()
    {
        ResetOutputs();
    }

    private void Start()
    {
        Debug.Log("Run in Play Mode");
        Debug.Log("Move head left/right and observe values");
        Debug.Log("Ensure values are changing in real time");
    }

    private void Update()
    {
        UpdateHeadPose();

        bool hasBodyData = UpdateBodyTracking();
        if (!hasBodyData)
        {
            ApplyEditorFallbackFromHead();
        }

        UpdateHeadGazeDivergenceInternal();
        ReportQaLogsOncePerSecond();
    }

    // Gaze direction can be injected by the synchronizer/caller when available.
    public void SetGazeDirection(Vector3 gazeDirection)
    {
        Vector3 normalized = SafeNormalize(gazeDirection);
        hasPendingGazeDirection = normalized != Vector3.zero;

        if (hasPendingGazeDirection)
        {
            pendingGazeDirection = normalized;
        }
    }

    private bool UpdateBodyTracking()
    {
        if (bodySkeleton == null || !bodySkeleton.IsDataValid || bodySkeleton.Bones == null || bodySkeleton.Bones.Count == 0)
        {
            ResetBodyOutputs();
            previousJointPositions.Clear();
            return false;
        }

        if (!TryGetRequiredJointPositions(out Dictionary<OVRSkeleton.BoneId, Vector3> joints))
        {
            ResetBodyOutputs();
            previousJointPositions.Clear();
            return false;
        }

        spine_base = joints[OVRSkeleton.BoneId.Body_Hips];
        spine_mid = joints[OVRSkeleton.BoneId.Body_SpineMiddle];
        neck = joints[OVRSkeleton.BoneId.Body_Neck];
        left_shoulder = joints[OVRSkeleton.BoneId.Body_LeftShoulder];
        right_shoulder = joints[OVRSkeleton.BoneId.Body_RightShoulder];
        left_hip = joints[OVRSkeleton.BoneId.FullBody_LeftUpperLeg];
        right_hip = joints[OVRSkeleton.BoneId.FullBody_RightUpperLeg];

        Vector3 spineVector = neck - spine_base;
        Vector3 spineDirection = SafeNormalize(spineVector);
        spine_angle_deg = spineDirection == Vector3.zero ? 0f : Vector3.Angle(spineDirection, Vector3.up);

        // Simple heuristic classification - will be refined in Layer 2
        if (spine_angle_deg < 10f)
        {
            posture_class = "standing";
        }
        else if (spine_angle_deg <= 25f)
        {
            posture_class = "leaning";
        }
        else
        {
            posture_class = "reaching";
        }

        avg_joint_velocity = ComputeAverageJointVelocity(joints);
        return true;
    }

    private void ApplyEditorFallbackFromHead()
    {
        if (head_forward == Vector3.zero)
        {
            posture_class = "unknown";
            spine_angle_deg = 0f;
            return;
        }

        spine_angle_deg = Vector3.Angle(Vector3.up, head_forward);

        if (spine_angle_deg < 10f)
        {
            posture_class = "standing";
        }
        else if (spine_angle_deg <= 25f)
        {
            posture_class = "leaning";
        }
        else
        {
            posture_class = "reaching";
        }
    }

    private void UpdateHeadPose()
    {
        Transform headAnchor = ResolveHeadAnchor();
        if (headAnchor == null)
        {
            head_position = Vector3.zero;
            head_forward = Vector3.zero;
            return;
        }

        if (!IsFinite(headAnchor.position) || !IsFinite(headAnchor.forward))
        {
            head_position = Vector3.zero;
            head_forward = Vector3.zero;
            return;
        }

        Vector3 forward = SafeNormalize(headAnchor.forward);
        if (forward == Vector3.zero)
        {
            head_position = Vector3.zero;
            head_forward = Vector3.zero;
            return;
        }

        head_position = headAnchor.position;
        head_forward = forward;
    }

    private void UpdateHeadGazeDivergenceInternal()
    {
        // If gaze direction is unavailable, divergence defaults to 0.
        if (!hasPendingGazeDirection || head_forward == Vector3.zero)
        {
            head_gaze_divergence_deg = 0f;
            return;
        }

        head_gaze_divergence_deg = Vector3.Angle(head_forward, pendingGazeDirection);
    }

    private void ReportQaLogsOncePerSecond()
    {
        qaLogTimer += Time.deltaTime;
        if (qaLogTimer < 1f)
        {
            return;
        }

        Debug.Log("Head Forward: " + head_forward);
        Debug.Log("Spine Angle: " + spine_angle_deg);
        Debug.Log("Posture: " + posture_class);

        qaLogTimer = 0f;
    }

    private Transform ResolveHeadAnchor()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        if (ovrCameraRig == null)
        {
            return null;
        }

        if (ovrCameraRig.centerEyeAnchor != null)
        {
            return ovrCameraRig.centerEyeAnchor;
        }

        return ovrCameraRig.transform;
    }

    private bool TryGetRequiredJointPositions(out Dictionary<OVRSkeleton.BoneId, Vector3> joints)
    {
        joints = new Dictionary<OVRSkeleton.BoneId, Vector3>(RequiredJoints.Length);

        var bones = bodySkeleton.Bones;
        for (int i = 0; i < bones.Count; i++)
        {
            OVRBone bone = bones[i];
            if (bone == null || bone.Transform == null)
            {
                continue;
            }

            OVRSkeleton.BoneId id = bone.Id;
            if (!IsRequiredJoint(id))
            {
                continue;
            }

            Vector3 pos = bone.Transform.position;
            if (!IsFinite(pos))
            {
                continue;
            }

            joints[id] = pos;
        }

        for (int i = 0; i < RequiredJoints.Length; i++)
        {
            if (!joints.ContainsKey(RequiredJoints[i]))
            {
                return false;
            }
        }

        return true;
    }

    private float ComputeAverageJointVelocity(Dictionary<OVRSkeleton.BoneId, Vector3> joints)
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return 0f;
        }

        float sumVelocity = 0f;
        int velocityCount = 0;

        foreach (KeyValuePair<OVRSkeleton.BoneId, Vector3> kvp in joints)
        {
            if (previousJointPositions.TryGetValue(kvp.Key, out Vector3 prevPos))
            {
                float speed = Vector3.Distance(prevPos, kvp.Value) / dt;
                if (IsFinite(speed))
                {
                    sumVelocity += speed;
                    velocityCount++;
                }
            }

            previousJointPositions[kvp.Key] = kvp.Value;
        }

        if (velocityCount == 0)
        {
            return 0f;
        }

        return sumVelocity / velocityCount;
    }

    private static bool IsRequiredJoint(OVRSkeleton.BoneId id)
    {
        for (int i = 0; i < RequiredJoints.Length; i++)
        {
            if (RequiredJoints[i] == id)
            {
                return true;
            }
        }

        return false;
    }

    private void ResetOutputs()
    {
        ResetBodyOutputs();
        head_position = Vector3.zero;
        head_forward = Vector3.zero;
        head_gaze_divergence_deg = 0f;
        hasPendingGazeDirection = false;
        pendingGazeDirection = Vector3.zero;
        qaLogTimer = 0f;
    }

    private void ResetBodyOutputs()
    {
        spine_base = Vector3.zero;
        spine_mid = Vector3.zero;
        neck = Vector3.zero;
        left_shoulder = Vector3.zero;
        right_shoulder = Vector3.zero;
        left_hip = Vector3.zero;
        right_hip = Vector3.zero;
        posture_class = "unknown";
        spine_angle_deg = 0f;
        avg_joint_velocity = 0f;
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(20, 180, 900, 40), "Head Forward: " + head_forward, style);
        GUI.Label(new Rect(20, 220, 900, 40), "Spine Angle: " + spine_angle_deg, style);
        GUI.Label(new Rect(20, 260, 900, 40), "Posture: " + posture_class, style);
    }

    private static Vector3 SafeNormalize(Vector3 v)
    {
        if (!IsFinite(v))
        {
            return Vector3.zero;
        }

        float sqrMag = v.sqrMagnitude;
        if (sqrMag < 1e-8f)
        {
            return Vector3.zero;
        }

        return v / Mathf.Sqrt(sqrMag);
    }

    private static bool IsFinite(Vector3 v)
    {
        return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
