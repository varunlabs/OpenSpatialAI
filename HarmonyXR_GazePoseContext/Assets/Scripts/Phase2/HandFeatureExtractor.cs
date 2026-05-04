using UnityEngine;

public struct HandFeatureVector
{
    public float interaction_frequency;
    public bool object_proximity;
}

public class HandFeatureExtractor
{
    public HandFeatureVector ExtractFeatures(SignalFrame frame)
    {
        int count = Mathf.Max(0, frame.interaction_count_10s);
        float interactionFrequency = count / 10f;

        float nearestObjectDistance = frame.nearest_object_dist_m;
        bool objectProximity;
        if (float.IsNaN(nearestObjectDistance) || nearestObjectDistance <= 0f)
        {
            objectProximity = false;
        }
        else
        {
            objectProximity = nearestObjectDistance < 0.3f;
        }

        return new HandFeatureVector
        {
            interaction_frequency = interactionFrequency,
            object_proximity = objectProximity
        };
    }
}
