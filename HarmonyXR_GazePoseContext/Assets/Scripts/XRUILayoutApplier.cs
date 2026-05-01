using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class XRUILayoutApplier : MonoBehaviour
{
    private readonly string[] _textNames =
    {
        "GazeStatusText",
        "AOIHitText",
        "HeadForwardText",
        "SpineAngleText",
        "PostureText",
        "PinchText",
        "InteractionCountText",
        "DistanceText"
    };

    private readonly string[] _debugFieldNames =
    {
        "gazeStatusText",
        "aoiHitText",
        "headForwardText",
        "spineAngleText",
        "postureText",
        "pinchText",
        "interactionCountText",
        "distanceText"
    };

    private readonly float[] _posYValues = { 180f, 130f, 80f, 30f, -20f, -70f, -120f, -170f };

    private void Start()
    {
        StartCoroutine(ApplyLayoutOverFrames());
    }

    private IEnumerator ApplyLayoutOverFrames()
    {
        for (int i = 0; i < 10; i++)
        {
            ApplyNow();
            yield return null;
        }
    }

    private void ApplyNow()
    {
        Transform centerEyeAnchor = FindCenterEyeAnchor();
        if (centerEyeAnchor == null)
        {
            Debug.LogWarning("XRUILayoutApplier: CenterEyeAnchor not found.");
            return;
        }

        Canvas targetCanvas = FindBestCanvas(centerEyeAnchor);
        if (targetCanvas == null)
        {
            Debug.LogWarning("XRUILayoutApplier: No matching Canvas found under CenterEyeAnchor.");
            return;
        }

        ApplyCanvasLayout(targetCanvas);
        ApplyTextLayouts(centerEyeAnchor, targetCanvas.transform);
    }

    private Transform FindCenterEyeAnchor()
    {
        GameObject found = GameObject.Find("CenterEyeAnchor");
        return found != null ? found.transform : null;
    }

    private Canvas FindBestCanvas(Transform centerEyeAnchor)
    {
        Canvas[] canvases = centerEyeAnchor.GetComponentsInChildren<Canvas>(true);
        Canvas best = null;
        int bestScore = -1;

        foreach (Canvas canvas in canvases)
        {
            int score = CountMatchingTextNames(canvas.transform);
            if (score > bestScore)
            {
                bestScore = score;
                best = canvas;
            }
        }

        return best;
    }

    private int CountMatchingTextNames(Transform canvasTransform)
    {
        int count = 0;
        var names = new HashSet<string>();
        TextMeshProUGUI[] allTmp = canvasTransform.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI tmp in allTmp)
        {
            names.Add(tmp.name);
        }

        foreach (string name in _textNames)
        {
            if (names.Contains(name))
            {
                count++;
            }
        }

        return count;
    }

    private void ApplyCanvasLayout(Canvas canvas)
    {
        Transform t = canvas.transform;
        t.localPosition = new Vector3(0f, 0f, 1.2f);
        t.localRotation = Quaternion.Euler(0f, 0f, 0f);
        t.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);

        RectTransform rt = canvas.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(1200f, 600f);
        }
    }

    private void ApplyTextLayouts(Transform centerEyeAnchor, Transform canvasTransform)
    {
        TextMeshProUGUI[] targets = ResolveTargetsFromXRDebugUI(centerEyeAnchor, canvasTransform);

        for (int i = 0; i < targets.Length; i++)
        {
            TextMeshProUGUI tmp = targets[i];
            if (tmp == null)
            {
                continue;
            }

            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(900f, 50f);
            rt.anchoredPosition = new Vector2(-250f, _posYValues[i]);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            tmp.fontSize = 38f;
            tmp.enableWordWrapping = true;
            tmp.alignment = TextAlignmentOptions.TopLeft;
        }
    }

    private TextMeshProUGUI[] ResolveTargetsFromXRDebugUI(Transform centerEyeAnchor, Transform canvasTransform)
    {
        TextMeshProUGUI[] result = new TextMeshProUGUI[_debugFieldNames.Length];

        Component xrDebugUi = centerEyeAnchor.GetComponentInChildren(System.Type.GetType("XRDebugUI"), true);
        if (xrDebugUi != null)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            System.Type type = xrDebugUi.GetType();

            for (int i = 0; i < _debugFieldNames.Length; i++)
            {
                FieldInfo field = type.GetField(_debugFieldNames[i], flags);
                if (field != null)
                {
                    result[i] = field.GetValue(xrDebugUi) as TextMeshProUGUI;
                }
            }
        }

        var byName = new Dictionary<string, TextMeshProUGUI>();
        TextMeshProUGUI[] allTmp = canvasTransform.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in allTmp)
        {
            if (!byName.ContainsKey(tmp.name))
            {
                byName[tmp.name] = tmp;
            }
        }

        for (int i = 0; i < _textNames.Length; i++)
        {
            if (result[i] == null && byName.TryGetValue(_textNames[i], out TextMeshProUGUI fallback))
            {
                result[i] = fallback;
            }
        }

        return result;
    }
}
