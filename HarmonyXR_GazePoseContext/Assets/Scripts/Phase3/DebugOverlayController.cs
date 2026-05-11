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
    [SerializeField] private HandCapture handCapture;
    [SerializeField] private Transform userHead;

    [Header("Overlay Root")]
    [SerializeField] private Canvas worldSpaceCanvas;
    [SerializeField] private float distanceFromHead = 0.65f;
    [SerializeField] private float rightOffset = 0.48f;
    [SerializeField] private float upOffset = 0.58f;

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

        if (handCapture == null)
        {
            handCapture = FindObjectOfType<HandCapture>(true);
        }

        if (userHead == null && Camera.main != null)
        {
            userHead = Camera.main.transform;
        }

        if (appShell != null)
        {
            appShell.ContextSnapshotUpdated += OnContextSnapshotUpdated;
        }

        EnsureOverlayScaffold();
        NormalizeOverlayLayout();
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
        if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool buttonPressed))
        {
            return buttonPressed;
        }

        if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            return triggerValue > 0.75f;
        }

        if (handCapture == null)
        {
            handCapture = FindObjectOfType<HandCapture>(true);
        }

        if (handCapture != null)
        {
            return handCapture.right_pinch;
        }

        return false;
    }

    private void UpdateOverlayTransform()
    {
        if (worldSpaceCanvas == null || userHead == null || !worldSpaceCanvas.gameObject.activeSelf)
        {
            return;
        }

        Vector3 flatForward = Vector3.ProjectOnPlane(userHead.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = userHead.forward;
        }

        Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;
        if (flatRight.sqrMagnitude < 0.0001f)
        {
            flatRight = Vector3.ProjectOnPlane(userHead.right, Vector3.up).normalized;
        }

        Vector3 anchor = userHead.position
            + flatForward * distanceFromHead
            + flatRight * rightOffset
            + Vector3.up * upOffset;

        worldSpaceCanvas.transform.position = anchor;

        // Keep the debug panel upright. Pitching the panel toward the head makes
        // the text appear slanted and can visually push rows outside the box.
        worldSpaceCanvas.transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    private void UpdateRealtimeUi()
    {
        if (worldSpaceCanvas == null || !worldSpaceCanvas.gameObject.activeSelf)
        {
            return;
        }

        if (contextStateText != null)
        {
            contextStateText.text = "State: " + latestSnapshot.state;
            contextStateText.color = StateColor(latestSnapshot.state);
        }

        if (confidenceFill != null)
        {
            confidenceFill.fillAmount = Mathf.Clamp01(latestSnapshot.confidence);
        }

        if (confidenceText != null)
        {
            confidenceText.text = "Confidence: " + (latestSnapshot.confidence * 100f).ToString("F0") + "%";
        }

        if (boundaryText != null)
        {
            boundaryText.text = "Boundary: " + latestSnapshot.boundaryType;
        }

        if (postureModeText != null)
        {
            postureModeText.text = "Posture Mode: " + latestSnapshot.postureMode;
        }

        if (lastFiveAoiText != null)
        {
            if (lastFiveAois.Count == 0)
            {
                lastFiveAoiText.text = "AOI (Last 5): -";
            }
            else
            {
                lastFiveAoiText.text = "AOI (Last 5):\n- " + string.Join("\n- ", lastFiveAois);
            }
        }

        if (fixationDurationText != null)
        {
            fixationDurationText.text = "Fixation: " + latestFrame.fixation_duration_s.ToString("F2") + " s";
        }

        if (bodyPostureClassText != null)
        {
            bodyPostureClassText.text = "Posture Class: " + (string.IsNullOrWhiteSpace(latestFrame.posture_class)
                ? "unknown"
                : latestFrame.posture_class);
        }

        if (interactionCountText != null)
        {
            interactionCountText.text = "Interactions (10s): " + latestFrame.interaction_count_10s;
        }

        RefreshTimelineUi();
    }

    private void EnqueueAoi(string aoi)
    {
        if (string.IsNullOrWhiteSpace(aoi))
        {
            return;
        }

        if (lastFiveAois.Count > 0)
        {
            string[] existing = lastFiveAois.ToArray();
            string mostRecent = existing[existing.Length - 1];
            if (string.Equals(mostRecent, aoi, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

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

    private void EnsureOverlayScaffold()
    {
        if (worldSpaceCanvas == null)
        {
            GameObject canvasGo = new GameObject("Phase3_DebugOverlayCanvas");
            worldSpaceCanvas = canvasGo.AddComponent<Canvas>();
            worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
            worldSpaceCanvas.worldCamera = Camera.main;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform canvasRt = worldSpaceCanvas.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(560f, 360f);
            canvasRt.localScale = Vector3.one * 0.00105f;
        }

        Transform root = worldSpaceCanvas.transform;
        RectTransform panel = EnsureUiImage("Panel", root, new Vector2(560f, 360f), new Color(0.02f, 0.025f, 0.03f, 0.94f));
        panel.anchoredPosition = Vector2.zero;
        Transform panelRoot = panel.transform;

        float y = -28f;
        contextStateText = EnsureField(contextStateText, "State", panelRoot, ref y);
        confidenceText = EnsureField(confidenceText, "Confidence", panelRoot, ref y);
        boundaryText = EnsureField(boundaryText, "Boundary", panelRoot, ref y);
        postureModeText = EnsureField(postureModeText, "Posture", panelRoot, ref y);
        fixationDurationText = EnsureField(fixationDurationText, "Fixation", panelRoot, ref y);
        bodyPostureClassText = EnsureField(bodyPostureClassText, "Posture Class", panelRoot, ref y);
        interactionCountText = EnsureField(interactionCountText, "Interactions", panelRoot, ref y);
        lastFiveAoiText = EnsureField(lastFiveAoiText, "Last AOIs", panelRoot, ref y, 120f);

        if (confidenceFill == null)
        {
            RectTransform bg = EnsureUiImage("ConfidenceBarBg", panelRoot, new Vector2(210f, 12f), new Color(0.2f, 0.2f, 0.2f, 0.9f));
            bg.anchoredPosition = new Vector2(170f, -60f);

            GameObject fillGo = new GameObject("ConfidenceBarFill");
            fillGo.transform.SetParent(bg, false);
            Image fill = fillGo.AddComponent<Image>();
            fill.color = new Color(0.18f, 0.72f, 0.92f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;

            RectTransform fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            confidenceFill = fill;
        }

        if (timelineSegmentContainer == null)
        {
            GameObject timelineRoot = new GameObject("TimelineSegments");
            timelineRoot.transform.SetParent(panelRoot, false);
            RectTransform rt = timelineRoot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520f, 24f);
            rt.anchoredPosition = new Vector2(0f, -198f);
            timelineSegmentContainer = rt;

            HorizontalLayoutGroup h = timelineRoot.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 2f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
        }

        if (timelineSegmentPrefab == null)
        {
            GameObject segGo = new GameObject("TimelineSegmentPrefab");
            segGo.SetActive(false);
            segGo.transform.SetParent(panelRoot, false);
            Image img = segGo.AddComponent<Image>();
            img.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            RectTransform rt = segGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(12f, 24f);
            timelineSegmentPrefab = img;
        }
    }

    private void NormalizeOverlayLayout()
    {
        if (worldSpaceCanvas == null)
        {
            return;
        }

        worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
        worldSpaceCanvas.worldCamera = Camera.main;

        distanceFromHead = 0.65f;
        rightOffset = 0.48f;
        upOffset = 0.58f;

        RectTransform canvasRt = worldSpaceCanvas.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(560f, 360f);
        canvasRt.localScale = Vector3.one * 0.00105f;
        canvasRt.localPosition = new Vector3(0f, 0f, 1.1f);
        canvasRt.localRotation = Quaternion.identity;

        RectTransform panel = GetOverlayPanel();
        if (panel != null)
        {
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(560f, 360f);
            panel.anchoredPosition = Vector2.zero;
            panel.localRotation = Quaternion.identity;
            panel.localScale = Vector3.one;
            panel.SetAsFirstSibling();
        }

        ReparentToOverlayPanel(contextStateText);
        ReparentToOverlayPanel(confidenceText);
        ReparentToOverlayPanel(boundaryText);
        ReparentToOverlayPanel(postureModeText);
        ReparentToOverlayPanel(lastFiveAoiText);
        ReparentToOverlayPanel(fixationDurationText);
        ReparentToOverlayPanel(bodyPostureClassText);
        ReparentToOverlayPanel(interactionCountText);

        LayoutField(contextStateText, new Vector2(510f, 36f), new Vector2(24f, -24f), 24f, TextAlignmentOptions.TopLeft, true);
        LayoutField(confidenceText, new Vector2(260f, 24f), new Vector2(24f, -66f), 17f, TextAlignmentOptions.TopLeft, false);
        LayoutField(boundaryText, new Vector2(260f, 24f), new Vector2(24f, -94f), 17f, TextAlignmentOptions.TopLeft, false);
        LayoutField(postureModeText, new Vector2(260f, 24f), new Vector2(24f, -122f), 17f, TextAlignmentOptions.TopLeft, false);
        LayoutField(lastFiveAoiText, new Vector2(510f, 92f), new Vector2(24f, -152f), 15f, TextAlignmentOptions.TopLeft, true);
        LayoutField(fixationDurationText, new Vector2(260f, 24f), new Vector2(24f, -252f), 17f, TextAlignmentOptions.TopLeft, false);
        LayoutField(bodyPostureClassText, new Vector2(260f, 24f), new Vector2(24f, -280f), 17f, TextAlignmentOptions.TopLeft, false);
        LayoutField(interactionCountText, new Vector2(260f, 24f), new Vector2(24f, -308f), 17f, TextAlignmentOptions.TopLeft, false);

        if (confidenceFill != null)
        {
            RectTransform confidenceRect = confidenceFill.rectTransform.parent as RectTransform;
            if (confidenceRect != null)
            {
                ReparentRectToOverlayPanel(confidenceRect);
                confidenceRect.anchorMin = new Vector2(0f, 1f);
                confidenceRect.anchorMax = new Vector2(0f, 1f);
                confidenceRect.pivot = new Vector2(0f, 1f);
                confidenceRect.sizeDelta = new Vector2(210f, 12f);
                confidenceRect.anchoredPosition = new Vector2(320f, -70f);
            }
        }

        if (timelineSegmentContainer != null)
        {
            ReparentRectToOverlayPanel(timelineSegmentContainer);
            timelineSegmentContainer.anchorMin = new Vector2(0f, 1f);
            timelineSegmentContainer.anchorMax = new Vector2(0f, 1f);
            timelineSegmentContainer.pivot = new Vector2(0f, 1f);
            timelineSegmentContainer.sizeDelta = new Vector2(510f, 14f);
            timelineSegmentContainer.anchoredPosition = new Vector2(24f, -336f);
        }
    }

    private static void LayoutField(TMP_Text field, Vector2 size, Vector2 anchoredPosition, float fontSize, TextAlignmentOptions alignment, bool wrap)
    {
        if (field == null)
        {
            return;
        }

        RectTransform rect = field.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        field.fontSize = fontSize;
        field.alignment = alignment;
        field.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        field.enableWordWrapping = wrap;
        field.overflowMode = wrap ? TextOverflowModes.Ellipsis : TextOverflowModes.Truncate;
        field.raycastTarget = false;
    }

    private RectTransform GetOverlayPanel()
    {
        if (worldSpaceCanvas == null)
        {
            return null;
        }

        return worldSpaceCanvas.transform.Find("Panel") as RectTransform;
    }

    private void ReparentToOverlayPanel(TMP_Text field)
    {
        RectTransform panel = GetOverlayPanel();
        if (field == null || panel == null || field.transform.parent == panel)
        {
            return;
        }

        field.transform.SetParent(panel, false);
    }

    private void ReparentRectToOverlayPanel(RectTransform rect)
    {
        RectTransform panel = GetOverlayPanel();
        if (rect == null || panel == null || rect.parent == panel)
        {
            return;
        }

        rect.SetParent(panel, false);
    }

    private static TMP_Text EnsureField(TMP_Text existing, string label, Transform parent, ref float y, float height = 32f)
    {
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new GameObject(label + "_Text");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 20f;
        txt.alignment = TextAlignmentOptions.Left;
        txt.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        txt.text = label + ": -";

        RectTransform rt = txt.rectTransform;
        rt.sizeDelta = new Vector2(560f, height);
        rt.anchoredPosition = new Vector2(0f, y);
        y -= height;
        return txt;
    }

    private static RectTransform EnsureUiImage(string name, Transform parent, Vector2 size, Color color)
    {
        Transform child = parent.Find(name);
        Image img = child != null ? child.GetComponent<Image>() : null;
        if (img == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            img = go.AddComponent<Image>();
        }

        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.sizeDelta = size;
        return rt;
    }
}
