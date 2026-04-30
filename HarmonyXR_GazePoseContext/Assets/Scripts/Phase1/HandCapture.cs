using System.Collections.Generic;
using UnityEngine;

public class HandCapture : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;

    [Header("Pinch Settings")]
    [SerializeField] private float pinchThreshold = 0.7f;

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
        ResetOutputs();
    }

    private void Update()
    {
        UpdatePinchStates();
        UpdateInteractionWindow();
        UpdateNearestAoiDistance();
        ReportQaLogsOncePerSecond();
    }

    private void UpdatePinchStates()
    {
        left_pinch = ReadPinchState(leftHand);
        right_pinch = ReadPinchState(rightHand);

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

    private bool ReadPinchState(OVRHand hand)
    {
        if (!IsHandTracked(hand))
        {
            return false;
        }

        float pinchStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        if (!IsFinite(pinchStrength))
        {
            return false;
        }

        return pinchStrength >= pinchThreshold;
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

        Debug.Log("Left Pinch: " + left_pinch);
        Debug.Log("Right Pinch: " + right_pinch);
        Debug.Log("Interaction Count (10s): " + interaction_count_10s);
        Debug.Log("Nearest Distance: " + nearest_object_dist_m);

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

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 340, 900, 30), "Left Pinch: " + left_pinch, style);
        GUI.Label(new Rect(10, 365, 900, 30), "Right Pinch: " + right_pinch, style);
        GUI.Label(new Rect(10, 390, 900, 30), "Interaction Count (10s): " + interaction_count_10s, style);
        GUI.Label(new Rect(10, 415, 900, 30), "Nearest Distance: " + nearest_object_dist_m.ToString("F3"), style);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsFinite(Vector3 v)
    {
        return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    }
}
