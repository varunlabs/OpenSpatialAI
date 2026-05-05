using TMPro;
using UnityEngine;

public class CubeStateDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private Renderer cubeRenderer;
    [SerializeField] private bool disableGazeHighlightOnCube = true;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        if (cubeRenderer == null)
        {
            cubeRenderer = GetComponentInParent<Renderer>();
        }
        if (cubeRenderer == null)
        {
            Debug.LogWarning("[CTX CUBE] cubeRenderer is null. Assign the cube MeshRenderer in Inspector.");
        }
        if (textMesh == null)
        {
            Debug.LogWarning("[CTX CUBE] textMesh is null. Assign a 3D TextMeshPro in Inspector.");
        }

        if (disableGazeHighlightOnCube && cubeRenderer != null)
        {
            GazeHighlight gazeHighlight = cubeRenderer.GetComponent<GazeHighlight>();
            if (gazeHighlight == null)
            {
                gazeHighlight = cubeRenderer.GetComponentInParent<GazeHighlight>();
            }
            if (gazeHighlight == null)
            {
                gazeHighlight = cubeRenderer.GetComponentInChildren<GazeHighlight>(true);
            }

            if (gazeHighlight != null && gazeHighlight.enabled)
            {
                gazeHighlight.enabled = false;
                Debug.LogWarning("[CTX CUBE] Disabled GazeHighlight on cube to avoid color conflicts with context state.");
            }
        }
    }

    public void UpdateState(ContextResult result)
    {
        if (textMesh == null)
        {
            Debug.LogWarning("[CTX CUBE] UpdateState skipped because textMesh is null.");
            return;
        }

        textMesh.text =
            "STATE: " + result.state.ToString().ToUpperInvariant() + "\n" +
            "IDLE | DISTRACTED | TRANSITIONING | ENGAGED";

        Color stateColor;
        switch (result.state)
        {
            case ContextState.Engaged:
                stateColor = Color.green;
                break;
            case ContextState.Distracted:
                stateColor = Color.red;
                break;
            case ContextState.Transitioning:
                stateColor = Color.yellow;
                break;
            case ContextState.Idle:
            default:
                stateColor = Color.white;
                break;
        }

        // Keep label readable regardless of cube color.
        textMesh.color = Color.black;

        if (cubeRenderer != null)
        {
            Material material = cubeRenderer.material;
            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, stateColor);
            }
            else if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, stateColor);
            }
        }
    }
}
