# Unity 6 Migration Notes

## Migration Target

- Previous project version in older documentation: Unity `2022.3.62f1`
- Current verified editor: Unity `6000.4.7f1`
- Target runtime: Meta Quest 2 Android build

## Main Migration Problems Encountered

### 1. Package Import And Safe Mode Issues

After opening the project in Unity 6, Unity reported package and missing namespace errors. The project was stabilized by allowing packages to resolve and regenerating Unity-generated cache folders when needed.

Important note: generated folders such as `Library`, `Library/PackageCache`, `Library/Bee`, and `.utmp` are not source assets.

### 2. Meta XR Package Namespace Conflict

The initial Quest build failed with duplicate Oculus namespace errors involving Meta XR package modules.

Fix:

- Replaced `com.meta.xr.sdk.all` with `com.meta.xr.sdk.core`.
- Removed unused full Meta package modules that were not required for the current prototype.

### 3. Missing Direct Dependencies

After removing large bundle packages, some direct dependencies had to be declared explicitly.

Added/kept:

- `com.unity.inputsystem`: `1.19.0`
- `com.unity.textmeshpro`: `5.0.0`
- `com.unity.ugui`: `2.0.0`
- `com.unity.render-pipelines.universal`: `17.4.0`
- `com.unity.xr.openxr`: `1.16.1`
- `com.unity.xr.management`: `4.6.0`

### 4. Android Activity Crash

The APK installed but stayed on the Quest loading screen. Logcat showed:

```text
ClassNotFoundException: com.unity3d.player.appui.AppUIGameActivity
```

Cause:

The Android manifest pointed to a Unity App UI activity class that was no longer present after the package cleanup.

Fix:

Updated:

```text
Assets/Plugins/Android/AndroidManifest.xml
```

from:

```text
com.unity3d.player.appui.AppUIGameActivity
```

to:

```text
com.unity3d.player.UnityPlayerGameActivity
```

### 5. Camera Fallback

`XRAppShellController` was updated so the existing main camera is not disabled unless the bootstrapped `OVRCameraRig` creates a valid render camera. This prevents a blank render if rig setup is incomplete.

## Current Known Non-Blocking Warnings

- Obsolete `FindObjectOfType` API warnings.
- TextMeshPro `enableWordWrapping` obsolete API warnings.
- URP SSAO performance warning for Quest.
- Active Input Handling set to `Both`.
- Shader warnings from Unity/URP/Sentis packages.
- Meta XR audio/acoustics settings can show missing-script warnings if Meta XR Audio/Acoustics packages are not installed.

These warnings did not block the verified Quest run.

## Verified Outcome

The project now:

- opens in Unity `6000.4.7f1`,
- builds for Android/Quest,
- installs to Meta Quest 2,
- launches past the Quest loading screen,
- runs the `TrainingSimulation` scene,
- supports the main interaction and simulation flow.
