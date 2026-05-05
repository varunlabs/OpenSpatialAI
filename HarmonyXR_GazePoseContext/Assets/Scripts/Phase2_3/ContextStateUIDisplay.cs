using TMPro;
using UnityEngine;

public class ContextStateUIDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stateText;

    private void Awake()
    {
        if (stateText == null)
        {
            stateText = GetComponent<TextMeshProUGUI>();
        }
        if (stateText == null)
        {
            stateText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (stateText == null)
        {
            Debug.LogWarning("[CTX UI] stateText is null. Assign the exact Context state label TMP in Inspector.");
        }
    }

    public void UpdateState(ContextResult result)
    {
        if (stateText == null)
        {
            return;
        }

        stateText.text = "STATE: " + result.state.ToString().ToUpperInvariant();

        switch (result.state)
        {
            case ContextState.Engaged:
                stateText.color = Color.green;
                break;
            case ContextState.Distracted:
                stateText.color = Color.red;
                break;
            case ContextState.Transitioning:
                stateText.color = Color.yellow;
                break;
            case ContextState.Idle:
            default:
                stateText.color = Color.white;
                break;
        }
    }
}
