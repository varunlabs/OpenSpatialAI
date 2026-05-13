using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class MetaRuntimeActionBindingsBuildFix : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report == null || report.summary.platformGroup != BuildTargetGroup.Standalone)
        {
            return;
        }

        string outputPath = report.summary.outputPath;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return;
        }

        string bindingsPath = Path.GetFullPath(Path.Combine(outputPath, "..", "RuntimeActionBindings.json"));
        if (!File.Exists(bindingsPath))
        {
            return;
        }

        File.Delete(bindingsPath);
        Debug.Log("[Build] Deleted stale RuntimeActionBindings.json before standalone build: " + bindingsPath);
    }
}
