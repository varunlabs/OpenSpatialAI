using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TrainingTaskCompletedEvent : UnityEvent
{
}

public class TrainingSimulationTaskManager : MonoBehaviour
{
    [SerializeField] private ReceptacleTrigger[] receptacles;
    [SerializeField] private TrainingTaskCompletedEvent onAllTasksCompleted = new TrainingTaskCompletedEvent();

    private int completedCount;

    private void Start()
    {
        if (receptacles == null || receptacles.Length == 0)
        {
            receptacles = FindObjectsByType<ReceptacleTrigger>(FindObjectsInactive.Include);
        }

        foreach (ReceptacleTrigger receptacle in receptacles)
        {
            if (receptacle != null)
            {
                receptacle.OnCorrectPlacement.AddListener(OnCorrectPlacement);
            }
        }
    }

    private void OnDestroy()
    {
        if (receptacles == null)
        {
            return;
        }

        foreach (ReceptacleTrigger receptacle in receptacles)
        {
            if (receptacle != null)
            {
                receptacle.OnCorrectPlacement.RemoveListener(OnCorrectPlacement);
            }
        }
    }

    private void OnCorrectPlacement(string objectName, string receptacleName)
    {
        completedCount++;
        Debug.Log("[TrainingSimulation] Progress: " + completedCount + "/3 completed.");

        if (completedCount >= 3)
        {
            Debug.Log("[TrainingSimulation] All sorting tasks completed.");
            onAllTasksCompleted.Invoke();
        }
    }
}
