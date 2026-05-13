using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainingSimulationUserGuide : MonoBehaviour
{
    [SerializeField] private XRAppShellController appShell;
    [SerializeField] private HandCapture handCapture;
    [SerializeField] private Transform userHead;
    [SerializeField] private float guideDistanceFromHead = 1.75f;
    [SerializeField] private float guideHeightOffset = 0.34f;
    [SerializeField] private float guideSideOffset = 0.36f;
    [SerializeField] private float carryDistanceFromHead = 1.15f;
    [SerializeField] private float carryHeightOffset = -0.18f;
    [SerializeField] private float dropHeightAbovePad = 0.14f;
    [SerializeField] private float placementAssistAngleDegrees = 24f;
    [SerializeField] private float placementAssistMaxDistance = 3.5f;
    [SerializeField] private bool requireOnboardingConfirmation = true;

    private readonly Dictionary<string, ObjectVisual> visuals = new Dictionary<string, ObjectVisual>();
    private readonly Dictionary<string, SortableTaskObject> taskObjects = new Dictionary<string, SortableTaskObject>();
    private readonly Dictionary<string, ReceptacleTrigger> receptacleTriggers = new Dictionary<string, ReceptacleTrigger>();
    private readonly Dictionary<string, string> objectToReceptacle = new Dictionary<string, string>
    {
        { "task_cube_1", "receptacle_a" },
        { "task_cylinder_1", "receptacle_b" },
        { "task_sphere_1", "receptacle_c" }
    };

    private readonly HashSet<string> completedObjects = new HashSet<string>();

    private Canvas guideCanvas;
    private Image stateAccentImage;
    private TMP_Text titleText;
    private TMP_Text statusText;
    private TMP_Text bodyText;
    private TMP_Text evidenceText;
    private TMP_Text completionBadgeText;
    private TMP_Text completionMetricsText;
    private TMP_Text completionProofText;
    private Button nextButton;
    private TMP_Text nextButtonText;
    private Canvas objectHintCanvas;
    private TMP_Text objectHintText;
    private Image objectHintImage;
    private Canvas padHintCanvas;
    private TMP_Text padHintText;
    private Image padHintImage;
    private ContextDebugTester contextSource;
    private string currentAoi = "none";
    private ContextState currentState = ContextState.Idle;
    private string selectedObjectName;
    private Vector3 selectedObjectStartPosition;
    private Quaternion selectedObjectStartRotation;
    private Collider[] selectedObjectColliders;
    private bool wasPinching;
    private float overrideMessageUntil;
    private string pinnedMessage;
    private SignalFrame latestFrame;
    private int onboardingStepIndex;
    private bool hasReceivedSnapshot;
    private int stateTransitionCount;
    private readonly HashSet<ContextState> observedStates = new HashSet<ContextState>();
    private const int OnboardingStepCount = 4;

    private struct ObjectVisual
    {
        public Transform transform;
        public Renderer renderer;
        public Material material;
        public Color baseColor;
        public Vector3 baseScale;
    }

    private void Start()
    {
        ResolveReferences();
        RegisterObjects();
        RegisterPlacementEvents();
        EnsureGuideCanvas();
        UpdateGuideText();

        if (appShell != null)
        {
            appShell.ContextSnapshotUpdated += OnContextSnapshotUpdated;
        }
    }

    private void OnDestroy()
    {
        if (appShell != null)
        {
            appShell.ContextSnapshotUpdated -= OnContextSnapshotUpdated;
        }

        ReceptacleTrigger[] receptacles = FindObjectsOfType<ReceptacleTrigger>(true);
        foreach (ReceptacleTrigger receptacle in receptacles)
        {
            if (receptacle != null)
            {
                receptacle.OnCorrectPlacement.RemoveListener(OnCorrectPlacement);
            }
        }
    }

    private void LateUpdate()
    {
        ResolveReferences();
        PositionGuideInView();
        UpdateSpatialHints();
        RefreshObjectHighlights();
        UpdateGuideText();
    }

    private void ResolveReferences()
    {
        if (appShell == null)
        {
            appShell = FindObjectOfType<XRAppShellController>(true);
        }

        if (userHead == null && Camera.main != null)
        {
            userHead = Camera.main.transform;
        }

        if (handCapture == null)
        {
            handCapture = FindObjectOfType<HandCapture>(true);
        }

        if (contextSource == null)
        {
            contextSource = FindObjectOfType<ContextDebugTester>(true);
        }
    }

    private void RegisterObjects()
    {
        RegisterVisual("task_cube_1", new Color(0.16f, 0.42f, 1.00f));
        RegisterVisual("task_cylinder_1", new Color(1.00f, 0.62f, 0.16f));
        RegisterVisual("task_sphere_1", new Color(0.12f, 0.82f, 0.58f));
        RegisterVisual("receptacle_a", new Color(0.10f, 0.24f, 0.58f));
        RegisterVisual("receptacle_b", new Color(0.56f, 0.32f, 0.08f));
        RegisterVisual("receptacle_c", new Color(0.06f, 0.42f, 0.30f));
    }

    private void RegisterVisual(string objectName, Color color)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null)
        {
            return;
        }

        SortableTaskObject taskObject = go.GetComponent<SortableTaskObject>();
        if (taskObject != null)
        {
            taskObjects[objectName] = taskObject;
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = go.GetComponentInChildren<Renderer>(true);
        }

        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;
        ApplyMaterialColor(material, color);

        visuals[objectName] = new ObjectVisual
        {
            transform = go.transform,
            renderer = renderer,
            material = material,
            baseColor = color,
            baseScale = go.transform.localScale
        };
    }

    private void RegisterPlacementEvents()
    {
        ReceptacleTrigger[] receptacles = FindObjectsOfType<ReceptacleTrigger>(true);
        foreach (ReceptacleTrigger receptacle in receptacles)
        {
            if (receptacle != null)
            {
                receptacle.OnCorrectPlacement.AddListener(OnCorrectPlacement);
                receptacleTriggers[receptacle.ReceptacleName] = receptacle;
            }
        }
    }

    private void OnCorrectPlacement(string objectName, string receptacleName)
    {
        completedObjects.Add(objectName);
        SetCompletedColor(objectName);
        SetCompletedColor(receptacleName);
        if (completedObjects.Count >= objectToReceptacle.Count)
        {
            pinnedMessage = null;
            overrideMessageUntil = 0f;
            UpdateGuideText();
            return;
        }

        ShowPinnedMessage("Placed " + FriendlyName(objectName) + ". Continue with the next object.", 2.5f);
    }

    private void OnContextSnapshotUpdated(XRContextSnapshot snapshot)
    {
        if (hasReceivedSnapshot && snapshot.state != currentState)
        {
            stateTransitionCount++;
        }

        hasReceivedSnapshot = true;
        currentAoi = string.IsNullOrWhiteSpace(snapshot.aoiHit) ? "none" : snapshot.aoiHit;
        currentState = snapshot.state;
        observedStates.Add(snapshot.state);

        if (contextSource != null)
        {
            latestFrame = contextSource.LatestFrame;
        }

        UpdateGuideText();
    }

    private void RefreshObjectHighlights()
    {
        HandlePinchSortingInteraction();

        foreach (KeyValuePair<string, ObjectVisual> kvp in visuals)
        {
            string key = kvp.Key;
            ObjectVisual visual = kvp.Value;
            if (visual.transform == null || visual.material == null)
            {
                continue;
            }

            if (completedObjects.Contains(key))
            {
                ApplyMaterialColor(visual.material, new Color(0.18f, 0.72f, 0.28f));
                visual.transform.localScale = Vector3.Lerp(visual.transform.localScale, visual.baseScale, 0.2f);
                continue;
            }

            bool isSelected = string.Equals(key, selectedObjectName, System.StringComparison.OrdinalIgnoreCase);
            bool isCurrent = string.Equals(key, currentAoi, System.StringComparison.OrdinalIgnoreCase);
            bool isMatchingReceptacle = objectToReceptacle.TryGetValue(currentAoi, out string receptacle) &&
                                        string.Equals(key, receptacle, System.StringComparison.OrdinalIgnoreCase);
            bool isSelectedMatchingReceptacle = objectToReceptacle.TryGetValue(selectedObjectName ?? string.Empty, out string selectedReceptacle) &&
                                                string.Equals(key, selectedReceptacle, System.StringComparison.OrdinalIgnoreCase);

            if (isSelected || isCurrent || isMatchingReceptacle || isSelectedMatchingReceptacle)
            {
                ApplyMaterialColor(visual.material, Color.Lerp(visual.baseColor, Color.white, 0.45f));
                visual.transform.localScale = Vector3.Lerp(visual.transform.localScale, visual.baseScale * 1.12f, 0.25f);
            }
            else
            {
                ApplyMaterialColor(visual.material, visual.baseColor);
                visual.transform.localScale = Vector3.Lerp(visual.transform.localScale, visual.baseScale, 0.2f);
            }
        }
    }

    private void HandlePinchSortingInteraction()
    {
        bool isPinching = handCapture != null && (handCapture.left_pinch || handCapture.right_pinch);
        bool pinchStarted = isPinching && !wasPinching;
        bool pinchReleased = !isPinching && wasPinching;

        if (IsOnboardingActive())
        {
            if (pinchStarted)
            {
                AdvanceOnboarding();
            }

            wasPinching = isPinching;
            return;
        }

        if (pinchStarted && string.IsNullOrEmpty(selectedObjectName))
        {
            TryBeginCarry(currentAoi);
        }

        if (isPinching && !string.IsNullOrEmpty(selectedObjectName))
        {
            UpdateCarriedObjectPosition();
        }

        if (pinchReleased && !string.IsNullOrEmpty(selectedObjectName))
        {
            TryDropCarriedObject();
        }

        wasPinching = isPinching;
    }

    private void TryBeginCarry(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName) ||
            completedObjects.Contains(objectName) ||
            !taskObjects.TryGetValue(objectName, out SortableTaskObject taskObject) ||
            taskObject == null)
        {
            return;
        }

        string activeObject = GetNextIncompleteObject();
        if (!string.IsNullOrEmpty(activeObject) &&
            !string.Equals(objectName, activeObject, System.StringComparison.OrdinalIgnoreCase))
        {
            ShowPinnedMessage(
                "Incorrect object selected.\n" +
                FriendlyName(objectName, capitalize: true) + " selected while " +
                FriendlyName(activeObject) + " is the active task. Please select the " +
                FriendlyName(activeObject) + ".",
                3f);
            return;
        }

        selectedObjectName = objectName;
        selectedObjectStartPosition = taskObject.transform.position;
        selectedObjectStartRotation = taskObject.transform.rotation;
        selectedObjectColliders = taskObject.GetComponentsInChildren<Collider>(true);
        SetSelectedCollidersEnabled(false);
        ShowPinnedMessage("Holding " + FriendlyName(objectName) + ". Aim at the highlighted " + FriendlyName(objectToReceptacle[objectName]) + " and release pinch.", 2f);
    }

    private void UpdateCarriedObjectPosition()
    {
        if (string.IsNullOrEmpty(selectedObjectName) ||
            !visuals.TryGetValue(selectedObjectName, out ObjectVisual visual) ||
            visual.transform == null ||
            userHead == null)
        {
            return;
        }

        Vector3 targetPosition = userHead.position +
                                 userHead.forward.normalized * carryDistanceFromHead +
                                 Vector3.up * carryHeightOffset;
        visual.transform.position = Vector3.Lerp(visual.transform.position, targetPosition, 0.45f);
        visual.transform.rotation = Quaternion.Slerp(visual.transform.rotation, userHead.rotation, 0.2f);
    }

    private void TryDropCarriedObject()
    {
        string objectName = selectedObjectName;
        selectedObjectName = null;
        SetSelectedCollidersEnabled(true);

        if (string.IsNullOrEmpty(objectName) || !taskObjects.TryGetValue(objectName, out SortableTaskObject taskObject))
        {
            return;
        }

        if (objectToReceptacle.TryGetValue(objectName, out string expectedReceptacleName) &&
            IsExpectedReceptacleTargeted(expectedReceptacleName) &&
            receptacleTriggers.TryGetValue(expectedReceptacleName, out ReceptacleTrigger receptacle) &&
            receptacle != null &&
            receptacle.TryAcceptPlacement(taskObject))
        {
            SnapObjectToReceptacle(taskObject.transform, receptacle.transform);
            return;
        }

        if (IsKnownReceptacle(currentAoi))
        {
            ShowPinnedMessage("Wrong pad. Aim at the highlighted " + FriendlyName(expectedReceptacleName) + " and release again.", 2.5f);
        }
        else
        {
            ShowPinnedMessage("Not placed. Pick up " + FriendlyName(objectName) + " again, aim at the highlighted " + FriendlyName(expectedReceptacleName) + ", then release.", 3f);
        }

        taskObject.transform.position = selectedObjectStartPosition;
        taskObject.transform.rotation = selectedObjectStartRotation;
    }

    private bool IsExpectedReceptacleTargeted(string expectedReceptacleName)
    {
        if (string.Equals(currentAoi, expectedReceptacleName, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (userHead == null ||
            !receptacleTriggers.TryGetValue(expectedReceptacleName, out ReceptacleTrigger receptacle) ||
            receptacle == null)
        {
            return false;
        }

        Vector3 toPad = receptacle.transform.position - userHead.position;
        float distance = toPad.magnitude;
        if (distance <= 0.001f || distance > placementAssistMaxDistance)
        {
            return false;
        }

        float angle = Vector3.Angle(userHead.forward, toPad.normalized);
        return angle <= placementAssistAngleDegrees;
    }

    private void SnapObjectToReceptacle(Transform objectTransform, Transform receptacleTransform)
    {
        if (objectTransform == null || receptacleTransform == null)
        {
            return;
        }

        objectTransform.position = receptacleTransform.position + Vector3.up * dropHeightAbovePad;
        objectTransform.rotation = receptacleTransform.rotation;
    }

    private void SetSelectedCollidersEnabled(bool enabled)
    {
        if (selectedObjectColliders == null)
        {
            return;
        }

        for (int i = 0; i < selectedObjectColliders.Length; i++)
        {
            if (selectedObjectColliders[i] != null)
            {
                selectedObjectColliders[i].enabled = enabled;
            }
        }
    }

    private bool IsKnownReceptacle(string aoiName)
    {
        if (string.IsNullOrWhiteSpace(aoiName))
        {
            return false;
        }

        return string.Equals(aoiName, "receptacle_a", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(aoiName, "receptacle_b", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(aoiName, "receptacle_c", System.StringComparison.OrdinalIgnoreCase);
    }

    private void SetCompletedColor(string objectName)
    {
        if (!visuals.TryGetValue(objectName, out ObjectVisual visual) || visual.material == null)
        {
            return;
        }

        ApplyMaterialColor(visual.material, new Color(0.18f, 0.72f, 0.28f));
    }

    private void EnsureGuideCanvas()
    {
        if (guideCanvas != null)
        {
            return;
        }

        GameObject canvasGo = new GameObject("TrainingSimulation_UserGuideCanvas");
        guideCanvas = canvasGo.AddComponent<Canvas>();
        guideCanvas.renderMode = RenderMode.WorldSpace;
        guideCanvas.worldCamera = Camera.main;
        guideCanvas.overrideSorting = true;
        guideCanvas.sortingOrder = 60;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = guideCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(760f, 500f);
        canvasRect.localScale = Vector3.one * 0.0011f;

        GameObject panelGo = new GameObject("GuidePanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.03f, 0.04f, 0.78f);
        panelImage.raycastTarget = false;

        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject accentGo = new GameObject("StateAccent");
        accentGo.transform.SetParent(panelGo.transform, false);
        stateAccentImage = accentGo.AddComponent<Image>();
        stateAccentImage.raycastTarget = false;
        RectTransform accentRect = stateAccentImage.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(0f, 8f);
        accentRect.anchoredPosition = Vector2.zero;

        titleText = CreateText("Title", panelGo.transform, new Vector2(34f, -26f), new Vector2(690f, 48f), 32f);
        titleText.fontStyle = FontStyles.Bold;
        statusText = CreateText("Status", panelGo.transform, new Vector2(34f, -82f), new Vector2(690f, 42f), 22f);
        bodyText = CreateText("Body", panelGo.transform, new Vector2(34f, -140f), new Vector2(690f, 225f), 24f);
        evidenceText = CreateText("Evidence", panelGo.transform, new Vector2(34f, -372f), new Vector2(480f, 104f), 19f);
        evidenceText.color = new Color(0.78f, 0.84f, 0.90f, 1f);
        completionBadgeText = CreateText("CompletionBadge", panelGo.transform, new Vector2(34f, -128f), new Vector2(690f, 74f), 30f);
        completionBadgeText.fontStyle = FontStyles.Bold;
        completionBadgeText.color = new Color(0.70f, 1.00f, 0.78f, 1f);
        completionMetricsText = CreateText("CompletionMetrics", panelGo.transform, new Vector2(34f, -220f), new Vector2(690f, 96f), 24f);
        completionProofText = CreateText("CompletionProof", panelGo.transform, new Vector2(34f, -338f), new Vector2(690f, 122f), 22f);
        completionProofText.color = new Color(0.84f, 0.90f, 0.96f, 1f);
        SetCompletionFieldsVisible(false);
        CreateNextButton(panelGo.transform);
        EnsureSpatialHintCanvases();
    }

    private void CreateNextButton(Transform parent)
    {
        GameObject buttonGo = new GameObject("OnboardingNextButton");
        buttonGo.transform.SetParent(parent, false);

        RectTransform rect = buttonGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(190f, 58f);
        rect.anchoredPosition = new Vector2(-34f, 30f);

        Image image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.12f, 0.22f, 0.34f, 0.96f);

        nextButton = buttonGo.AddComponent<Button>();
        nextButton.targetGraphic = image;
        nextButton.onClick.AddListener(AdvanceOnboarding);

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(buttonGo.transform, false);
        nextButtonText = labelGo.AddComponent<TextMeshProUGUI>();
        nextButtonText.fontSize = 24f;
        nextButtonText.fontStyle = FontStyles.Bold;
        nextButtonText.color = Color.white;
        nextButtonText.alignment = TextAlignmentOptions.Center;
        nextButtonText.raycastTarget = false;

        RectTransform labelRect = nextButtonText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);
    }

    private TMP_Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        GameObject textGo = new GameObject(name);
        textGo.transform.SetParent(parent, false);
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = new Color(0.94f, 0.96f, 1f, 1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return text;
    }

    private void PositionGuideInView()
    {
        if (guideCanvas == null || userHead == null)
        {
            return;
        }

        Vector3 flatForward = Vector3.ProjectOnPlane(userHead.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.forward;
        }

        Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;
        Vector3 position = userHead.position
            + flatForward * guideDistanceFromHead
            + flatRight * guideSideOffset
            + Vector3.up * guideHeightOffset;

        guideCanvas.transform.position = position;
        guideCanvas.transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    private void UpdateGuideText(string overrideMessage = null)
    {
        if (titleText == null || bodyText == null)
        {
            return;
        }

        ResolveReferences();
        if (contextSource != null)
        {
            latestFrame = contextSource.LatestFrame;
        }

        titleText.text = ResolveTitle();
        if (statusText != null)
        {
            statusText.text = IsOnboardingActive()
                ? "Step " + (onboardingStepIndex + 1) + " of " + OnboardingStepCount
                : StateLabel(currentState) + "  -  " + StateMeaning(currentState);
            statusText.color = IsOnboardingActive()
                ? new Color(0.18f, 0.72f, 0.92f, 1f)
                : StateColor(currentState);
        }

        if (stateAccentImage != null)
        {
            stateAccentImage.color = IsOnboardingActive()
                ? new Color(0.18f, 0.72f, 0.92f, 1f)
                : StateColor(currentState);
        }

        UpdateNextButtonVisibility();

        if (Time.time < overrideMessageUntil && !string.IsNullOrWhiteSpace(pinnedMessage))
        {
            bodyText.text = WithStatus(pinnedMessage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(overrideMessage))
        {
            bodyText.text = WithStatus(overrideMessage);
            return;
        }

        if (completedObjects.Count >= objectToReceptacle.Count)
        {
            ShowCompletionSummary();
            return;
        }

        SetCompletionFieldsVisible(false);

        if (IsOnboardingActive())
        {
            bodyText.text = BuildOnboardingMessage();
            UpdateEvidencePanelForOnboarding();
            return;
        }

        if (!string.IsNullOrEmpty(selectedObjectName))
        {
            bodyText.text = WithStatus(
                "Holding " + FriendlyName(selectedObjectName) + ". Aim at the highlighted " +
                FriendlyName(objectToReceptacle[selectedObjectName]) + " and release pinch.");
            return;
        }

        if (objectToReceptacle.ContainsKey(currentAoi))
        {
            string activeObject = GetNextIncompleteObject();
            if (!string.IsNullOrEmpty(activeObject) &&
                !string.Equals(currentAoi, activeObject, System.StringComparison.OrdinalIgnoreCase))
            {
                bodyText.text = WithStatus(
                    "Incorrect object in focus\n" +
                    FriendlyName(currentAoi, capitalize: true) + " is not the active task.\n" +
                    "Please select the " + FriendlyName(activeObject) + ".");
                return;
            }

            bodyText.text = WithStatus("Looking at " + FriendlyName(currentAoi) + ". Pinch and hold to pick it up.");
            return;
        }

        string nextObject = GetNextIncompleteObject();
        string nextInstruction = string.IsNullOrEmpty(nextObject)
            ? "Look at a cube, cylinder, or sphere."
            : "Active task\nSelect the " + FriendlyName(nextObject) + ".";

        bodyText.text = WithStatus(
            nextInstruction + "\n" +
            "Pinch and hold to pick it up.\n" +
            "Aim at the highlighted matching pad, then release.");
    }

    private string ResolveTitle()
    {
        if (completedObjects.Count >= objectToReceptacle.Count)
        {
            return "Research Session Completed";
        }

        if (IsOnboardingActive())
        {
            return "Adaptive XR Context Prototype";
        }

        return "Live Context-Guided Task";
    }

    private string WithStatus(string message)
    {
        string handState = IsPinching()
            ? "pinching"
            : "open";
        string selected = string.IsNullOrEmpty(selectedObjectName) ? "none" : FriendlyName(selectedObjectName);
        if (evidenceText != null)
        {
            if (completedObjects.Count >= objectToReceptacle.Count)
            {
                evidenceText.text =
                    "Analysis complete: gaze, hand activity, pose, and task behavior were interpreted during the session.\n" +
                    "Adaptive responses: context-aware guidance and support controls were demonstrated.";
                return message;
            }

            evidenceText.text =
                "Behavior analysis: " + BuildStateReason() + "\n" +
                "Session activity: Gaze " + FriendlyName(ResolveActiveAoi()) +
                "  |  Hand " + handState +
                "  |  Pose " + ResolvePostureLabel() +
                "  |  Progress " + completedObjects.Count + "/" + objectToReceptacle.Count +
                "  |  Selected " + selected;
        }

        return message;
    }

    private string BuildOnboardingMessage()
    {
        switch (onboardingStepIndex)
        {
            case 0:
                return
                    "What this is\n" +
                    "A guided research prototype for adaptive XR.\n\n" +
                    "It studies whether an XR system can understand user context from natural behavior.";
            case 1:
                return
                    "What the system analyzes\n" +
                    "Gaze shows task attention.\n" +
                    "Hand activity shows interaction.\n" +
                    "Pose and movement show pauses or transitions.";
            case 2:
                return
                    "Why you are sorting\n" +
                    "The sorting task gives the system a controlled activity to observe.\n\n" +
                    "The task is simple so the research signal is clear.";
            case 3:
            default:
                return
                    "What will be proven\n" +
                    "The app detects engaged, distracted, transitioning, and idle states.\n\n" +
                    "Then it shows how adaptive XR can support the user at the right moment.";
        }
    }

    private void ShowCompletionSummary()
    {
        SetCompletionFieldsVisible(true);

        bodyText.text = string.Empty;
        if (evidenceText != null)
        {
            evidenceText.gameObject.SetActive(false);
        }

        if (completionBadgeText != null)
        {
            completionBadgeText.text =
                "Task Successfully Completed\n" +
                "Adaptive XR Analysis Complete";
        }

        if (completionMetricsText != null)
        {
            completionMetricsText.text =
                "Result: 3 of 3 objects sorted\n" +
                "States observed: " + BuildObservedStatesSummary() + "\n" +
                "State changes observed: " + stateTransitionCount;
        }

        if (completionProofText != null)
        {
            completionProofText.text =
                "Research Session Completed\n" +
                "The proof-of-concept demonstrated that gaze, hand activity, pose, and task behavior can support adaptive XR context awareness.";
        }
    }

    private void SetCompletionFieldsVisible(bool visible)
    {
        if (completionBadgeText != null)
        {
            completionBadgeText.gameObject.SetActive(visible);
        }

        if (completionMetricsText != null)
        {
            completionMetricsText.gameObject.SetActive(visible);
        }

        if (completionProofText != null)
        {
            completionProofText.gameObject.SetActive(visible);
        }

        if (evidenceText != null && !visible)
        {
            evidenceText.gameObject.SetActive(true);
        }
    }

    private string BuildObservedStatesSummary()
    {
        ContextState[] orderedStates =
        {
            ContextState.Engaged,
            ContextState.Distracted,
            ContextState.Transitioning,
            ContextState.Idle
        };

        List<string> labels = new List<string>();
        for (int i = 0; i < orderedStates.Length; i++)
        {
            if (observedStates.Contains(orderedStates[i]))
            {
                labels.Add(StateLabel(orderedStates[i]));
            }
        }

        if (labels.Count == 0)
        {
            return StateLabel(currentState);
        }

        return string.Join(", ", labels);
    }

    private string BuildStateReason()
    {
        string aoi = ResolveActiveAoi();
        bool onTaskAoi = IsTaskRelevantAoi(aoi);
        bool pinching = IsPinching();
        int interactions = Mathf.Max(0, latestFrame.interaction_count_10s);
        float fixation = Mathf.Max(0f, latestFrame.fixation_duration_s);
        float bodyVelocity = Mathf.Max(0f, latestFrame.avg_joint_velocity);
        string posture = string.IsNullOrWhiteSpace(latestFrame.posture_class) ? "unknown" : latestFrame.posture_class.ToLowerInvariant();

        switch (currentState)
        {
            case ContextState.Engaged:
                if (onTaskAoi && (pinching || !string.IsNullOrEmpty(selectedObjectName) || interactions > 0))
                {
                    return "Gaze is on " + FriendlyName(aoi) + " and task interaction is active.";
                }

                if (onTaskAoi && fixation >= 0.2f)
                {
                    return "Gaze has remained on the task area long enough to suggest stable focus.";
                }

                return "Task-relevant gaze is more stable than distractive scanning.";

            case ContextState.Distracted:
                if (!onTaskAoi && bodyVelocity >= 0.03f)
                {
                    return "Attention moved away from the task area while body movement increased.";
                }

                if (!onTaskAoi)
                {
                    return "Gaze is off the task objects and recent task interaction is low.";
                }

                return "Task focus appears unstable and attention is drifting away.";

            case ContextState.Transitioning:
                if (pinching || !string.IsNullOrEmpty(selectedObjectName))
                {
                    return "Hand activity suggests the user is moving between task steps.";
                }

                if (bodyVelocity >= 0.03f || posture == "leaning" || posture == "reaching")
                {
                    return "Body posture and movement suggest repositioning between targets.";
                }

                return "Gaze and posture indicate a shift between task targets rather than a settled state.";

            case ContextState.Idle:
            default:
                if (!pinching && interactions == 0 && bodyVelocity < 0.05f)
                {
                    return "There is little hand activity or body movement near the task.";
                }

                return "Task-directed activity has paused long enough to be treated as idle.";
        }
    }

    private string ResolveActiveAoi()
    {
        if (!string.IsNullOrWhiteSpace(currentAoi) && currentAoi != "none")
        {
            return currentAoi;
        }

        if (!string.IsNullOrWhiteSpace(latestFrame.aoi_hit) && latestFrame.aoi_hit != "none")
        {
            return latestFrame.aoi_hit;
        }

        return "task area";
    }

    private bool IsTaskRelevantAoi(string aoi)
    {
        return objectToReceptacle.ContainsKey(aoi) || IsKnownReceptacle(aoi);
    }

    private bool IsPinching()
    {
        if (handCapture != null && (handCapture.left_pinch || handCapture.right_pinch))
        {
            return true;
        }

        return latestFrame.left_pinch || latestFrame.right_pinch;
    }

    private bool IsOnboardingActive()
    {
        return requireOnboardingConfirmation &&
               completedObjects.Count == 0 &&
               onboardingStepIndex < OnboardingStepCount;
    }

    private void AdvanceOnboarding()
    {
        if (!IsOnboardingActive())
        {
            return;
        }

        onboardingStepIndex++;
        if (!IsOnboardingActive())
        {
            ShowPinnedMessage("Research intro complete. Begin with the cube.", 2f);
        }

        UpdateGuideText();
    }

    private void UpdateNextButtonVisibility()
    {
        if (nextButton == null)
        {
            return;
        }

        bool show = IsOnboardingActive();
        nextButton.gameObject.SetActive(show);
        nextButton.interactable = show;

        if (nextButtonText != null)
        {
            nextButtonText.text = onboardingStepIndex >= OnboardingStepCount - 1 ? "Begin" : "Next";
        }
    }

    private void UpdateEvidencePanelForOnboarding()
    {
        if (evidenceText == null)
        {
            return;
        }

        evidenceText.text =
            "Continue: press Next or pinch once.\n" +
            "Research signals: Gaze  |  Hands  |  Pose  |  Task behavior";
    }

    private string ResolvePostureLabel()
    {
        if (!string.IsNullOrWhiteSpace(latestFrame.posture_class) && latestFrame.posture_class != "unknown")
        {
            return latestFrame.posture_class.ToLowerInvariant();
        }

        return "stable";
    }

    private void EnsureSpatialHintCanvases()
    {
        objectHintText = CreateSpatialHint("ActiveObjectHint", out objectHintCanvas, out objectHintImage);
        padHintText = CreateSpatialHint("TargetPadHint", out padHintCanvas, out padHintImage);
    }

    private TMP_Text CreateSpatialHint(string name, out Canvas canvas, out Image panelImage)
    {
        GameObject canvasGo = new GameObject(name);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 55;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(310f, 92f);
        canvasRect.localScale = Vector3.one * 0.001f;

        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.05f, 0.07f, 0.84f);
        panelImage.raycastTarget = false;

        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        TMP_Text text = CreateText("Label", panelGo.transform, new Vector2(18f, -14f), new Vector2(274f, 68f), 22f);
        text.fontStyle = FontStyles.Bold;
        return text;
    }

    private void UpdateSpatialHints()
    {
        if (objectHintCanvas == null || padHintCanvas == null)
        {
            return;
        }

        bool showHints = !IsOnboardingActive() && completedObjects.Count < objectToReceptacle.Count;
        objectHintCanvas.gameObject.SetActive(showHints);
        padHintCanvas.gameObject.SetActive(showHints);

        if (!showHints || userHead == null)
        {
            return;
        }

        string activeObject = string.IsNullOrEmpty(selectedObjectName) ? GetNextIncompleteObject() : selectedObjectName;
        if (string.IsNullOrEmpty(activeObject) || !visuals.TryGetValue(activeObject, out ObjectVisual activeVisual))
        {
            return;
        }

        string targetPad = objectToReceptacle.TryGetValue(activeObject, out string pad) ? pad : null;
        objectHintText.text = string.IsNullOrEmpty(selectedObjectName)
            ? "Active task\nSelect the " + FriendlyName(activeObject)
            : "Selected\n" + FriendlyName(activeObject, capitalize: true);
        if (objectHintImage != null)
        {
            objectHintImage.color = string.IsNullOrEmpty(selectedObjectName)
                ? new Color(0.06f, 0.12f, 0.18f, 0.88f)
                : new Color(0.04f, 0.16f, 0.10f, 0.88f);
        }
        PositionHintCanvas(objectHintCanvas, activeVisual.transform, new Vector3(0f, 0.28f, 0f));

        if (!string.IsNullOrEmpty(targetPad) && visuals.TryGetValue(targetPad, out ObjectVisual padVisual))
        {
            padHintText.text = "Target pad\nPlace on " + FriendlyName(targetPad);
            if (padHintImage != null)
            {
                padHintImage.color = new Color(0.07f, 0.11f, 0.16f, 0.88f);
            }
            PositionHintCanvas(padHintCanvas, padVisual.transform, new Vector3(0f, 0.22f, 0f));
        }
    }

    private void PositionHintCanvas(Canvas canvas, Transform anchor, Vector3 worldOffset)
    {
        if (canvas == null || anchor == null || userHead == null)
        {
            return;
        }

        canvas.transform.position = anchor.position + worldOffset;
        Vector3 faceDirection = Vector3.ProjectOnPlane(canvas.transform.position - userHead.position, Vector3.up).normalized;
        if (faceDirection.sqrMagnitude < 0.0001f)
        {
            faceDirection = Vector3.forward;
        }

        canvas.transform.rotation = Quaternion.LookRotation(faceDirection, Vector3.up);
    }

    private string GetNextIncompleteObject()
    {
        string[] orderedObjects = { "task_cube_1", "task_cylinder_1", "task_sphere_1" };
        for (int i = 0; i < orderedObjects.Length; i++)
        {
            string candidate = orderedObjects[i];
            if (!completedObjects.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private void ShowPinnedMessage(string message, float seconds)
    {
        pinnedMessage = message;
        overrideMessageUntil = Time.time + seconds;
        UpdateGuideText();
    }

    private static string FriendlyName(string objectName, bool capitalize = false)
    {
        string label;
        switch (objectName)
        {
            case "task_cube_1":
                label = "cube";
                break;
            case "task_cylinder_1":
                label = "cylinder";
                break;
            case "task_sphere_1":
                label = "sphere";
                break;
            case "receptacle_a":
                label = "cube pad";
                break;
            case "receptacle_b":
                label = "cylinder pad";
                break;
            case "receptacle_c":
                label = "sphere pad";
                break;
            case "task area":
                label = "task area";
                break;
            default:
                label = objectName.Replace("_", " ");
                break;
        }

        if (!capitalize || string.IsNullOrEmpty(label))
        {
            return label;
        }

        return char.ToUpperInvariant(label[0]) + label.Substring(1);
    }

    private static string StateLabel(ContextState state)
    {
        switch (state)
        {
            case ContextState.Engaged:
                return "Engaged";
            case ContextState.Distracted:
                return "Distracted";
            case ContextState.Transitioning:
                return "Transitioning";
            case ContextState.Idle:
            default:
                return "Idle";
        }
    }

    private static string StateMeaning(ContextState state)
    {
        switch (state)
        {
            case ContextState.Engaged:
                return "focused on the current task";
            case ContextState.Distracted:
                return "attention has shifted away from the task";
            case ContextState.Transitioning:
                return "moving between task steps or targets";
            case ContextState.Idle:
            default:
                return "paused with little task-directed activity";
        }
    }

    private static Color StateColor(ContextState state)
    {
        switch (state)
        {
            case ContextState.Engaged:
                return new Color(0.50f, 0.20f, 0.75f);
            case ContextState.Distracted:
                return new Color(1.00f, 0.75f, 0.20f);
            case ContextState.Transitioning:
                return new Color(1.00f, 0.50f, 0.40f);
            case ContextState.Idle:
            default:
                return new Color(0.70f, 0.72f, 0.76f);
        }
    }

    private static void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }
}
