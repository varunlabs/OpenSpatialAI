using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class DebugOverlayController : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private XRAppShellController appShell;
    [SerializeField] private ContextDebugTester contextSource;
    [SerializeField] private Transform userHead;

    [Header("Overlay Root")]
    [SerializeField] private Canvas worldSpaceCanvas;
    [SerializeField] private float distanceFromHead = 1.1f;
    [SerializeField] private float rightOffset = 0.45f;
    [SerializeField] private float upOffset = 0.25f;

    [Header("Toggle")]
    [SerializeField] private float rightTriggerLongPressSeconds = 0.8f;

    [Header("UI Fields")]
    [SerializeField] private TMP_Text contextStateText;
    [SerializeField] private Image confidenceFill;
    [SerializeField] private TMP_Text confidenceText;
    [SerializeField] private TMP_Text boundaryText;
    [SerializeField] private TMP_Text postureModeText;
    [SerializeField] private TMP_Text lastFiveAoiText;
    [SerializeField] private TMP_Text fixationDurationText;
    [SerializeField] private TMP_Text bodyPostureClassText;
    [SerializeField] private TMP_Text interactionCountText;

    [Header("Timeline (Last 30s)")]
    [SerializeField] private RectTransform timelineSegmentContainer;
    [SerializeField] private Image timelineSegmentPrefab;
    [SerializeField] private int timelineSegments = 30;

    private readonly Queue<string> lastFiveAois = new Queue<string>();
    private readonly List<StatePoint> timelinePoints = new List<StatePoint>();
    private readonly List<Image> timelineImages = new List<Image>();

    private XRContextSnapshot latestSnapshot;
    private SignalFrame latestFrame;
    private float triggerHoldTimer;
    private bool wasTriggerPressed;

    private struct StatePoint
    {
        public float time;
        public ContextState state;
    }

    private void Start()
    {
        if (appShell == null)
        {
            appShell = FindObjectOfType<XRAppShellController>(true);
        }

        if (contextSource == null)
        {
            contextSource = FindObjectOfType<ContextDebugTester>(true);
        }

        if (userHead == null && Camera.main != null)
        {
            userHead = Camera.main.transform;
        }

        if (appShell != null)
        {
            appShell.ContextSnapshotUpdated += OnContextSnapshotUpdated;
        }

        BuildTimelineUi();
        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (appShell != null)
        {
            appShell.ContextSnapshotUpdated -= OnContextSnapshotUpdated;
        }
    }

    private void Update()
    {
        HandleOverlayToggle();
        UpdateOverlayTransform();
        UpdateRealtimeUi();
    }

    private void OnContextSnapshotUpdated(XRContextSnapshot snapshot)
    {
        latestSnapshot = snapshot;

        if (!string.IsNullOrWhiteSpace(snapshot.aoiHit) && snapshot.aoiHit != "none")
        {
            EnqueueAoi(snapshot.aoiHit);
        }

        if (contextSource != null)
        {
            latestFrame = contextSource.LatestFrame;
        }

        AddTimelinePoint(snapshot.state);
    }

    private void HandleOverlayToggle()
    {
        bool triggerPressed = IsRightTriggerPressed();

        if (triggerPressed)
        {
            triggerHoldTimer += Time.deltaTime;
            if (!wasTriggerPressed && triggerHoldTimer >= rightTriggerLongPressSeconds)
            {
                ToggleOverlay();
                wasTriggerPressed = true;
            }
        }
        else
        {
            triggerHoldTimer = 0f;
            wasTriggerPressed = false;
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            ToggleOverlay();
        }
    }

    private void ToggleOverlay()
    {
        if (worldSpaceCanvas == null)
        {
            return;
        }

        bool next = !worldSpaceCanvas.gameObject.activeSelf;
        worldSpaceCanvas.gameObject.SetActive(next);
    }

    private bool IsRightTriggerPressed()
    {
        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!rightHand.isValid)
        {
            return false;
        }

        if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool buttonPressed))
        {
            return buttonPressed;
        }

        if (rightHand.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            return triggerValue > 0.75f;
        }

        return false;
    }

    private void UpdateOverlayTransform()
    {
        if (worldSpaceCanvas == null || userHead == null || !worldSpaceCanvas.gameObject.activeSelf)
        {
            return;
        }

        Vector3 anchor = userHead.position
            + userHead.forward * distanceFromHead
            + userHead.right * rightOffset
            + userHead.up * upOffset;

        worldSpaceCanvas.transform.position = anchor;

        Vector3 lookDir = worldSpaceCanvas.transform.position - userHead.position;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            worldSpaceCanvas.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }
    }

    private void UpdateRealtimeUi()
    {
        if (worldSpaceCanvas == null || !worldSpaceCanvas.gameObject.activeSelf)
        {
            return;
        }

        if (contextStateText != null)
        {
            contextStateText.text = latestSnapshot.state.ToString();
            contextStateText.color = StateColor(latestSnapshot.state);
        }

        if (confidenceFill != null)
        {
            confidenceFill.fillAmount = Mathf.Clamp01(latestSnapshot.confidence);
        }

        if (confidenceText != null)
        {
            confidenceText.text = (latestSnapshot.confidence * 100f).ToString("F0") + "%";
        }

        if (boundaryText != null)
        {
            boundaryText.text = latestSnapshot.boundaryType.ToString();
        }

        if (postureModeText != null)
        {
            postureModeText.text = latestSnapshot.postureMode.ToString();
        }

        if (lastFiveAoiText != null)
        {
            lastFiveAoiText.text = string.Join("\n", lastFiveAois);
        }

        if (fixationDurationText != null)
        {
            fixationDurationText.text = latestFrame.fixation_duration_s.ToString("F2") + " s";
        }

        if (bodyPostureClassText != null)
        {
            bodyPostureClassText.text = string.IsNullOrWhiteSpace(latestFrame.posture_class)
                ? "unknown"
                : latestFrame.posture_class;
        }

        if (interactionCountText != null)
        {
            interactionCountText.text = latestFrame.interaction_count_10s.ToString();
        }

        RefreshTimelineUi();
    }

    private void EnqueueAoi(string aoi)
    {
        lastFiveAois.Enqueue(aoi);
        while (lastFiveAois.Count > 5)
        {
            lastFiveAois.Dequeue();
        }
    }

    private void AddTimelinePoint(ContextState state)
    {
        timelinePoints.Add(new StatePoint { time = Time.time, state = state });

        float cutoff = Time.time - 30f;
        timelinePoints.RemoveAll(p => p.time < cutoff);
    }

    private void BuildTimelineUi()
    {
        if (timelineSegmentContainer == null || timelineSegmentPrefab == null)
        {
            return;
        }

        foreach (Transform child in timelineSegmentContainer)
        {
            Destroy(child.gameObject);
        }

        timelineImages.Clear();
        for (int i = 0; i < timelineSegments; i++)
        {
            Image seg = Instantiate(timelineSegmentPrefab, timelineSegmentContainer);
            seg.gameObject.SetActive(true);
            timelineImages.Add(seg);
        }
    }

    private void RefreshTimelineUi()
    {
        if (timelineImages.Count == 0)
        {
            return;
        }

        float now = Time.time;
        float windowStart = now - 30f;

        for (int i = 0; i < timelineImages.Count; i++)
        {
            float t = Mathf.Lerp(windowStart, now, (i + 1f) / timelineImages.Count);
            ContextState stateAtT = ResolveStateAtTime(t);
            timelineImages[i].color = StateColor(stateAtT);
        }
    }

    private ContextState ResolveStateAtTime(float sampleTime)
    {
        ContextState state = latestSnapshot.state;

        for (int i = 0; i < timelinePoints.Count; i++)
        {
            if (timelinePoints[i].time <= sampleTime)
            {
                state = timelinePoints[i].state;
            }
            else
            {
                break;
            }
        }

        return state;
    }

    private static Color StateColor(ContextState state)
    {
        switch (state)
        {
            case ContextState.Engaged:
                return new Color(0.50f, 0.20f, 0.75f); // purple
            case ContextState.Distracted:
                return new Color(1.00f, 0.75f, 0.20f); // amber
            case ContextState.Transitioning:
                return new Color(1.00f, 0.50f, 0.40f); // coral
            case ContextState.Idle:
            default:
                return new Color(0.60f, 0.60f, 0.60f); // gray
        }
    }
}
