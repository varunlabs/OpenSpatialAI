# Initial Setup Steps

[First](00_START_HERE.md) | [Previous](03_REPOSITORY_AND_PROJECT_STRUCTURE.md) | [Next](05_UNITY_SCENE_AND_RUNTIME_FLOW.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

## Step 1: Clone Or Download Repository

Place the repository on the local machine:

```text
OpenSpatialAI/
```

## Step 2: Install Unity

Install Unity:

```text
6000.4.7f1
```

Include Android Build Support, Android SDK/NDK Tools, and OpenJDK.

## Step 3: Open Unity Project

In Unity Hub, open:

```text
OpenSpatialAI/HarmonyXR_GazePoseContext
```

Wait for package import to finish.

## Step 4: Let Unity Resolve Packages

Unity may take time to import packages after a fresh clone. Do not delete scenes or assets during this stage.

Expected package sources:

```text
Packages/manifest.json
Packages/packages-lock.json
```

## Step 5: Confirm Main Scene

Open:

```text
Assets/Scenes/TrainingSimulation.unity
```

## Step 6: Confirm Build Settings

Open:

```text
File > Build Profiles
```

or:

```text
File > Build Settings
```

Confirm:

- Platform is Android.
- `TrainingSimulation.unity` is enabled.
- `SampleScene.unity` is disabled or not used.

## Step 7: Confirm Meta XR Settings

Open:

```text
Edit > Project Settings > Meta XR
```

If Unity shows required fixes, apply them carefully. The verified build uses GameActivity for Unity 6.

## Step 8: Confirm Quest Device

Connect Quest by USB. Accept USB debugging prompt inside headset.

Optional ADB check:

```text
adb devices
```

Expected result:

```text
<device-id> device
```

[First](00_START_HERE.md) | [Previous](03_REPOSITORY_AND_PROJECT_STRUCTURE.md) | [Next](05_UNITY_SCENE_AND_RUNTIME_FLOW.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

