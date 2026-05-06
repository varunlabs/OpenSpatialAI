using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AdaptationManager : MonoBehaviour
{
    [Header("Context Source")]
    [SerializeField] private XRAppShellController appShell;

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
    private ContextState activeState;
    private Vignette vignette;
    private Vector3 lastTaskPanelPosition;

    private void Start()
    {
        if (appShell == null)
        {
            appShell = FindObjectOfType<XRAppShellController>(true);
        }

        if (userHead == null && Camera.main != null)
        {
            userHead = Camera.main.transform;
        }

        CacheEffects();
        ResetVisualState();

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

    public void OnContinuePressed()
    {
        if (currentTaskPanel != null)
        {
            currentTaskPanel.transform.position = lastTaskPanelPosition;
            currentTaskPanel.alpha = 1f;
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
        if (restDimOverlay != null)
        {
            restDimOverlay.gameObject.SetActive(true);
            restDimOverlay.alpha = 0.55f;
        }
    }

    public void OnRecenterViewPressed()
    {
        if (uiAnchorRoot == null || userHead == null)
        {
            return;
        }

        uiAnchorRoot.position = userHead.position + userHead.forward * 1.5f;
        uiAnchorRoot.rotation = Quaternion.LookRotation(uiAnchorRoot.position - userHead.position);
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
        if (snapshot.state == activeState && activeBehavior != null)
        {
            return;
        }

        activeState = snapshot.state;

        if (activeBehavior != null)
        {
            StopCoroutine(activeBehavior);
        }

        ResetVisualState();
        activeBehavior = StartCoroutine(RunBehavior(snapshot));
    }

    private IEnumerator RunBehavior(XRContextSnapshot snapshot)
    {
        switch (snapshot.state)
        {
            case ContextState.Engaged:
                yield return StartCoroutine(EngagedBehavior(snapshot));
                break;
            case ContextState.Distracted:
                yield return StartCoroutine(DistractedBehavior(snapshot));
                break;
            case ContextState.Transitioning:
                yield return StartCoroutine(TransitioningBehavior(snapshot));
                break;
            case ContextState.Idle:
                yield return StartCoroutine(IdleBehavior(snapshot));
                break;
        }
    }

    private IEnumerator EngagedBehavior(XRContextSnapshot snapshot)
    {
        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.Override(engagedVignetteIntensity);
        }

        if (snapshot.boundaryType == BoundaryType.RoomScale && navigationHintsRoot != null)
        {
            navigationHintsRoot.SetActive(false);
        }

        yield break;
    }

    private IEnumerator DistractedBehavior(XRContextSnapshot snapshot)
    {
        yield return new WaitForSeconds(1.5f);

        bool roomScale = snapshot.boundaryType == BoundaryType.RoomScale;

        if (roomScale)
        {
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
        else
        {
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
    }

    private IEnumerator TransitioningBehavior(XRContextSnapshot snapshot)
    {
        if (nextTaskPanel != null)
        {
            nextTaskPanel.gameObject.SetActive(true);
            nextTaskPanel.alpha = 0f;
        }

        bool roomScale = snapshot.boundaryType == BoundaryType.RoomScale;

        if (!roomScale)
        {
            if (currentTaskPanel != null)
            {
                lastTaskPanelPosition = currentTaskPanel.transform.position;
                currentTaskPanel.alpha = 0.7f;
            }
        }
        else if (nextTaskPanel != null && userHead != null)
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

    private IEnumerator IdleBehavior(XRContextSnapshot snapshot)
    {
        if (restPanel != null)
        {
            restPanel.SetActive(true);
        }

        if (snapshot.boundaryType == BoundaryType.RoomScale && floorArrow != null && userHead != null)
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

        if (currentTaskPanel != null)
        {
            currentTaskPanel.alpha = 1f;
        }

        if (nextTaskPanel != null)
        {
            nextTaskPanel.alpha = 0f;
            nextTaskPanel.gameObject.SetActive(false);
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

        if (floorArrow != null)
        {
            floorArrow.SetActive(false);
        }
    }
}
