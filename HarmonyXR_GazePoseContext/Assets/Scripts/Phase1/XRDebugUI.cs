using UnityEngine;
using TMPro;

public class XRDebugUI : MonoBehaviour
{
    [Header("Capture References")]
    [SerializeField] private GazeCapture gazeCapture;
    [SerializeField] private BodyPoseCapture bodyPoseCapture;
    [SerializeField] private HandCapture handCapture;

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI gazeStatusText;
    [SerializeField] private TextMeshProUGUI aoiHitText;
    [SerializeField] private TextMeshProUGUI headForwardText;
    [SerializeField] private TextMeshProUGUI spineAngleText;
    [SerializeField] private TextMeshProUGUI postureText;
    [SerializeField] private TextMeshProUGUI pinchText;
    [SerializeField] private TextMeshProUGUI interactionCountText;
    [SerializeField] private TextMeshProUGUI distanceText;

    private void Update()
    {
        UpdateGazeSection();
        UpdateBodySection();
        UpdateHandSection();
    }

    private void UpdateGazeSection()
    {
        if (gazeStatusText != null)
        {
            if (gazeCapture == null)
            {
                gazeStatusText.text = "Gaze Status: Missing GazeCapture";
            }
            else
            {
                gazeStatusText.text = "Gaze Status: " + (gazeCapture.is_fixation ? "Fixation" : "Scanning");
            }
        }

        if (aoiHitText != null)
        {
            if (gazeCapture == null)
            {
                aoiHitText.text = "AOI Hit: N/A";
            }
            else
            {
                aoiHitText.text = "AOI Hit: " + gazeCapture.aoi_hit;
            }
        }
    }

    private void UpdateBodySection()
    {
        if (headForwardText != null)
        {
            if (bodyPoseCapture == null)
            {
                headForwardText.text = "Head Forward: N/A";
            }
            else
            {
                headForwardText.text = "Head Forward: " + bodyPoseCapture.head_forward.ToString("F3");
            }
        }

        if (spineAngleText != null)
        {
            if (bodyPoseCapture == null)
            {
                spineAngleText.text = "Spine Angle: N/A";
            }
            else
            {
                spineAngleText.text = "Spine Angle: " + bodyPoseCapture.spine_angle_deg.ToString("F2") + " deg";
            }
        }

        if (postureText != null)
        {
            if (bodyPoseCapture == null)
            {
                postureText.text = "Posture: N/A";
            }
            else
            {
                postureText.text = "Posture: " + bodyPoseCapture.posture_class;
            }
        }
    }

    private void UpdateHandSection()
    {
        if (pinchText != null)
        {
            if (handCapture == null)
            {
                pinchText.text = "Left/Right Pinch: N/A";
            }
            else
            {
                pinchText.text = "Left/Right Pinch: " + handCapture.left_pinch + " / " + handCapture.right_pinch;
            }
        }

        if (interactionCountText != null)
        {
            if (handCapture == null)
            {
                interactionCountText.text = "Interaction Count (10s): N/A";
            }
            else
            {
                interactionCountText.text = "Interaction Count (10s): " + handCapture.interaction_count_10s;
            }
        }

        if (distanceText != null)
        {
            if (handCapture == null)
            {
                distanceText.text = "Nearest Distance (m): N/A";
            }
            else
            {
                distanceText.text = "Nearest Distance (m): " + handCapture.nearest_object_dist_m.ToString("F3");
            }
        }
    }
}
