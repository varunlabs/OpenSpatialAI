using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class XRAppShellController : MonoBehaviour
{
    [Header("Runtime Mode")]
    [SerializeField] private bool qaMode = false;

    [SerializeField] private ContextDebugTester contextSource;
    [SerializeField] private SignalSynchroniser signalSynchroniser;
    [SerializeField] private HandCapture handCapture;
    [SerializeField] private GazeCapture gazeCapture;

    [Header("Runtime Bootstrap")]
    [SerializeField] private bool autoBootstrapHeadsetRig = true;
    [SerializeField] private bool autoBootstrapLiveContext = true;
    [SerializeField] private bool autoAlignTrainingSimulation = true;
    [SerializeField] private float trainingSceneDistanceFromHead = 2f;

    [Header("Fallback Testing (when Phase1 source is unavailable)")]
    [SerializeField] private bool enableFallbackSimulation = true;
    [SerializeField] private bool enableHeadsetFallbackControls = true;
    [SerializeField] private bool enableHeadsetQaOverride = true;
    [SerializeField] private float qaOverrideHoldSeconds = 8f;
    [SerializeField] private bool preferEngagedStateWhenFallbackStarts = true;
    [SerializeField] private ContextState simulatedState = ContextState.Idle;
    [SerializeField] private BoundaryType simulatedBoundary = BoundaryType.Stationary;
    [SerializeField] private PostureMode simulatedPosture = PostureMode.Sitting;
    [SerializeField, Range(0f, 1f)] private float simulatedConfidence = 0.8f;

    public event Action<XRContextSnapshot> ContextSnapshotUpdated;

    public XRContextSnapshot LatestSnapshot { get; private set; }
    public bool QaModeEnabled => qaMode;
    private bool isUsingFallback;
    private bool lastLeftPrimaryPressed;
    private bool lastRightPrimaryPressed;
    private bool lastLeftPinch;
    private bool lastRightPinch;
    private float qaOverrideUntilTime;

    private void Start()
    {
        ApplyRuntimeModeDefaults();
        EnsureHeadsetRigIfNeeded();
        EnsureTrainingSimulationUserGuide();
        StartCoroutine(AlignTrainingSimulationWhenReady());
        StartCoroutine(BindContextSourceWhenAvailable());
    }

    private void OnDestroy()
    {
        if (contextSource != null)
        {
            contextSource.ContextEvaluated -= OnContextEvaluated;
        }
    }

    private IEnumerator BindContextSourceWhenAvailable()
    {
        float elapsed = 0f;
        bool attemptedBootstrap = false;
        while (contextSource == null && elapsed < 3f)
        {
            contextSource = FindObjectOfType<ContextDebugTester>(true);
            if (contextSource == null)
            {
                if (autoBootstrapLiveContext && !attemptedBootstrap)
                {
                    attemptedBootstrap = true;
                    TryBootstrapLiveContext();
                }

                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
        }

        if (contextSource != null)
        {
            contextSource.ContextEvaluated -= OnContextEvaluated;
            contextSource.ContextEvaluated += OnContextEvaluated;
            isUsingFallback = false;
            qaOverrideUntilTime = 0f;
            Debug.Log("[Phase3] XRAppShellController bound to ContextDebugTester automatically.");
            yield break;
        }

        if (enableFallbackSimulation)
        {
            isUsingFallback = true;
            ApplyHeadsetFriendlyFallbackDefaults();
            Debug.LogWarning("[Phase3] ContextDebugTester not found. Using fallback simulation (keyboard in editor, controller/hand input in headset).");
            PublishSimulatedSnapshot();
            yield break;
        }

        isUsingFallback = false;
        Debug.LogError("[Phase3] ContextDebugTester not found and fallback is disabled. No context snapshots will be emitted.");
    }

    private void Update()
    {
        bool changed = false;
        if (qaMode && isUsingFallback)
        {
            changed |= HandleKeyboardFallbackControls();
        }

        // Only allow headset-side simulated state controls when the app is
        // actually running in fallback mode. In live context mode, pinch and
        // controller input belong to the task itself and must never override
        // real context inference.
        if (qaMode && isUsingFallback)
        {
            changed |= HandleHeadsetFallbackControls();
        }

        if (changed)
        {
            PublishSimulatedSnapshot();
        }
    }

    private void PublishSimulatedSnapshot()
    {
        ResolveRuntimeRefs();

        LatestSnapshot = new XRContextSnapshot
        {
            state = simulatedState,
            confidence = simulatedConfidence,
            boundaryType = simulatedBoundary,
            postureMode = simulatedPosture,
            timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            aoiHit = ResolveFallbackAoi()
        };

        ContextSnapshotUpdated?.Invoke(LatestSnapshot);
        Debug.Log("[Phase3] Simulated context => state=" + simulatedState + ", boundary=" + simulatedBoundary);
    }

    private void OnContextEvaluated(ContextResult result, SpatialContextVector spatial, SignalFrame frame)
    {
        if (IsQaOverrideActive())
        {
            return;
        }

        LatestSnapshot = new XRContextSnapshot
        {
            state = result.state,
            confidence = result.confidence,
            boundaryType = spatial.boundary_type,
            postureMode = spatial.posture_mode,
            timestampMs = frame.timestamp_ms,
            aoiHit = frame.aoi_hit
        };

        ContextSnapshotUpdated?.Invoke(LatestSnapshot);
    }

    private void TryBootstrapLiveContext()
    {
        EnsureHeadsetRigIfNeeded();
        ResolveRuntimeRefs();

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[Phase3] No Main Camera found. Live context bootstrap skipped.");
            return;
        }

        EnsureComponent<GazeCapture>(mainCamera.gameObject);
        EnsureComponent<BodyPoseCapture>(mainCamera.gameObject);
        EnsureComponent<SpatialContextDetector>(mainCamera.gameObject);
        EnsureRuntimeHandTrackingObjects();
        EnsureComponent<HandCapture>(mainCamera.gameObject);

        signalSynchroniser = FindObjectOfType<SignalSynchroniser>(true);
        if (signalSynchroniser == null)
        {
            GameObject signalGo = new GameObject("Phase3_RuntimeSignals");
            signalSynchroniser = signalGo.AddComponent<SignalSynchroniser>();
        }

        contextSource = FindObjectOfType<ContextDebugTester>(true);
        if (contextSource == null)
        {
            GameObject contextGo = new GameObject("Phase3_RuntimeContextSource");
            contextSource = contextGo.AddComponent<ContextDebugTester>();
        }
    }

    private void EnsureTrainingSimulationUserGuide()
    {
        if (SceneManager.GetActiveScene().name != "TrainingSimulation")
        {
            return;
        }

        if (FindObjectOfType<TrainingSimulationUserGuide>(true) != null)
        {
            return;
        }

        GameObject guideGo = new GameObject("Phase3_UserGuide");
        guideGo.AddComponent<TrainingSimulationUserGuide>();
    }

    private void EnsureHeadsetRigIfNeeded()
    {
        if (!autoBootstrapHeadsetRig)
        {
            return;
        }

        if (FindObjectOfType<OVRCameraRig>(true) != null)
        {
            ConfigureOvrManager(FindObjectOfType<OVRManager>(true));
            return;
        }

        GameObject existingMainCamera = Camera.main != null ? Camera.main.gameObject : GameObject.Find("Main Camera");
        Vector3 spawnPosition = existingMainCamera != null ? existingMainCamera.transform.position : Vector3.zero;
        Quaternion spawnRotation = existingMainCamera != null ? existingMainCamera.transform.rotation : Quaternion.identity;

        GameObject rigRoot = new GameObject("Phase3_OVRCameraRig");
        rigRoot.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        OVRManager manager = rigRoot.AddComponent<OVRManager>();
        ConfigureOvrManager(manager);
        rigRoot.AddComponent<OVRCameraRig>();

        if (existingMainCamera != null)
        {
            existingMainCamera.tag = "Untagged";
            existingMainCamera.SetActive(false);
        }

        Debug.Log("[Phase3] Bootstrapped OVRCameraRig for TrainingSimulation.");
    }

    private static void ConfigureOvrManager(OVRManager manager)
    {
        if (manager == null)
        {
            return;
        }

        manager.launchSimultaneousHandsControllersOnStartup = true;
        manager.controllerDrivenHandPosesType = OVRManager.ControllerDrivenHandPosesType.Natural;
    }

    private static void EnsureRuntimeHandTrackingObjects()
    {
        bool hasLeft = false;
        bool hasRight = false;
        OVRHand[] existingHands = FindObjectsOfType<OVRHand>(true);

        for (int i = 0; i < existingHands.Length; i++)
        {
            OVRHand hand = existingHands[i];
            if (hand == null)
            {
                continue;
            }

            string objectName = hand.gameObject.name.ToLowerInvariant();
            hasLeft |= objectName.Contains("left");
            hasRight |= objectName.Contains("right");
        }

        Transform parent = ResolveHandTrackingParent();
        if (!hasLeft)
        {
            CreateRuntimeHand("Phase3_LeftHandAnchor", true, parent);
        }

        if (!hasRight)
        {
            CreateRuntimeHand("Phase3_RightHandAnchor", false, parent);
        }
    }

    private static Transform ResolveHandTrackingParent()
    {
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>(true);
        if (rig != null)
        {
            return rig.transform;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }

    private static void CreateRuntimeHand(string objectName, bool left, Transform parent)
    {
        GameObject handGo = new GameObject(objectName);
        if (parent != null)
        {
            handGo.transform.SetParent(parent, false);
        }

        OVRHand hand = handGo.AddComponent<OVRHand>();
        SetOvrHandType(hand, left);
        Debug.Log("[Phase3] Bootstrapped " + objectName + " for real hand pinch tracking.");
    }

    private static void SetOvrHandType(OVRHand hand, bool left)
    {
        if (hand == null)
        {
            return;
        }

        System.Reflection.FieldInfo field = typeof(OVRHand).GetField(
            "HandType",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (field != null)
        {
            field.SetValue(hand, left ? OVRHand.Hand.HandLeft : OVRHand.Hand.HandRight);
        }
    }

    private void ResolveRuntimeRefs()
    {
        if (signalSynchroniser == null)
        {
            signalSynchroniser = FindObjectOfType<SignalSynchroniser>(true);
        }

        if (handCapture == null)
        {
            handCapture = FindObjectOfType<HandCapture>(true);
        }

        if (gazeCapture == null)
        {
            gazeCapture = FindObjectOfType<GazeCapture>(true);
        }
    }

    private void ApplyHeadsetFriendlyFallbackDefaults()
    {
        if (Application.isEditor || !preferEngagedStateWhenFallbackStarts)
        {
            return;
        }

        simulatedState = ContextState.Engaged;
    }

    private void ApplyRuntimeModeDefaults()
    {
        if (qaMode)
        {
            return;
        }

        // Production mode: never allow simulated-state overrides from controller/hand input.
        enableFallbackSimulation = false;
        enableHeadsetFallbackControls = false;
        enableHeadsetQaOverride = false;
        preferEngagedStateWhenFallbackStarts = false;
    }

    private bool HandleKeyboardFallbackControls()
    {
        bool changed = false;

        if (Input.GetKeyDown(KeyCode.Alpha1)) { simulatedState = ContextState.Engaged; changed = true; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { simulatedState = ContextState.Distracted; changed = true; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { simulatedState = ContextState.Transitioning; changed = true; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { simulatedState = ContextState.Idle; changed = true; }

        if (Input.GetKeyDown(KeyCode.Q)) { simulatedBoundary = BoundaryType.Stationary; changed = true; }
        if (Input.GetKeyDown(KeyCode.W)) { simulatedBoundary = BoundaryType.RoomScale; changed = true; }
        if (Input.GetKeyDown(KeyCode.E)) { simulatedBoundary = BoundaryType.Custom; changed = true; }
        if (Input.GetKeyDown(KeyCode.R)) { simulatedBoundary = BoundaryType.Passthrough; changed = true; }

        return changed;
    }

    private bool HandleHeadsetFallbackControls()
    {
        if (!enableHeadsetFallbackControls && !enableHeadsetQaOverride)
        {
            return false;
        }

        ResolveRuntimeRefs();
        bool changed = false;

        bool leftPrimaryPressed = GetPrimaryButton(XRNode.LeftHand);
        bool rightPrimaryPressed = GetPrimaryButton(XRNode.RightHand);

        if (leftPrimaryPressed && !lastLeftPrimaryPressed)
        {
            AdvanceSimulatedState();
            BeginQaOverride();
            changed = true;
        }

        if (rightPrimaryPressed && !lastRightPrimaryPressed)
        {
            AdvanceSimulatedBoundary();
            BeginQaOverride();
            changed = true;
        }

        lastLeftPrimaryPressed = leftPrimaryPressed;
        lastRightPrimaryPressed = rightPrimaryPressed;

        bool leftPinch = handCapture != null && handCapture.left_pinch;
        bool rightPinch = handCapture != null && handCapture.right_pinch;

        if (rightPinch && !lastRightPinch)
        {
            AdvanceSimulatedState();
            BeginQaOverride();
            changed = true;
        }

        if (leftPinch && !lastLeftPinch)
        {
            AdvanceSimulatedBoundary();
            BeginQaOverride();
            changed = true;
        }

        lastLeftPinch = leftPinch;
        lastRightPinch = rightPinch;

        return changed;
    }

    private void BeginQaOverride()
    {
        if (!enableHeadsetQaOverride)
        {
            return;
        }

        qaOverrideUntilTime = Time.time + qaOverrideHoldSeconds;
    }

    private bool IsQaOverrideActive()
    {
        return enableHeadsetQaOverride && Time.time < qaOverrideUntilTime;
    }

    private void AdvanceSimulatedState()
    {
        switch (simulatedState)
        {
            case ContextState.Engaged:
                simulatedState = ContextState.Distracted;
                break;
            case ContextState.Distracted:
                simulatedState = ContextState.Transitioning;
                break;
            case ContextState.Transitioning:
                simulatedState = ContextState.Idle;
                break;
            case ContextState.Idle:
            default:
                simulatedState = ContextState.Engaged;
                break;
        }
    }

    private void AdvanceSimulatedBoundary()
    {
        switch (simulatedBoundary)
        {
            case BoundaryType.Stationary:
                simulatedBoundary = BoundaryType.RoomScale;
                break;
            case BoundaryType.RoomScale:
                simulatedBoundary = BoundaryType.Custom;
                break;
            case BoundaryType.Custom:
                simulatedBoundary = BoundaryType.Passthrough;
                break;
            case BoundaryType.Passthrough:
            default:
                simulatedBoundary = BoundaryType.Stationary;
                break;
        }
    }

    private static bool GetPrimaryButton(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
        {
            return false;
        }

        return device.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed) && pressed;
    }

    private string ResolveFallbackAoi()
    {
        if (gazeCapture != null && !string.IsNullOrWhiteSpace(gazeCapture.aoi_hit))
        {
            return gazeCapture.aoi_hit;
        }

        return "simulation";
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        if (existing != null)
        {
            return existing;
        }

        return target.AddComponent<T>();
    }

    private IEnumerator AlignTrainingSimulationWhenReady()
    {
        if (!autoAlignTrainingSimulation || SceneManager.GetActiveScene().name != "TrainingSimulation")
        {
            yield break;
        }

        float elapsed = 0f;
        while (Camera.main == null && elapsed < 3f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            yield break;
        }

        Transform head = mainCamera.transform;
        Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.forward;
        }

        Quaternion yaw = Quaternion.LookRotation(flatForward, Vector3.up);
        float estimatedFloorY = Mathf.Max(0f, head.position.y - 1.6f);
        Vector3 taskAnchorPosition = new Vector3(
            head.position.x + flatForward.x * trainingSceneDistanceFromHead,
            estimatedFloorY,
            head.position.z + flatForward.z * trainingSceneDistanceFromHead);

        AlignTransform("TaskAreaAnchor", taskAnchorPosition + new Vector3(0f, 0.85f, 0f), yaw, Vector3.one);
        AlignTransform("table_surface", taskAnchorPosition + yaw * new Vector3(0f, 0.75f, 0f), yaw, new Vector3(1.8f, 0.08f, 1.2f));
        AlignTransform("task_cube_1", taskAnchorPosition + yaw * new Vector3(-0.5f, 0.88f, -0.25f), yaw, new Vector3(0.16f, 0.16f, 0.16f));
        AlignTransform("task_cylinder_1", taskAnchorPosition + yaw * new Vector3(0f, 0.90f, -0.25f), yaw, new Vector3(0.14f, 0.12f, 0.14f));
        AlignTransform("task_sphere_1", taskAnchorPosition + yaw * new Vector3(0.5f, 0.88f, -0.25f), yaw, new Vector3(0.16f, 0.16f, 0.16f));
        AlignTransform("receptacle_a", taskAnchorPosition + yaw * new Vector3(-0.5f, 0.82f, 0.35f), yaw, new Vector3(0.22f, 0.06f, 0.22f));
        AlignTransform("receptacle_b", taskAnchorPosition + yaw * new Vector3(0f, 0.82f, 0.35f), yaw, new Vector3(0.22f, 0.06f, 0.22f));
        AlignTransform("receptacle_c", taskAnchorPosition + yaw * new Vector3(0.5f, 0.82f, 0.35f), yaw, new Vector3(0.22f, 0.06f, 0.22f));
        AlignTransform("instruction_panel", taskAnchorPosition + yaw * new Vector3(0f, 1.30f, 0.80f), yaw * Quaternion.Euler(0f, 180f, 0f), new Vector3(0.9f, 0.45f, 1f), true);
        AlignTransform("UIAnchorRoot", head.position + flatForward * 1.5f, Quaternion.LookRotation(flatForward, Vector3.up), Vector3.one);
    }

    private static void AlignTransform(string objectName, Vector3 position, Quaternion rotation, Vector3 scale, bool forceActive = false)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null)
        {
            return;
        }

        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;

        if (forceActive && !go.activeSelf)
        {
            go.SetActive(true);
        }
    }
}
