# Troubleshooting And Warnings

[First](00_START_HERE.md) | [Previous](07_SCRIPT_GUIDE.md) | [Next](09_FINAL_OUTPUT_AND_SUBMISSION.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

## App Stays On Quest Loading Screen

Known cause:

```text
ClassNotFoundException: com.unity3d.player.appui.AppUIGameActivity
```

Fix:

Open:

```text
Assets/Plugins/Android/AndroidManifest.xml
```

Use:

```text
com.unity3d.player.UnityPlayerGameActivity
```

## Gradle Duplicate Oculus Namespace Error

Known issue:

```text
Namespace 'com.oculus.Integration' is used in multiple modules
```

Fix used in current project:

- remove Meta XR All-in-One,
- use Meta XR Core directly,
- keep required Unity packages explicit.

## Safe Mode After Package Import

Do not delete scenes. Safe Mode can appear when Unity package imports are incomplete or corrupted.

Recommended approach:

1. Close Unity.
2. Let package cache regenerate if needed.
3. Reopen project.
4. Confirm `TrainingSimulation` still exists.

## Non-Blocking Warnings

These warnings are known and did not block the verified Quest run:

- obsolete `FindObjectOfType` warnings,
- obsolete TextMeshPro word wrapping warnings,
- URP SSAO performance warning,
- Active Input Handling set to `Both`,
- shader warnings from Unity packages,
- missing Meta XR audio/acoustic settings if those packages are not installed.

## Do Not Delete These

Do not remove:

```text
Assets/Scenes/TrainingSimulation.unity
Assets/Scripts
Assets/Plugins/Android/AndroidManifest.xml
Assets/Resources/OculusRuntimeSettings.asset
Assets/Resources/OVRBuildConfig.asset
Assets/XR
Packages/manifest.json
Packages/packages-lock.json
ProjectSettings
```

## Optional Cleanup Later

Only after final submission or on a backup cleanup branch:

```text
Assets/TextMesh Pro/Examples & Extras
Assets/_Recovery
Assets/Readme.asset
Assets/TutorialInfo
```

These are not urgent because the project is currently working.

[First](00_START_HERE.md) | [Previous](07_SCRIPT_GUIDE.md) | [Next](09_FINAL_OUTPUT_AND_SUBMISSION.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

