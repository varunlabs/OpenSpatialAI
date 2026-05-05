using TMPro;
using UnityEngine;

public class CubeStateDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }
        if (textMesh == null)
        {
            Debug.LogWarning("[CTX CUBE] textMesh is null. Assign a 3D TextMeshPro in Inspector.");
        }
    }

    public void UpdateState(ContextResult result)
    {
        if (textMesh == null)
        {
            Debug.LogWarning("[CTX CUBE] UpdateState skipped because textMesh is null.");
            return;
        }

        textMesh.text = "STATE: " + result.state.ToString().ToUpperInvariant();

        switch (result.state)
        {
            case ContextState.Engaged:
                textMesh.color = Color.green;
                break;
            case ContextState.Distracted:
                textMesh.color = Color.red;
                break;
            case ContextState.Transitioning:
                textMesh.color = Color.yellow;
                break;
            case ContextState.Idle:
            default:
                textMesh.color = Color.white;
                break;
        }
    }
}
