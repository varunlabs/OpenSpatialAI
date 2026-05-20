# Build And Run On Quest

[First](00_START_HERE.md) | [Previous](05_UNITY_SCENE_AND_RUNTIME_FLOW.md) | [Next](07_SCRIPT_GUIDE.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

## Step 1: Open Build Settings

Open:

```text
File > Build Profiles
```

or:

```text
File > Build Settings
```

## Step 2: Select Android

Set platform:

```text
Android
```

If needed, click:

```text
Switch Platform
```

## Step 3: Confirm Scene List

Enabled scene:

```text
Assets/Scenes/TrainingSimulation.unity
```

Do not use `SampleScene` as the final scene.

## Step 4: Confirm Player Settings

Recommended settings:

- Scripting Backend: IL2CPP
- Target Architecture: ARM64
- Target SDK: Android API 34
- Minimum SDK: compatible with Quest build settings
- Application Entry: GameActivity

## Step 5: Confirm Android Manifest

Open:

```text
Assets/Plugins/Android/AndroidManifest.xml
```

Confirm activity:

```xml
android:name="com.unity3d.player.UnityPlayerGameActivity"
```

This is required for Unity 6 Quest launch.

## Step 6: Connect Quest

Connect Meta Quest 2 using USB. Accept debugging prompt inside headset.

## Step 7: Build And Run

Click:

```text
Build And Run
```

Expected behavior:

- Unity builds APK.
- APK installs on Quest.
- App launches automatically.
- Training simulation appears on headset.

## Step 8: Verify Runtime

Check:

- app opens past Quest loading screen,
- training environment is visible,
- guide UI appears,
- object sorting works,
- Continue / Take a break UI appears when relevant,
- completion flow works.

## Build Time Explanation

A first build after package migration may take 20-30 minutes because Unity regenerates:

- IL2CPP output,
- Android Gradle project,
- native libraries,
- shaders,
- package caches,
- build artifacts.

Do not repeatedly rebuild unless a source/project file changed.

[First](00_START_HERE.md) | [Previous](05_UNITY_SCENE_AND_RUNTIME_FLOW.md) | [Next](07_SCRIPT_GUIDE.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

