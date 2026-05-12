using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TaskPlacementEvent : UnityEvent<string, string>
{
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ReceptacleTrigger : MonoBehaviour
{
    [SerializeField] private TaskObjectType acceptedType;
    [SerializeField] private string receptacleName = "receptacle_a";
    [SerializeField] private bool disableAfterCorrectPlacement = true;
    [SerializeField] private TaskPlacementEvent onCorrectPlacement = new TaskPlacementEvent();

    private bool hasAcceptedObject;

    public TaskPlacementEvent OnCorrectPlacement => onCorrectPlacement;
    public string ReceptacleName => receptacleName;
    public TaskObjectType AcceptedType => acceptedType;
    public bool HasAcceptedObject => hasAcceptedObject;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (string.IsNullOrWhiteSpace(receptacleName))
        {
            receptacleName = gameObject.name;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        SortableTaskObject taskObj = other.GetComponent<SortableTaskObject>();
        if (taskObj == null)
        {
            taskObj = other.GetComponentInParent<SortableTaskObject>();
        }

        TryAcceptPlacement(taskObj);
    }

    public bool TryAcceptPlacement(SortableTaskObject taskObj)
    {
        if (hasAcceptedObject || taskObj == null || taskObj.ObjectType != acceptedType)
        {
            return false;
        }

        hasAcceptedObject = true;
        onCorrectPlacement.Invoke(taskObj.gameObject.name, receptacleName);
        Debug.Log("[TrainingSimulation] Correct placement: " + taskObj.gameObject.name + " -> " + receptacleName);

        if (disableAfterCorrectPlacement)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }

        return true;
    }
}
