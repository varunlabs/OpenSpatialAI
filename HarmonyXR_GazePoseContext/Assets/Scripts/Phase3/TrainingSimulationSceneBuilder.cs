using UnityEngine;

public class TrainingSimulationSceneBuilder : MonoBehaviour
{
    [ContextMenu("Build Training Simulation Layout")]
    public void BuildLayout()
    {
        EnsureTable();
        EnsureTaskObjects();
        EnsureReceptacles();
        EnsureInstructionPanel();

        Debug.Log("[TrainingSimulation] Layout build complete.");
    }

    private void EnsureTable()
    {
        GameObject table = FindOrCreate("table_surface", PrimitiveType.Cube, new Vector3(0f, 0.75f, 2f), new Vector3(1.8f, 0.08f, 1.2f));
        AddOrGetAOIMarker(table, "table_surface");
    }

    private void EnsureTaskObjects()
    {
        GameObject cube = FindOrCreate("task_cube_1", PrimitiveType.Cube, new Vector3(-0.5f, 0.88f, 1.75f), new Vector3(0.16f, 0.16f, 0.16f));
        ConfigureTaskObject(cube, TaskObjectType.Cube, "task_cube_1");

        GameObject cylinder = FindOrCreate("task_cylinder_1", PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 1.75f), new Vector3(0.14f, 0.12f, 0.14f));
        ConfigureTaskObject(cylinder, TaskObjectType.Cylinder, "task_cylinder_1");

        GameObject sphere = FindOrCreate("task_sphere_1", PrimitiveType.Sphere, new Vector3(0.5f, 0.88f, 1.75f), new Vector3(0.16f, 0.16f, 0.16f));
        ConfigureTaskObject(sphere, TaskObjectType.Sphere, "task_sphere_1");
    }

    private void EnsureReceptacles()
    {
        GameObject receptacleA = FindOrCreate("receptacle_a", PrimitiveType.Cube, new Vector3(-0.5f, 0.82f, 2.35f), new Vector3(0.22f, 0.06f, 0.22f));
        ConfigureReceptacle(receptacleA, TaskObjectType.Cube, "receptacle_a");

        GameObject receptacleB = FindOrCreate("receptacle_b", PrimitiveType.Cube, new Vector3(0f, 0.82f, 2.35f), new Vector3(0.22f, 0.06f, 0.22f));
        ConfigureReceptacle(receptacleB, TaskObjectType.Cylinder, "receptacle_b");

        GameObject receptacleC = FindOrCreate("receptacle_c", PrimitiveType.Cube, new Vector3(0.5f, 0.82f, 2.35f), new Vector3(0.22f, 0.06f, 0.22f));
        ConfigureReceptacle(receptacleC, TaskObjectType.Sphere, "receptacle_c");
    }

    private void EnsureInstructionPanel()
    {
        GameObject panel = FindOrCreate("instruction_panel", PrimitiveType.Quad, new Vector3(0f, 1.3f, 2.8f), new Vector3(0.9f, 0.45f, 1f));
        panel.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        AddOrGetAOIMarker(panel, "instruction_panel");
    }

    private static GameObject FindOrCreate(string objectName, PrimitiveType primitiveType, Vector3 position, Vector3 scale)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null)
        {
            go = GameObject.CreatePrimitive(primitiveType);
            go.name = objectName;
        }

        go.transform.position = position;
        go.transform.localScale = scale;
        go.tag = "AOI";
        return go;
    }

    private static AOIMarker AddOrGetAOIMarker(GameObject go, string aoiName)
    {
        AOIMarker marker = go.GetComponent<AOIMarker>();
        if (marker == null)
        {
            marker = go.AddComponent<AOIMarker>();
        }

        SetSerializedPrivateString(marker, "aoiName", aoiName);
        return marker;
    }

    private static void ConfigureTaskObject(GameObject go, TaskObjectType type, string aoiName)
    {
        AddOrGetAOIMarker(go, aoiName);

        SortableTaskObject sortable = go.GetComponent<SortableTaskObject>();
        if (sortable == null)
        {
            sortable = go.AddComponent<SortableTaskObject>();
        }

        SetSerializedEnum(sortable, "objectType", type);
    }

    private static void ConfigureReceptacle(GameObject go, TaskObjectType acceptedType, string receptacleName)
    {
        AddOrGetAOIMarker(go, receptacleName);

        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        ReceptacleTrigger trigger = go.GetComponent<ReceptacleTrigger>();
        if (trigger == null)
        {
            trigger = go.AddComponent<ReceptacleTrigger>();
        }

        SetSerializedEnum(trigger, "acceptedType", acceptedType);
        SetSerializedPrivateString(trigger, "receptacleName", receptacleName);
    }

    private static void SetSerializedPrivateString(Object target, string fieldName, string value)
    {
        if (target == null)
        {
            return;
        }

        var type = target.GetType();
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    private static void SetSerializedEnum<TEnum>(Object target, string fieldName, TEnum value) where TEnum : struct
    {
        if (target == null)
        {
            return;
        }

        var type = target.GetType();
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
