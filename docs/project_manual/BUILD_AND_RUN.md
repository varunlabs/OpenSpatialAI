# Build And Run Guide

## Required Environment

- Unity Editor: `6000.4.7f1`
- Platform: Android
- Target headset: Meta Quest 2
- Unity modules required:
  - Android Build Support
  - Android SDK and NDK Tools
  - OpenJDK
- USB debugging enabled on the Quest headset.

## Open The Project

1. Open Unity Hub.
2. Add or open this project folder:

```text
HarmonyXR_GazePoseContext
```

3. Use Unity `6000.4.7f1`.
4. Wait for package import to finish.
5. If Unity asks to enter Safe Mode, do not delete scenes. Let packages finish resolving first.

## Important Scene

Only this scene is required for the current app:

```text
Assets/Scenes/TrainingSimulation.unity
```

Build Settings currently keep `TrainingSimulation` enabled. `SampleScene` is not part of the required runtime flow.

## Android Build Settings

Use:

- Platform: Android
- Architecture: ARM64
- Scripting backend: IL2CPP
- Target SDK: Android API 34
- Application entry: GameActivity

## Meta XR Setup

Open:

```text
Edit > Project Settings > Meta XR
```

Apply required fixes if Unity shows them. The important migration fix was using a single GameActivity entry for Unity 2023.2+ / Unity 6.

## Build And Run

1. Connect Meta Quest 2 by USB.
2. Confirm the device is visible in Build Settings.
3. Click `Build And Run`.
4. Wait for the Gradle/IL2CPP build to complete.
5. Unity should install and launch the APK on the headset.

The verified build installed and launched on Quest 2 after the Android manifest was updated to:

```text
com.unity3d.player.UnityPlayerGameActivity
```

## Expected Runtime Result

On Quest, the app should:

- leave the Quest loading screen,
- open the Unity XR scene,
- show the training simulation environment,
- display Continue / Take a break style UI during the flow,
- allow gaze/task interaction,
- complete the sorting task.

## If The App Stays On Quest Loading Screen

Check Android logcat. The known migration failure was:

```text
ClassNotFoundException: com.unity3d.player.appui.AppUIGameActivity
```

Fix:

```text
Assets/Plugins/Android/AndroidManifest.xml
```

The activity must be:

```xml
android:name="com.unity3d.player.UnityPlayerGameActivity"
```

not:

```xml
android:name="com.unity3d.player.appui.AppUIGameActivity"
```

## Build Time Note

The first Quest build after migration can take 20-30 minutes because Unity regenerates Library, IL2CPP, shaders, Gradle files, and Android native build outputs. Later builds should be faster if Library and Gradle cache remain valid.
