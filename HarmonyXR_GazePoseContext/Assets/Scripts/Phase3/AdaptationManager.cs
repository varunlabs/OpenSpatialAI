using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AdaptationManager : MonoBehaviour
{
    [Header("Runtime Mode")]
    [SerializeField] private bool qaMode = false;

    [Header("Context Source")]
    [SerializeField] private XRAppShellController appShell;

    [Header("State Stability")]
    [SerializeField] private float engagedMinVisibleSeconds = 1.0f;
    [SerializeField] private float distractedMinVisibleSeconds = 2.25f;
    [SerializeField] private float transitioningMinVisibleSeconds = 1.75f;
    [SerializeField] private float idleMinVisibleSeconds = 5.0f;
    [SerializeField] private float manualUiSuppressSeconds = 4.0f;

    [Header("Headset Controls")]
    [SerializeField] private bool keepHeadsetControlsVisible = true;
    [SerializeField] private float headsetUiDistance = 1.75f;
    [SerializeField] private float headsetUiVerticalOffset = 0.35f;
    [SerializeField] private float uiCanvasScale = 0.0012f;
    [SerializeField] private Vector3 currentTaskPanelLocalOffset = new Vector3(0f, 52f, 60f);
    [SerializeField] private Vector3 nextTaskPanelLocalOffset = new Vector3(0f, 52f, 60f);
    [SerializeField] private Vector3 distractedPanelLocalOffset = new Vector3(260f, 120f, 70f);
    [SerializeField] private Vector3 restPanelLocalOffset = new Vector3(-260f, -120f, 70f);
    [SerializeField] private Vector3 persistentControlsLocalOffset = new Vector3(-250f, -135f, 70f);
    [SerializeField] private Vector3 persistentStatsLocalOffset = new Vector3(430f, 125f, 90f);
    [SerializeField, Range(0f, 1f)] private float takeBreakDimAlpha = 0.08f;
    [SerializeField, Range(0f, 1f)] private float takeBreakVignetteIntensity = 0.18f;
    [SerializeField] private RectTransform persistentControlsRoot;
    [SerializeField] private RectTransform persistentStatsRoot;
    [SerializeField] private TMP_Text persistentStatsText;

    [Header("Engaged")]
    [SerializeField] private Volume globalVolume;
    [SerializeField, Range(0f, 1f)] private float engagedVignetteIntensity = 0.05f;
    [SerializeField] private GameObject navigationHintsRoot;

    [Header("Distracted")]
    [SerializeField] private CanvasGroup distractedEdgePanel;
    [SerializeField] private RectTransform distractedArrow;
    [SerializeField] private Transform taskAreaAnchor;
    [SerializeField] private AudioSource spatialCueSource;

    [Header("Transitioning")]
    [SerializeField] private CanvasGroup currentTaskPanel;
    [SerializeField] private CanvasGroup nextTaskPanel;
    [SerializeField] private Transform userHead;

    [Header("Idle")]
    [SerializeField] private GameObject restPanel;
    [SerializeField] private CanvasGroup restDimOverlay;
    [SerializeField] private GameObject floorArrow;
    [SerializeField] private Transform[] taskObjects;
    [SerializeField] private Transform uiAnchorRoot;

    private Coroutine activeBehavior;
    private Coroutine queuedStateRoutine;
    private ContextState activeState;
    private BoundaryType activeBoundaryType;
    private Vignette vignette;
    private Vector3 lastTaskPanelPosition;
    private ContextDebugTester contextSource;
    private XRContextSnapshot latestSnapshot;
    private SignalFrame latestFrame;
    private GazeFeatureExtractor statsGazeExtractor;
    private float activeStateStartedAt;
    private float suppressUiUntilTime;
    private bool hasQueuedSnapshot;
    private bool hasAppliedSnapshot;
    private bool idleChoicePanelLocked;
    private bool restBreakActive;
    private XRContextSnapshot queuedSnapshot;

    private void Start()
    {
        if (appShell == null)
        {
            appShell = FindObjectOfType<XRAppShellController>(true);
        }

        // If app shell is production-mode, force user-facing UI behavior.
        if (appShell != null && appShell.QaModeEnabled)
        {
            qaMode = true;
        }

        // Use one clean runtime headset panel and keep the old scene panels hidden.
        keepHeadsetControlsVisible = true;

        if (userHead == null && Camera.main != null)
        {
            userHead = Camera.main.transform;
        }

        if (userHead == null)
        {
            OVRCameraRig rig = FindObjectOfType<OVRCameraRig>(true);
            if (rig != null && rig.centerEyeAnchor != null)
            {
                userHead = rig.centerEyeAnchor;
            }
        }

        statsGazeExtractor = new GazeFeatureExtractor();
        NormalizePhase3Ui();
        EnsureHeadsetUiIsReadable();
        EnsurePersistentHeadsetControls();
        if (qaMode)
        {
            EnsurePersistentStatsPanel();
        }
        CacheEffects();
        ResetVisualState();
        KeepPersistentControlsVisible();
        if (qaMode)
        {
            UpdatePersistentStatsPanel();
        }

        if (appShell == null)
        {
            Debug.LogWarning("[Phase3] AdaptationManager could not find XRAppShellController.");
            return;
        }

        appShell.ContextSnapshotUpdated += OnContextSnapshotUpdated;
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
        RefreshComfortUiPlacement();
        KeepPersistentControlsVisible();
        HideLegacyPanelsWhenPersistentHeadsetUiIsUsed();
        if (qaMode)
        {
            UpdatePersistentStatsPanel();
        }
        else
        {
            HidePersistentStatsPanel();
        }
    }

    public void OnContinuePressed()
    {
        suppressUiUntilTime = Time.time + manualUiSuppressSeconds;
        idleChoicePanelLocked = false;
        restBreakActive = false;
        hasQueuedSnapshot = false;

        if (vignette != null)
        {
            vignette.active = false;
        }

        if (keepHeadsetControlsVisible)
        {
            HideLegacyPanelsWhenPersistentHeadsetUiIsUsed();
        }
        else if (currentTaskPanel != null)
        {
            currentTaskPanel.transform.position = lastTaskPanelPosition;
            currentTaskPanel.alpha = 1f;
            currentTaskPanel.gameObject.SetActive(true);
        }

        if (restPanel != null)
        {
            restPanel.SetActive(false);
        }

        if (restDimOverlay != null)
        {
            restDimOverlay.alpha = 0f;
            restDimOverlay.gameObject.SetActive(false);
        }
    }

    public void OnTakeBreakPressed()
    {
        suppressUiUntilTime = Time.time + manualUiSuppressSeconds;
        idleChoicePanelLocked = true;
        restBreakActive = true;

        if (keepHeadsetControlsVisible)
        {
            HideLegacyPanelsWhenPersistentHeadsetUiIsUsed();
            if (vignette != null)
            {
                vignette.active = true;
                vignette.intensity.Override(takeBreakVignetteIntensity);
            }

            return;
        }

        if (restDimOverlay != null)
        {
            restDimOverlay.gameObject.SetActive(true);
            restDimOverlay.alpha = Mathf.Clamp01(takeBreakDimAlpha);
        }

        if (restPanel != null)
        {
            restPanel.SetActive(true);
        }
    }

    public void OnRecenterViewPressed()
    {
        suppressUiUntilTime = Time.time + manualUiSuppressSeconds;
        if (userHead == null)
        {
            if (Camera.main != null)
            {
                userHead = Camera.main.transform;
            }
            else
            {
                OVRCameraRig rig = FindObjectOfType<OVRCameraRig>(true);
                if (rig != null && rig.centerEyeAnchor != null)
                {
                    userHead = rig.centerEyeAnchor;
                }
            }
        }

        if (uiAnchorRoot == null || userHead == null)
        {
            return;
        }

        uiAnchorRoot.position = userHead.position + userHead.forward * headsetUiDistance + userHead.up * headsetUiVerticalOffset;
        uiAnchorRoot.rotation = Quaternion.LookRotation(uiAnchorRoot.position - userHead.position);
        NormalizePhase3Ui();
        EnsureHeadsetUiIsReadable();
        KeepPersistentControlsVisible();
        HideLegacyPanelsWhenPersistentHeadsetUiIsUsed();
    }

    private void CacheEffects()
    {
        if (globalVolume == null || globalVolume.profile == null)
        {
            return;
        }

        globalVolume.profile.TryGet(out vignette);
    }

    private void OnContextSnapshotUpdated(XRContextSnapshot snapshot)
    {
        latestSnapshot = snapshot;
        ResolveContextSource();

        if (contextSource != null)
        {
            latestFrame = contextSource.LatestFrame;
        }

        if (idleChoicePanelLocked && !qaMode)
        {
            return;
        }

        if (!hasAppliedSnapshot)
        {
            ApplySnapshot(snapshot);
            return;
        }

        if (snapshot.state == activeState && snapshot.boundaryType == activeBoundaryType)
        {
            return;
        }

        if (Time.time < suppressUiUntilTime)
        {
            queuedSnapshot = snapshot;
            hasQueuedSnapshot = true;
            StartQueuedStateRoutineIfNeeded();
            return;
        }

        if (Time.time - activeStateStartedAt < GetMinimumVisibleSeconds(activeState))
        {
            queuedSnapshot = snapshot;
            hasQueuedSnapshot = true;
            StartQueuedStateRoutineIfNeeded();
            return;
        }

        ApplySnapshot(snapshot);
    }

    private IEnumerator OnContextStateChanged(ContextState newState, BoundaryType boundaryType)
    {
        switch (newState)
        {
            case ContextState.Distracted:
                yield return StartCoroutine(DispatchDistractedByBoundary(boundaryType));
                break;
            case ContextState.Transitioning:
                yield return StartCoroutine(DispatchTransitioningByBoundary(boundaryType));
                break;
            case ContextState.Engaged:
                yield return StartCoroutine(DispatchEngagedByBoundary(boundaryType));
                break;
            case ContextState.Idle:
                yield return StartCoroutine(DispatchIdleByBoundary(boundaryType));
                break;
            default:
                yield break;
        }
    }

    private IEnumerator DispatchDistractedByBoundary(BoundaryType boundaryType)
    {
        switch (boundaryType)
        {
            case BoundaryType.Stationary:
            case BoundaryType.Custom:
                yield return StartCoroutine(ShowVisualNudge());
                break;
            case BoundaryType.RoomScale:
                yield return StartCoroutine(PlaySpatialAudioCue());
                break;
            case BoundaryType.Passthrough:
                yield return StartCoroutine(ShowMinimalArOverlay());
                break;
            default:
                yield return StartCoroutine(ShowVisualNudge());
                break;
        }
    }

    private IEnumerator DispatchTransitioningByBoundary(BoundaryType boundaryType)
    {
        switch (boundaryType)
        {
            case BoundaryType.RoomScale:
                yield return StartCoroutine(MoveContentToGazeDirection());
                break;
            case BoundaryType.Stationary:
            case BoundaryType.Custom:
            case BoundaryType.Passthrough:
            default:
                yield return StartCoroutine(PreloadNextTaskPanel());
                break;
        }
    }

    private IEnumerator DispatchEngagedByBoundary(BoundaryType boundaryType)
    {
        yield return StartCoroutine(EngagedBehavior(boundaryType));
    }

    private IEnumerator DispatchIdleByBoundary(BoundaryType boundaryType)
    {
        yield return StartCoroutine(IdleBehavior(boundaryType));
    }

    private void ApplySnapshot(XRContextSnapshot snapshot)
    {
        hasAppliedSnapshot = true;
        activeState = snapshot.state;
        activeBoundaryType = snapshot.boundaryType;
        activeStateStartedAt = Time.time;

        if (snapshot.state != ContextState.Idle && !restBreakActive)
        {
            idleChoicePanelLocked = false;
        }

        if (activeBehavior != null)
        {
            StopCoroutine(activeBehavior);
            activeBehavior = null;
        }

        ResetVisualState();
        RefreshComfortUiPlacement();
        EnsureHeadsetUiIsReadable();
        KeepPersistentControlsVisible();
        if (qaMode)
        {
            EnsurePersistentStatsPanel();
            UpdatePersistentStatsPanel();
        }
        else
        {
            HidePersistentStatsPanel();
        }
        activeBehavior = StartCoroutine(RunBehavior(snapshot.state, snapshot.boundaryType));
        Debug.Log("[Phase3] Visual state => " + snapshot.state + " (" + snapshot.boundaryType + ")");
    }

    private IEnumerator RunBehavior(ContextState state, BoundaryType boundaryType)
    {
        yield return StartCoroutine(OnContextStateChanged(state, boundaryType));
        activeBehavior = null;
    }

    private void StartQueuedStateRoutineIfNeeded()
    {
        if (queuedStateRoutine == null)
        {
            queuedStateRoutine = StartCoroutine(ApplyQueuedSnapshotWhenReady());
        }
    }

    private IEnumerator ApplyQueuedSnapshotWhenReady()
    {
        while (hasQueuedSnapshot)
        {
            float holdRemaining = GetMinimumVisibleSeconds(activeState) - (Time.time - activeStateStartedAt);
            float suppressRemaining = suppressUiUntilTime - Time.time;
            float waitTime = Mathf.Max(holdRemaining, suppressRemaining);
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }

            if (!hasQueuedSnapshot)
            {
                break;
            }

            XRContextSnapshot snapshotToApply = queuedSnapshot;
            hasQueuedSnapshot = false;

            if (snapshotToApply.state != activeState || snapshotToApply.boundaryType != activeBoundaryType)
            {
                ApplySnapshot(snapshotToApply);
            }
        }

        queuedStateRoutine = null;
    }

    private float GetMinimumVisibleSeconds(ContextState state)
    {
        switch (state)
        {
            case ContextState.Engaged:
                return engagedMinVisibleSeconds;
            case ContextState.Distracted:
                return distractedMinVisibleSeconds;
            case ContextState.Transitioning:
                return transitioningMinVisibleSeconds;
            case ContextState.Idle:
            default:
                return idleMinVisibleSeconds;
        }
    }

    private IEnumerator EngagedBehavior(BoundaryType boundaryType)
    {
        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.Override(engagedVignetteIntensity);
        }

        if (boundaryType == BoundaryType.RoomScale && navigationHintsRoot != null)
        {
            navigationHintsRoot.SetActive(false);
        }

        yield break;
    }

    private IEnumerator ShowVisualNudge()
    {
        yield return new WaitForSeconds(1.5f);

        RefreshComfortUiPlacement();
        if (distractedEdgePanel != null)
        {
            distractedEdgePanel.gameObject.SetActive(true);
            distractedEdgePanel.alpha = 1f;
        }

        if (distractedArrow != null && taskAreaAnchor != null && userHead != null)
        {
            Vector3 flatDir = taskAreaAnchor.position - userHead.position;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude > 0.0001f)
            {
                distractedArrow.rotation = Quaternion.LookRotation(flatDir.normalized);
            }
        }
    }

    private IEnumerator PlaySpatialAudioCue()
    {
        yield return new WaitForSeconds(1.5f);

        if (spatialCueSource != null)
        {
            if (taskAreaAnchor != null)
            {
                spatialCueSource.transform.position = taskAreaAnchor.position;
            }

            spatialCueSource.spatialBlend = 1f;
            spatialCueSource.Play();
        }
    }

    private IEnumerator ShowMinimalArOverlay()
    {
        // Minimal AR overlay placeholder reuses the edge panel in passthrough mode.
        RefreshComfortUiPlacement();
        if (distractedEdgePanel != null)
        {
            distractedEdgePanel.gameObject.SetActive(true);
            distractedEdgePanel.alpha = 0.6f;
        }

        yield break;
    }

    private IEnumerator PreloadNextTaskPanel()
    {
        if (keepHeadsetControlsVisible)
        {
            yield break;
        }

        if (nextTaskPanel != null)
        {
            nextTaskPanel.gameObject.SetActive(true);
            nextTaskPanel.alpha = 0f;
        }
        
        if (currentTaskPanel != null)
        {
            lastTaskPanelPosition = currentTaskPanel.transform.position;
            currentTaskPanel.alpha = 0.7f;
        }

        if (nextTaskPanel != null)
        {
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                nextTaskPanel.alpha = Mathf.Lerp(0f, 1f, t / 0.4f);
                yield return null;
            }

            nextTaskPanel.alpha = 1f;
        }
    }

    private IEnumerator MoveContentToGazeDirection()
    {
        if (keepHeadsetControlsVisible)
        {
            yield break;
        }

        if (nextTaskPanel != null)
        {
            nextTaskPanel.gameObject.SetActive(true);
            nextTaskPanel.alpha = 0f;
        }

        if (nextTaskPanel != null && userHead != null)
        {
            Vector3 targetPos = userHead.position + userHead.forward * 2f;
            nextTaskPanel.transform.position = targetPos;
            nextTaskPanel.transform.rotation = Quaternion.LookRotation(nextTaskPanel.transform.position - userHead.position);
        }

        if (nextTaskPanel != null)
        {
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                nextTaskPanel.alpha = Mathf.Lerp(0f, 1f, t / 0.4f);
                yield return null;
            }

            nextTaskPanel.alpha = 1f;
        }
    }

    private IEnumerator IdleBehavior(BoundaryType boundaryType)
    {
        idleChoicePanelLocked = true;
        restBreakActive = false;
        RefreshComfortUiPlacement();
        KeepPersistentControlsVisible();

        HideCanvasGroup(currentTaskPanel);
        HideCanvasGroup(nextTaskPanel);
        if (distractedEdgePanel != null)
        {
            distractedEdgePanel.alpha = 0f;
            distractedEdgePanel.gameObject.SetActive(false);
        }
        if (restDimOverlay != null)
        {
            restDimOverlay.alpha = 0f;
            restDimOverlay.interactable = false;
            restDimOverlay.blocksRaycasts = false;
            restDimOverlay.gameObject.SetActive(false);
        }

        if (boundaryType == BoundaryType.RoomScale && floorArrow != null && userHead != null)
        {
            Transform nearest = FindNearestTaskObject(userHead.position);
            if (nearest != null)
            {
                floorArrow.SetActive(true);
                floorArrow.transform.position = userHead.position + Vector3.down * 0.8f;

                Vector3 dir = nearest.position - floorArrow.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    floorArrow.transform.rotation = Quaternion.LookRotation(dir.normalized);
                }
            }
        }

        yield break;
    }

    private Transform FindNearestTaskObject(Vector3 from)
    {
        if (taskObjects == null || taskObjects.Length == 0)
        {
            return null;
        }

        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (Transform t in taskObjects)
        {
            if (t == null)
            {
                continue;
            }

            float d = Vector3.SqrMagnitude(t.position - from);
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        return best;
    }

    private void ResetVisualState()
    {
        if (vignette != null)
        {
            vignette.active = false;
        }

        if (navigationHintsRoot != null)
        {
            navigationHintsRoot.SetActive(true);
        }

        if (distractedEdgePanel != null)
        {
            distractedEdgePanel.alpha = 0f;
            distractedEdgePanel.gameObject.SetActive(false);
        }

        bool isIdleState = activeState == ContextState.Idle;
        bool isTransitioningState = activeState == ContextState.Transitioning;

        if (currentTaskPanel != null)
        {
            if (keepHeadsetControlsVisible || idleChoicePanelLocked || isIdleState || !isTransitioningState)
            {
                currentTaskPanel.alpha = 0f;
                currentTaskPanel.gameObject.SetActive(false);
            }
            else
            {
                currentTaskPanel.alpha = 1f;
            }
        }

        if (nextTaskPanel != null)
        {
            if (keepHeadsetControlsVisible || idleChoicePanelLocked || isIdleState || !isTransitioningState)
            {
                nextTaskPanel.alpha = 0f;
                nextTaskPanel.gameObject.SetActive(false);
            }
        }

        if (restPanel != null)
        {
            restPanel.SetActive(!keepHeadsetControlsVisible && idleChoicePanelLocked);
        }

        if (restDimOverlay != null)
        {
            restDimOverlay.interactable = false;
            restDimOverlay.blocksRaycasts = false;
            restDimOverlay.alpha = restBreakActive ? Mathf.Clamp01(takeBreakDimAlpha) : 0f;
            restDimOverlay.gameObject.SetActive(restBreakActive);
        }

        if (floorArrow != null)
        {
            floorArrow.SetActive(false);
        }

        KeepPersistentControlsVisible();
        HideLegacyPanelsWhenPersistentHeadsetUiIsUsed();
    }

    private void HideLegacyPanelsWhenPersistentHeadsetUiIsUsed()
    {
        if (!keepHeadsetControlsVisible)
        {
            return;
        }

        HideCanvasGroup(currentTaskPanel);
        HideCanvasGroup(nextTaskPanel);

        if (restPanel != null)
        {
            restPanel.SetActive(false);
        }

        if (restDimOverlay != null)
        {
            restDimOverlay.alpha = 0f;
            restDimOverlay.interactable = false;
            restDimOverlay.blocksRaycasts = false;
            restDimOverlay.gameObject.SetActive(false);
        }

        if (floorArrow != null)
        {
            floorArrow.SetActive(false);
        }

        HideKnownLegacyPanelObject("CurrentTaskPanel");
        HideKnownLegacyPanelObject("CurrentTaskPanel ");
        HideKnownLegacyPanelObject("NextTaskPanel");
        HideKnownLegacyPanelObject("NextTaskPanel ");
        HideKnownLegacyPanelObject("RestPanel");
        HideKnownLegacyPanelObject("RestDimOverlay");
        HideLegacyUiUnderAnchor();
    }

    private static void HideCanvasGroup(CanvasGroup group)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        group.gameObject.SetActive(false);
    }

    private static void HideKnownLegacyPanelObject(string objectName)
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];
            if (t != null && string.Equals(t.name.Trim(), objectName.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private void HideLegacyUiUnderAnchor()
    {
        if (uiAnchorRoot == null)
        {
            return;
        }

        Transform[] children = uiAnchorRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child == uiAnchorRoot)
            {
                continue;
            }

            if (IsPersistentHeadsetUi(child))
            {
                continue;
            }

            if (distractedEdgePanel != null &&
                (child == distractedEdgePanel.transform || child.IsChildOf(distractedEdgePanel.transform)))
            {
                continue;
            }

            if (child.GetComponent<Canvas>() != null)
            {
                continue;
            }

            if (child.GetComponent<CanvasGroup>() != null || child.GetComponent<Image>() != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private bool IsPersistentHeadsetUi(Transform candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (persistentControlsRoot != null &&
            (candidate == persistentControlsRoot || candidate.IsChildOf(persistentControlsRoot)))
        {
            return true;
        }

        return persistentStatsRoot != null &&
               (candidate == persistentStatsRoot || candidate.IsChildOf(persistentStatsRoot));
    }

    private void RefreshComfortUiPlacement()
    {
        EnsureUiAnchorRoot();

        if (userHead == null && Camera.main != null)
        {
            userHead = Camera.main.transform;
        }

        if (uiAnchorRoot == null || userHead == null)
        {
            return;
        }

        Vector3 flatForward = Vector3.ProjectOnPlane(userHead.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.forward;
        }

        uiAnchorRoot.position = userHead.position + flatForward * headsetUiDistance + Vector3.up * headsetUiVerticalOffset;
        uiAnchorRoot.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

        PositionRuntimePanel(persistentControlsRoot, new Vector2(760f, 150f), persistentControlsLocalOffset);
        PositionRuntimePanel(persistentStatsRoot, new Vector2(860f, 330f), persistentStatsLocalOffset);
        NormalizePanelRect(distractedEdgePanel != null ? distractedEdgePanel.gameObject : null, new Vector2(280f, 160f), distractedPanelLocalOffset);
        NormalizePanelRect(restPanel, new Vector2(360f, 260f), restPanelLocalOffset);
    }

    private static void PositionRuntimePanel(RectTransform panel, Vector2 size, Vector3 localOffset)
    {
        if (panel == null)
        {
            return;
        }

        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = size;
        panel.anchoredPosition = new Vector2(localOffset.x, localOffset.y);
        panel.localPosition = localOffset;
        panel.localRotation = Quaternion.identity;
        panel.localScale = Vector3.one;
    }

    private void NormalizePhase3Ui()
    {
        EnsureUiAnchorRoot();

        if (uiAnchorRoot == null)
        {
            return;
        }

        if (userHead == null && Camera.main != null)
        {
            userHead = Camera.main.transform;
        }

        Canvas rootCanvas = uiAnchorRoot.GetComponent<Canvas>();
        if (rootCanvas == null)
        {
            rootCanvas = uiAnchorRoot.GetComponentInChildren<Canvas>(true);
        }

        if (rootCanvas != null)
        {
            rootCanvas.renderMode = RenderMode.WorldSpace;
            rootCanvas.worldCamera = Camera.main;
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = 50;

            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(900f, 600f);
            canvasRect.localScale = Vector3.one * uiCanvasScale;
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
        }

        if (userHead != null)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(userHead.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.forward;
            }

            uiAnchorRoot.position = userHead.position + flatForward * headsetUiDistance + Vector3.up * headsetUiVerticalOffset;
            uiAnchorRoot.rotation = Quaternion.LookRotation(uiAnchorRoot.position - userHead.position, Vector3.up);
        }
        uiAnchorRoot.position += Vector3.up * 0.02f;

        NormalizePanelRect(restPanel, new Vector2(360f, 240f), restPanelLocalOffset);
        NormalizePanelRect(currentTaskPanel != null ? currentTaskPanel.gameObject : null, new Vector2(420f, 220f), currentTaskPanelLocalOffset);
        NormalizePanelRect(nextTaskPanel != null ? nextTaskPanel.gameObject : null, new Vector2(420f, 220f), nextTaskPanelLocalOffset);
        NormalizePanelRect(distractedEdgePanel != null ? distractedEdgePanel.gameObject : null, new Vector2(280f, 160f), distractedPanelLocalOffset);
        HideLegacyPanelsWhenPersistentHeadsetUiIsUsed();

        if (restDimOverlay != null)
        {
            RectTransform dimRect = restDimOverlay.GetComponent<RectTransform>();
            if (dimRect != null)
            {
                dimRect.anchorMin = new Vector2(0.5f, 0.5f);
                dimRect.anchorMax = new Vector2(0.5f, 0.5f);
                dimRect.pivot = new Vector2(0.5f, 0.5f);
                dimRect.sizeDelta = new Vector2(460f, 260f);
                dimRect.anchoredPosition = Vector2.zero;
                dimRect.localPosition = new Vector3(0f, 0f, 0.08f);
                dimRect.anchorMin = new Vector2(0.5f, 0.5f);
                dimRect.anchorMax = new Vector2(0.5f, 0.5f);
                dimRect.pivot = new Vector2(0.5f, 0.5f);
            }
        }
    }

    private void EnsureUiAnchorRoot()
    {
        if (uiAnchorRoot != null)
        {
            return;
        }

        GameObject existing = GameObject.Find("UIAnchorRoot");
        if (existing == null)
        {
            existing = new GameObject("UIAnchorRoot");
        }

        uiAnchorRoot = existing.transform;
    }

    private void EnsureHeadsetUiIsReadable()
    {
        EnsureCanvasInputComponents();
        RepairCanvasGroups();
        RepairPanelBackground(restPanel);
        RepairPanelBackground(currentTaskPanel != null ? currentTaskPanel.gameObject : null);
        RepairPanelBackground(nextTaskPanel != null ? nextTaskPanel.gameObject : null);
        RepairPanelBackground(distractedEdgePanel != null ? distractedEdgePanel.gameObject : null);
        ApplyComfortableDimOverlayStyle();
        RepairText(restPanel);
        RepairText(currentTaskPanel != null ? currentTaskPanel.gameObject : null);
        RepairText(nextTaskPanel != null ? nextTaskPanel.gameObject : null);
        RepairText(distractedEdgePanel != null ? distractedEdgePanel.gameObject : null);
        RepairButtons(restPanel);
        ArrangeRestPanelLayout(restPanel);
    }

    private void EnsureCanvasInputComponents()
    {
        if (uiAnchorRoot == null)
        {
            return;
        }

        Canvas canvas = uiAnchorRoot.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Phase3_RuntimeHeadsetCanvas");
            canvasGo.transform.SetParent(uiAnchorRoot, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvasGo.AddComponent<CanvasScaler>();
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(900f, 600f);
        canvasRect.localScale = Vector3.one * uiCanvasScale;
        canvasRect.localPosition = Vector3.zero;
        canvasRect.localRotation = Quaternion.identity;
    }

    private void RepairCanvasGroups()
    {
        RepairCanvasGroup(restPanel);
        RepairCanvasGroup(currentTaskPanel != null ? currentTaskPanel.gameObject : null);
        RepairCanvasGroup(nextTaskPanel != null ? nextTaskPanel.gameObject : null);
        RepairCanvasGroup(distractedEdgePanel != null ? distractedEdgePanel.gameObject : null);
    }

    private static void RepairCanvasGroup(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    private void ApplyComfortableDimOverlayStyle()
    {
        if (restDimOverlay == null)
        {
            return;
        }

        Image dimImage = restDimOverlay.GetComponent<Image>();
        if (dimImage != null)
        {
            dimImage.color = new Color(0.02f, 0.03f, 0.05f, 0.98f);
            dimImage.raycastTarget = false;
        }
    }

    private static void RepairText(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            label.enabled = true;
            label.color = Color.white;
            label.fontSize = Mathf.Max(label.fontSize, 22f);
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            RectTransform rect = label.rectTransform;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 240f), Mathf.Max(rect.sizeDelta.y, 48f));
        }
    }

    private static void RepairPanelBackground(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Image image = root.GetComponent<Image>();
        if (image != null)
        {
            image.enabled = true;
            image.raycastTarget = false;
            image.color = new Color(0.02f, 0.03f, 0.05f, 0.82f);
        }
    }

    private static void RepairButtons(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 230f), Mathf.Max(rect.sizeDelta.y, 58f));
                rect.localScale = Vector3.one;
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
                button.targetGraphic = image;
            }

            if (image != null)
            {
                image.enabled = true;
                image.raycastTarget = true;
                image.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            HeadsetUiButton headsetButton = button.GetComponent<HeadsetUiButton>();
            if (headsetButton == null)
            {
                headsetButton = button.gameObject.AddComponent<HeadsetUiButton>();
            }

            headsetButton.Configure(button, label);
        }
    }

    private static void ArrangeRestPanelLayout(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        RectTransform panelRect = root.transform as RectTransform;
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(360f, 260f);
            panelRect.localScale = Vector3.one;
        }

        Image panelImage = root.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.015f, 0.02f, 0.03f, 0.88f);
        }

        Button continueButton = null;
        Button breakButton = null;
        Button recenterButton = null;
        Button[] buttons = root.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            string label = GetButtonLabel(button);
            if (label.Contains("continue"))
            {
                continueButton = button;
            }
            else if (label.Contains("break"))
            {
                breakButton = button;
            }
            else if (label.Contains("recenter"))
            {
                recenterButton = button;
            }
        }

        PositionRestButton(continueButton, new Vector2(0f, 62f));
        PositionRestButton(breakButton, new Vector2(0f, 0f));
        PositionRestButton(recenterButton, new Vector2(0f, -62f));
    }

    private static string GetButtonLabel(Button button)
    {
        if (button == null)
        {
            return string.Empty;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null && !string.IsNullOrWhiteSpace(label.text))
        {
            return label.text.Trim().ToLowerInvariant();
        }

        return button.gameObject.name.ToLowerInvariant();
    }

    private static void PositionRestButton(Button button, Vector2 anchoredPosition)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300f, 54f);
            rect.anchoredPosition = anchoredPosition;
            rect.localPosition = new Vector3(anchoredPosition.x, anchoredPosition.y, 0.02f);
            rect.localScale = Vector3.one;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = 23f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
        }
    }

    private void EnsurePersistentHeadsetControls()
    {
        if (!keepHeadsetControlsVisible)
        {
            return;
        }

        EnsureUiAnchorRoot();
        EnsureCanvasInputComponents();

        Canvas canvas = uiAnchorRoot.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        if (persistentControlsRoot == null)
        {
            Transform existing = canvas.transform.Find("Phase3_HeadsetControlsPanel");
            if (existing != null)
            {
                persistentControlsRoot = existing as RectTransform;
            }
        }

        if (persistentControlsRoot == null)
        {
            GameObject panelGo = new GameObject("Phase3_HeadsetControlsPanel");
            panelGo.transform.SetParent(canvas.transform, false);
            persistentControlsRoot = panelGo.AddComponent<RectTransform>();
            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.03f, 0.05f, 0.72f);
            panelImage.raycastTarget = false;
            panelGo.AddComponent<CanvasGroup>();

            CreatePersistentControlsTitle();
            CreatePersistentButton("Continue", new Vector2(-245f, -34f), OnContinuePressed);
            CreatePersistentButton("Take a break", new Vector2(0f, -34f), OnTakeBreakPressed);
            CreatePersistentButton("Recenter view", new Vector2(245f, -34f), OnRecenterViewPressed);
        }
        else if (persistentControlsRoot.Find("IdleActionsTitle") == null)
        {
            CreatePersistentControlsTitle();
        }

        persistentControlsRoot.anchorMin = new Vector2(0.5f, 0.5f);
        persistentControlsRoot.anchorMax = new Vector2(0.5f, 0.5f);
        persistentControlsRoot.pivot = new Vector2(0.5f, 0.5f);
        persistentControlsRoot.sizeDelta = new Vector2(760f, 150f);
        persistentControlsRoot.anchoredPosition = new Vector2(persistentControlsLocalOffset.x, persistentControlsLocalOffset.y);
        persistentControlsRoot.localPosition = new Vector3(persistentControlsLocalOffset.x, persistentControlsLocalOffset.y, persistentControlsLocalOffset.z);
        persistentControlsRoot.localRotation = Quaternion.identity;
        persistentControlsRoot.localScale = Vector3.one;
        NormalizePersistentControlButtons();
        KeepPersistentControlsVisible();
    }

    private void CreatePersistentControlsTitle()
    {
        if (persistentControlsRoot == null)
        {
            return;
        }

        GameObject titleGo = new GameObject("IdleActionsTitle");
        titleGo.transform.SetParent(persistentControlsRoot, false);
        TextMeshProUGUI title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "Adaptive support";
        title.fontSize = 24f;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.94f, 0.96f, 1f, 1f);
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;

        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(700f, 36f);
        titleRect.anchoredPosition = new Vector2(0f, 42f);
        titleRect.localScale = Vector3.one;
    }

    private void NormalizePersistentControlButtons()
    {
        if (persistentControlsRoot == null)
        {
            return;
        }

        Button[] buttons = persistentControlsRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            string label = GetButtonLabel(button);
            if (label.Contains("continue"))
            {
                PositionRuntimeButton(button, new Vector2(-245f, -34f));
            }
            else if (label.Contains("break"))
            {
                PositionRuntimeButton(button, new Vector2(0f, -34f));
            }
            else if (label.Contains("recenter"))
            {
                PositionRuntimeButton(button, new Vector2(245f, -34f));
            }
        }
    }

    private static void PositionRuntimeButton(Button button, Vector2 position)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.transform as RectTransform;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(220f, 58f);
            rect.anchoredPosition = position;
            rect.localPosition = new Vector3(position.x, position.y, 0.02f);
            rect.localScale = Vector3.one;
        }

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
            button.targetGraphic = image;
        }

        if (image != null)
        {
            image.enabled = true;
            image.color = new Color(0.07f, 0.10f, 0.15f, 0.96f);
            image.raycastTarget = true;
        }
    }

    private void CreatePersistentButton(string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonGo = new GameObject(label + " Button");
        buttonGo.transform.SetParent(persistentControlsRoot, false);

        RectTransform rect = buttonGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(230f, 58f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 24f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        HeadsetUiButton headsetButton = buttonGo.AddComponent<HeadsetUiButton>();
        headsetButton.Configure(button, text);
    }

    private void KeepPersistentControlsVisible()
    {
        if (!keepHeadsetControlsVisible || persistentControlsRoot == null)
        {
            return;
        }

        bool shouldShow = ShouldShowPersistentControls();
        persistentControlsRoot.gameObject.SetActive(shouldShow);
        CanvasGroup group = persistentControlsRoot.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = shouldShow ? 1f : 0f;
            group.interactable = shouldShow;
            group.blocksRaycasts = shouldShow;
        }
    }

    private bool ShouldShowPersistentControls()
    {
        return activeState == ContextState.Idle || idleChoicePanelLocked || restBreakActive;
    }

    private void EnsurePersistentStatsPanel()
    {
        EnsureUiAnchorRoot();
        EnsureCanvasInputComponents();

        Canvas canvas = uiAnchorRoot.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        if (persistentStatsRoot == null)
        {
            Transform existing = canvas.transform.Find("Phase3_HeadsetStatsPanel");
            if (existing != null)
            {
                persistentStatsRoot = existing as RectTransform;
                persistentStatsText = existing.GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (persistentStatsRoot == null)
        {
            GameObject panelGo = new GameObject("Phase3_HeadsetStatsPanel");
            panelGo.transform.SetParent(canvas.transform, false);
            persistentStatsRoot = panelGo.AddComponent<RectTransform>();

            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.03f, 0.05f, 0.82f);
            panelImage.raycastTarget = false;

            CanvasGroup group = panelGo.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject textGo = new GameObject("StatsText");
            textGo.transform.SetParent(panelGo.transform, false);
            persistentStatsText = textGo.AddComponent<TextMeshProUGUI>();
            persistentStatsText.color = Color.white;
            persistentStatsText.fontSize = 23f;
            persistentStatsText.alignment = TextAlignmentOptions.TopLeft;
            persistentStatsText.enableWordWrapping = true;
            persistentStatsText.raycastTarget = false;

            RectTransform textRect = persistentStatsText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 18f);
            textRect.offsetMax = new Vector2(-24f, -18f);
        }

        persistentStatsRoot.gameObject.SetActive(true);
        persistentStatsRoot.anchorMin = new Vector2(0.5f, 0.5f);
        persistentStatsRoot.anchorMax = new Vector2(0.5f, 0.5f);
        persistentStatsRoot.pivot = new Vector2(0.5f, 0.5f);
        persistentStatsRoot.sizeDelta = new Vector2(860f, 330f);
        persistentStatsRoot.anchoredPosition = new Vector2(persistentStatsLocalOffset.x, persistentStatsLocalOffset.y);
        persistentStatsRoot.localPosition = new Vector3(persistentStatsLocalOffset.x, persistentStatsLocalOffset.y, persistentStatsLocalOffset.z);
        persistentStatsRoot.localRotation = Quaternion.identity;
        persistentStatsRoot.localScale = Vector3.one;

        if (persistentStatsText != null)
        {
            persistentStatsText.fontSize = 23f;
            persistentStatsText.color = Color.white;
            persistentStatsText.alignment = TextAlignmentOptions.TopLeft;
            persistentStatsText.enableWordWrapping = true;
        }
    }

    private void HidePersistentStatsPanel()
    {
        if (persistentStatsRoot != null)
        {
            persistentStatsRoot.gameObject.SetActive(false);
            return;
        }

        if (uiAnchorRoot == null)
        {
            return;
        }

        Canvas canvas = uiAnchorRoot.GetComponentInChildren<Canvas>(true);
        Transform existing = canvas != null ? canvas.transform.Find("Phase3_HeadsetStatsPanel") : null;
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
        }
    }

    private void UpdatePersistentStatsPanel()
    {
        if (persistentStatsText == null)
        {
            EnsurePersistentStatsPanel();
        }

        if (persistentStatsText == null)
        {
            return;
        }

        ResolveContextSource();
        if (contextSource != null)
        {
            latestFrame = contextSource.LatestFrame;
        }

        string aoi = string.IsNullOrWhiteSpace(latestSnapshot.aoiHit) ? latestFrame.aoi_hit : latestSnapshot.aoiHit;
        if (string.IsNullOrWhiteSpace(aoi))
        {
            aoi = "none";
        }

        GazeFeatureVector gaze = GetLatestGazeFeatures();
        string posture = string.IsNullOrWhiteSpace(latestFrame.posture_class) ? latestSnapshot.postureMode.ToString() : latestFrame.posture_class;
        persistentStatsText.text =
            "STATE    " + latestSnapshot.state + "\n" +
            "CONF     " + (latestSnapshot.confidence * 100f).ToString("F0") + "%    BOUND  " + latestSnapshot.boundaryType + "\n" +
            "AOI      " + aoi + "\n" +
            "POSTURE  " + posture + "\n" +
            "FIX      " + latestFrame.fixation_duration_s.ToString("F2") + "s    DWELL  " + gaze.aoi_dwell_ratio.ToString("F2") + "\n" +
            "SACC     " + gaze.saccade_rate_per_s.ToString("F2") + "/s    BODY  " + latestFrame.avg_joint_velocity.ToString("F2") + "\n" +
            "HAND     " + latestFrame.interaction_count_10s + "    PINCH  " + latestFrame.left_pinch + "/" + latestFrame.right_pinch + "\n" +
            "DIST     " + latestFrame.nearest_object_dist_m.ToString("F2") + "m";
    }

    private GazeFeatureVector GetLatestGazeFeatures()
    {
        if (statsGazeExtractor == null)
        {
            statsGazeExtractor = new GazeFeatureExtractor();
        }

        return statsGazeExtractor.ExtractFeatures(latestFrame);
    }

    private void ResolveContextSource()
    {
        if (contextSource == null)
        {
            contextSource = FindObjectOfType<ContextDebugTester>(true);
        }
    }

    private static void NormalizePanelRect(GameObject panelObject, Vector2 size, Vector3 localPosition)
    {
        if (panelObject == null)
        {
            return;
        }

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        rect.localPosition = localPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }
}
