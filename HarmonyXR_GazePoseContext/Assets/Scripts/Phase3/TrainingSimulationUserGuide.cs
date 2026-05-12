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
    [SerializeField] private float guideHeightOffset = 0.32f;
    [SerializeField] private float guideSideOffset = 0.48f;
    [SerializeField] private float carryDistanceFromHead = 1.15f;
    [SerializeField] private float carryHeightOffset = -0.18f;
    [SerializeField] private float dropHeightAbovePad = 0.14f;
    [SerializeField] private float placementAssistAngleDegrees = 24f;
    [SerializeField] private float placementAssistMaxDistance = 3.5f;
    [SerializeField] private float onboardingSeconds = 6f;

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
    private TMP_Text titleText;
    private TMP_Text statusText;
    private TMP_Text bodyText;
    private string currentAoi = "none";
    private ContextState currentState = ContextState.Idle;
    private string selectedObjectName;
    private Vector3 selectedObjectStartPosition;
    private Quaternion selectedObjectStartRotation;
    private Collider[] selectedObjectColliders;
    private bool wasPinching;
    private float overrideMessageUntil;
    private float onboardingUntil;
    private string pinnedMessage;

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
        onboardingUntil = Time.time + onboardingSeconds;
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
            ShowPinnedMessage("All objects sorted. Task complete.", 3f);
            return;
        }

        ShowPinnedMessage("Placed " + FriendlyName(objectName) + ". Continue with the next object.", 2.5f);
    }

    private void OnContextSnapshotUpdated(XRContextSnapshot snapshot)
    {
        currentAoi = string.IsNullOrWhiteSpace(snapshot.aoiHit) ? "none" : snapshot.aoiHit;
        currentState = snapshot.state;
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
        canvasRect.sizeDelta = new Vector2(560f, 340f);
        canvasRect.localScale = Vector3.one * 0.00125f;

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

        titleText = CreateText("Title", panelGo.transform, new Vector2(34f, -24f), new Vector2(492f, 42f), 24f);
        statusText = CreateText("Status", panelGo.transform, new Vector2(34f, -64f), new Vector2(492f, 30f), 18f);
        bodyText = CreateText("Body", panelGo.transform, new Vector2(34f, -106f), new Vector2(492f, 190f), 20f);
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

        titleText.text = "Sort the objects";
        if (statusText != null)
        {
            statusText.text = "State: " + StateLabel(currentState);
            statusText.color = StateColor(currentState);
        }

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
            bodyText.text = WithStatus("All 3 objects are sorted. The training task is complete.");
            return;
        }

        if (Time.time < onboardingUntil)
        {
            bodyText.text = WithStatus(
                "Sort the 3 objects into matching pads.\n" +
                "Look at an object, pinch and hold to pick it up.\n" +
                "Aim at the highlighted matching pad, then release.");
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
            bodyText.text = WithStatus("Looking at " + FriendlyName(currentAoi) + ". Pinch and hold to pick it up.");
            return;
        }

        string nextObject = GetNextIncompleteObject();
        string nextInstruction = string.IsNullOrEmpty(nextObject)
            ? "Look at a cube, cylinder, or sphere."
            : "Next: look at the " + FriendlyName(nextObject) + ".";

        bodyText.text = WithStatus(
            nextInstruction + "\n" +
            "Pinch and hold to pick it up.\n" +
            "Aim at the highlighted matching pad, then release.");
    }

    private string WithStatus(string message)
    {
        string handState = handCapture != null && (handCapture.left_pinch || handCapture.right_pinch)
            ? "pinching"
            : "open";
        string selected = string.IsNullOrEmpty(selectedObjectName) ? "none" : FriendlyName(selectedObjectName);
        return message + "\n\nProgress: " + completedObjects.Count + "/" + objectToReceptacle.Count +
               "  |  Hand: " + handState +
               "  |  Selected: " + selected;
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

    private static string FriendlyName(string objectName)
    {
        switch (objectName)
        {
            case "task_cube_1":
                return "cube";
            case "task_cylinder_1":
                return "cylinder";
            case "task_sphere_1":
                return "sphere";
            case "receptacle_a":
                return "cube pad";
            case "receptacle_b":
                return "cylinder pad";
            case "receptacle_c":
                return "sphere pad";
            default:
                return objectName.Replace("_", " ");
        }
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
