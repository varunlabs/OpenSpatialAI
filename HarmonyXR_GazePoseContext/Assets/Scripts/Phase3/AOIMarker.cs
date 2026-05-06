using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class AOIMarker : MonoBehaviour
{
    [SerializeField] private string aoiName = "aoi_object";

    public string AoiName => aoiName;

    private void Reset()
    {
        gameObject.tag = "AOI";

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        if (string.IsNullOrWhiteSpace(aoiName))
        {
            aoiName = gameObject.name;
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(aoiName))
        {
            aoiName = gameObject.name;
        }
    }
}
