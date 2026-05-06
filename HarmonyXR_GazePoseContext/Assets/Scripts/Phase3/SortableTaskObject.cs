using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SortableTaskObject : MonoBehaviour
{
    [SerializeField] private TaskObjectType objectType;

    public TaskObjectType ObjectType => objectType;

    private void Reset()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
    }
}
