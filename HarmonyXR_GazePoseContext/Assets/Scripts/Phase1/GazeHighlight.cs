using UnityEngine;

public class GazeHighlight : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Header("Context State Override")]
    [SerializeField] private bool useContextStateColor;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color engagedColor = Color.green;
    [SerializeField] private Color transitioningColor = Color.yellow;
    [SerializeField] private Color distractedColor = Color.red;

    private Renderer cachedRenderer;
    private Material cachedMaterial;
    private bool hasBaseColorProperty;
    private bool lastState;
    private bool hasState;
    public bool UsesContextStateColor => useContextStateColor;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponentInChildren<Renderer>(true);
        }

        if (cachedRenderer != null)
        {
            cachedMaterial = cachedRenderer.material;
            hasBaseColorProperty = cachedMaterial != null && cachedMaterial.HasProperty(BaseColorId);
            if (!hasBaseColorProperty)
            {
                Debug.LogWarning($"GazeHighlight on '{name}' material does not expose _BaseColor.");
            }
        }
    }

    private void OnEnable()
    {
        SetGazeState(false);
    }

    public void SetGazeState(bool isGazed)
    {
        if (useContextStateColor)
        {
            return;
        }

        if (hasState && lastState == isGazed)
        {
            return;
        }

        hasState = true;
        lastState = isGazed;

        Color targetColor = isGazed ? Color.green : Color.red;
        ApplyColor(targetColor);
    }

    public void SetContextState(ContextState state)
    {
        if (!useContextStateColor)
        {
            return;
        }

        Color targetColor;
        switch (state)
        {
            case ContextState.Engaged:
                targetColor = engagedColor;
                break;
            case ContextState.Transitioning:
                targetColor = transitioningColor;
                break;
            case ContextState.Distracted:
                targetColor = distractedColor;
                break;
            case ContextState.Idle:
            default:
                targetColor = idleColor;
                break;
        }

        ApplyColor(targetColor);
    }

    private void ApplyColor(Color color)
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponentInChildren<Renderer>(true);
            }
        }

        if (cachedRenderer == null)
        {
            return;
        }

        if (cachedMaterial == null)
        {
            cachedMaterial = cachedRenderer.material;
            hasBaseColorProperty = cachedMaterial != null && cachedMaterial.HasProperty(BaseColorId);
            if (!hasBaseColorProperty)
            {
                Debug.LogWarning($"GazeHighlight on '{name}' material does not expose _BaseColor.");
            }
        }

        if (cachedMaterial != null && hasBaseColorProperty)
        {
            cachedMaterial.SetColor(BaseColorId, color);
        }
    }
}
