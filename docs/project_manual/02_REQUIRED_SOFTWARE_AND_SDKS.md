# Required Software And SDKs

[First](00_START_HERE.md) | [Previous](01_PROJECT_OVERVIEW.md) | [Next](03_REPOSITORY_AND_PROJECT_STRUCTURE.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

## Unity Editor

Install:

```text
Unity 6000.4.7f1
```

Install through Unity Hub. The project was verified with this exact version.

## Unity Modules

Install these Unity modules with the editor:

- Android Build Support
- Android SDK and NDK Tools
- OpenJDK

Without these modules, the Quest APK build will fail.

## Target Device

Verified device:

```text
Meta Quest 2
```

Device requirements:

- Developer Mode enabled.
- USB debugging allowed.
- USB cable connected during Build And Run.

## Unity Packages

The current project uses these important packages:

- `com.meta.xr.sdk.core`: `201.0.0`
- `com.unity.xr.openxr`: `1.16.1`
- `com.unity.xr.management`: `4.6.0`
- `com.unity.render-pipelines.universal`: `17.4.0`
- `com.unity.inputsystem`: `1.19.0`
- `com.unity.textmeshpro`: `5.0.0`
- `com.unity.ugui`: `2.0.0`

Package source file:

```text
HarmonyXR_GazePoseContext/Packages/manifest.json
```

Package lock file:

```text
HarmonyXR_GazePoseContext/Packages/packages-lock.json
```

## Meta XR Package Decision

The project uses Meta XR Core directly:

```text
com.meta.xr.sdk.core
```

The full Meta XR All-in-One package was removed from the current Unity 6 build because it caused duplicate Oculus Android namespace conflicts during Gradle manifest processing.

## Android Activity Requirement

Unity 6 requires the Android manifest to launch:

```text
com.unity3d.player.UnityPlayerGameActivity
```

This is defined in:

```text
HarmonyXR_GazePoseContext/Assets/Plugins/Android/AndroidManifest.xml
```

Do not replace it with:

```text
com.unity3d.player.appui.AppUIGameActivity
```

That class caused the Quest loading-screen crash.

[First](00_START_HERE.md) | [Previous](01_PROJECT_OVERVIEW.md) | [Next](03_REPOSITORY_AND_PROJECT_STRUCTURE.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

