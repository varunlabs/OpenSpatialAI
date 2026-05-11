using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandCapture : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;

    [Header("Pinch Settings")]
    [SerializeField] private float pinchThreshold = 0.7f;
    [SerializeField] private bool allowControllerInteractionFallback = true;
    [SerializeField] private float controllerTriggerThreshold = 0.75f;

    public bool left_pinch { get; private set; }
    public bool right_pinch { get; private set; }
    public int interaction_count_10s { get; private set; }
    public float nearest_object_dist_m { get; private set; }

    private readonly Queue<float> interactionEventTimes = new Queue<float>();
    private bool prevLeftPinch;
    private bool prevRightPinch;
    private float qaLogTimer;

    private void Awake()
    {
        ResolveHandsIfNeeded();
        ResetOutputs();
        Debug.Log("Test on headset: pinch index+thumb; observe values change");
    }

    private void Update()
    {
        ResolveHandsIfNeeded();
        UpdatePinchStates();
        UpdateInteractionWindow();
        UpdateNearestAoiDistance();
        ReportQaLogsOncePerSecond();
    }

    private void UpdatePinchStates()
    {
        bool leftHandPinch = ReadPinchState(leftHand, "Left");
        bool rightHandPinch = ReadPinchState(rightHand, "Right");
        bool leftControllerSelect = allowControllerInteractionFallback &&
                                    (ReadControllerSelect(XRNode.LeftHand) || ReadOvrControllerSelect(true));
        bool rightControllerSelect = allowControllerInteractionFallback &&
                                     (ReadControllerSelect(XRNode.RightHand) || ReadOvrControllerSelect(false));

        left_pinch = leftHandPinch || leftControllerSelect;
        right_pinch = rightHandPinch || rightControllerSelect;

        bool leftPinchStart = left_pinch && !prevLeftPinch;
        bool rightPinchStart = right_pinch && !prevRightPinch;

        if (leftPinchStart)
        {
            RegisterInteractionEvent();
        }

        if (rightPinchStart)
        {
            RegisterInteractionEvent();
        }

        prevLeftPinch = left_pinch;
        prevRightPinch = right_pinch;
    }

    private bool ReadPinchState(OVRHand hand, string handLabel)
    {
        if (hand == null)
        {
            Debug.Log(handLabel + " Hand Tracked: false");
            Debug.Log(handLabel + " Hand HighConfidence: false");
            Debug.Log(handLabel + " Pinch Strength: 0");
            return false;
        }

        Debug.Log(handLabel + " Hand Tracked: " + hand.IsTracked);
        Debug.Log(handLabel + " Hand HighConfidence: " + hand.IsDataHighConfidence);

        if (!hand.IsTracked)
        {
            return false;
        }

        float strength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        Debug.Log(handLabel + " Pinch Strength: " + strength);

        if (!IsFinite(strength))
        {
            return false;
        }

        bool pinch = strength >= pinchThreshold;
        if (pinch)
        {
            Debug.Log("PINCH DETECTED");
        }

        return pinch;
    }

    private bool ReadControllerSelect(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
        {
            return false;
        }

        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButton) && triggerButton)
        {
            return true;
        }

        if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton) && gripButton)
        {
            return true;
        }

        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButton) && primaryButton)
        {
            return true;
        }

        return device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) &&
               IsFinite(triggerValue) &&
               triggerValue >= controllerTriggerThreshold;
    }

    private bool ReadOvrControllerSelect(bool left)
    {
        OVRInput.Controller controller = left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        OVRInput.Controller handController = left ? OVRInput.Controller.LHand : OVRInput.Controller.RHand;
        OVRInput.RawButton indexButton = left ? OVRInput.RawButton.LIndexTrigger : OVRInput.RawButton.RIndexTrigger;
        OVRInput.RawButton gripButton = left ? OVRInput.RawButton.LHandTrigger : OVRInput.RawButton.RHandTrigger;
        OVRInput.RawAxis1D indexAxis = left ? OVRInput.RawAxis1D.LIndexTrigger : OVRInput.RawAxis1D.RIndexTrigger;

        if (OVRInput.Get(indexButton, controller) ||
            OVRInput.Get(gripButton, controller) ||
            OVRInput.Get(indexButton, handController) ||
            OVRInput.Get(gripButton, handController) ||
            OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, handController) ||
            OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, handController))
        {
            return true;
        }

        float triggerValue = OVRInput.Get(indexAxis, controller);
        float handTriggerValue = OVRInput.Get(indexAxis, handController);
        return (IsFinite(triggerValue) && triggerValue >= controllerTriggerThreshold) ||
               (IsFinite(handTriggerValue) && handTriggerValue >= controllerTriggerThreshold);
    }

    private void RegisterInteractionEvent()
    {
        interactionEventTimes.Enqueue(Time.time);
    }

    private void UpdateInteractionWindow()
    {
        float cutoff = Time.time - 10f;

        while (interactionEventTimes.Count > 0 && interactionEventTimes.Peek() < cutoff)
        {
            interactionEventTimes.Dequeue();
        }

        interaction_count_10s = interactionEventTimes.Count;
    }

    private void UpdateNearestAoiDistance()
    {
        Transform dominantHand = ResolveDominantHandTransform();
        if (dominantHand == null || !IsFinite(dominantHand.position))
        {
            nearest_object_dist_m = 0f;
            return;
        }

        GameObject[] aoiObjects = GameObject.FindGameObjectsWithTag("AOI");
        if (aoiObjects == null || aoiObjects.Length == 0)
        {
            nearest_object_dist_m = 0f;
            return;
        }

        float nearest = float.PositiveInfinity;
        Vector3 handPos = dominantHand.position;

        for (int i = 0; i < aoiObjects.Length; i++)
        {
            GameObject obj = aoiObjects[i];
            if (obj == null)
            {
                continue;
            }

            Vector3 aoiPos = obj.transform.position;
            if (!IsFinite(aoiPos))
            {
                continue;
            }

            float dist = Vector3.Distance(handPos, aoiPos);
            if (IsFinite(dist) && dist < nearest)
            {
                nearest = dist;
            }
        }

        nearest_object_dist_m = float.IsPositiveInfinity(nearest) ? 0f : nearest;
    }

    private Transform ResolveDominantHandTransform()
    {
        if (IsHandTracked(rightHand))
        {
            return rightHand.transform;
        }

        if (IsHandTracked(leftHand))
        {
            return leftHand.transform;
        }

        return null;
    }

    private bool IsHandTracked(OVRHand hand)
    {
        return hand != null && hand.IsTracked && hand.IsDataHighConfidence;
    }

    private void ReportQaLogsOncePerSecond()
    {
        qaLogTimer += Time.deltaTime;
        if (qaLogTimer < 1f)
        {
            return;
        }

        Debug.Log(
            "Left Pinch: " + left_pinch +
            " | Right Pinch: " + right_pinch +
            " | Interaction Count (10s): " + interaction_count_10s +
            " | Nearest Distance (m): " + nearest_object_dist_m.ToString("F3") +
            " | OVR Controllers: " + OVRInput.GetConnectedControllers()
        );

        qaLogTimer = 0f;
    }

    private void ResetOutputs()
    {
        left_pinch = false;
        right_pinch = false;
        interaction_count_10s = 0;
        nearest_object_dist_m = 0f;
        prevLeftPinch = false;
        prevRightPinch = false;
        qaLogTimer = 0f;
        interactionEventTimes.Clear();
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsFinite(Vector3 v)
    {
        return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    }

    private void ResolveHandsIfNeeded()
    {
        if (leftHand != null && rightHand != null)
        {
            return;
        }

        OVRHand[] hands = FindObjectsOfType<OVRHand>(true);
        for (int i = 0; i < hands.Length; i++)
        {
            OVRHand hand = hands[i];
            if (hand == null)
            {
                continue;
            }

            string objectName = hand.gameObject.name.ToLowerInvariant();
            if (leftHand == null && objectName.Contains("left"))
            {
                leftHand = hand;
            }
            else if (rightHand == null && objectName.Contains("right"))
            {
                rightHand = hand;
            }
        }
    }
}
