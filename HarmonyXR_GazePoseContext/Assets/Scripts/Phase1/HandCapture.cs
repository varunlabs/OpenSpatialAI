using System.Collections.Generic;
using UnityEngine;

// Layer 1 sensor script — handles hand tracking only, no context logic
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
    private GameObject[] cachedAoiObjects;

    private void Awake()
    {
        CacheAoiObjects();
        ResetOutputs();
    }

    private void Update()
    {
        UpdatePinchStates();
        UpdateInteractionWindow();
        UpdateNearestAoiDistance();
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

        return pinchStrength > pinchThreshold;
    }

    private void RegisterInteractionEvent()
    {
        interactionEventTimes.Enqueue(Time.time);
    }

    // Maintains rolling 10-second interaction event window.
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
            // nearest_object_dist_m = 0 means no AOI found OR hand tracking unavailable.
            nearest_object_dist_m = 0f;
            return;
        }

        if (cachedAoiObjects == null || cachedAoiObjects.Length == 0)
        {
            CacheAoiObjects();
        }

        if (cachedAoiObjects == null || cachedAoiObjects.Length == 0)
        {
            // nearest_object_dist_m = 0 means no AOI found OR hand tracking unavailable.
            nearest_object_dist_m = 0f;
            return;
        }

        float nearest = float.PositiveInfinity;
        Vector3 handPosition = dominantHand.position;

        for (int i = 0; i < cachedAoiObjects.Length; i++)
        {
            GameObject obj = cachedAoiObjects[i];
            if (obj == null)
            {
                continue;
            }

            Vector3 target = obj.transform.position;
            if (!IsFinite(target))
            {
                continue;
            }

            float dist = Vector3.Distance(handPosition, target);
            if (IsFinite(dist) && dist < nearest)
            {
                nearest = dist;
            }
        }

        if (float.IsPositiveInfinity(nearest))
        {
            CacheAoiObjects();
            nearest = FindNearestDistanceFromCache(handPosition);
        }

        // nearest_object_dist_m = 0 means no AOI found OR hand tracking unavailable.
        nearest_object_dist_m = float.IsPositiveInfinity(nearest) ? 0f : nearest;
    }

    private float FindNearestDistanceFromCache(Vector3 handPosition)
    {
        if (cachedAoiObjects == null || cachedAoiObjects.Length == 0)
        {
            return float.PositiveInfinity;
        }

        float nearest = float.PositiveInfinity;

        for (int i = 0; i < cachedAoiObjects.Length; i++)
        {
            GameObject obj = cachedAoiObjects[i];
            if (obj == null)
            {
                continue;
            }

            Vector3 target = obj.transform.position;
            if (!IsFinite(target))
            {
                continue;
            }

            float dist = Vector3.Distance(handPosition, target);
            if (IsFinite(dist) && dist < nearest)
            {
                nearest = dist;
            }
        }

        return nearest;
    }

    private void CacheAoiObjects()
    {
        cachedAoiObjects = GameObject.FindGameObjectsWithTag("AOI");
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
        if (hand == null)
        {
            return false;
        }

        return hand.IsTracked && hand.IsDataHighConfidence;
    }

    private void ResetOutputs()
    {
        left_pinch = false;
        right_pinch = false;
        interaction_count_10s = 0;
        nearest_object_dist_m = 0f;
        prevLeftPinch = false;
        prevRightPinch = false;
        interactionEventTimes.Clear();
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
