# HarmonyXR Project Manual

This folder is the complete project manual for the HarmonyXR GazePose-Context Unity project. It explains what the project does, how to open and build it, what each major script is responsible for, and what changed during the Unity 6 migration.

## Current Verified Status

- Unity project: `HarmonyXR_GazePoseContext`
- Unity Editor: `6000.4.7f1`
- Target device: Meta Quest 2
- Main scene: `Assets/Scenes/TrainingSimulation.unity`
- Android package id: `com.UnityTechnologies.com.unity.template.urpblank`
- Current status: Quest build installs, launches, and runs the training simulation flow on-device.

## Documentation Map

Use the numbered manual first:

- `00_START_HERE.md`: first page and reading order.
- `01_PROJECT_OVERVIEW.md`: project purpose and research goal.
- `02_REQUIRED_SOFTWARE_AND_SDKS.md`: exact software and SDK versions.
- `03_REPOSITORY_AND_PROJECT_STRUCTURE.md`: repository and Unity folder structure.
- `04_INITIAL_SETUP_STEPS.md`: how to open the project from scratch.
- `05_UNITY_SCENE_AND_RUNTIME_FLOW.md`: scene and runtime behavior.
- `06_BUILD_AND_RUN_ON_QUEST.md`: final Quest APK build process.
- `07_SCRIPT_GUIDE.md`: script-by-script explanation.
- `08_TROUBLESHOOTING_AND_WARNINGS.md`: known issues and fixes.
- `09_FINAL_OUTPUT_AND_SUBMISSION.md`: final output and submission checklist.

Additional summary files:

- `BUILD_AND_RUN.md`: how to open the project, configure Unity, build the APK, and test on Quest.
- `PROJECT_STRUCTURE.md`: what the main folders contain and which scene/assets are important.
- `SCRIPT_REFERENCE.md`: script-by-script explanation of the project code.
- `UNITY_6_MIGRATION_NOTES.md`: package, manifest, and runtime fixes made during the Unity 6 migration.
- `FINAL_STATUS_20260520.md`: current completion status, known warnings, and final submission checklist.

## What The Prototype Demonstrates

HarmonyXR GazePose-Context is a Unity XR proof-of-concept for adaptive context inference. During a simple sorting task, the system captures gaze, head/body, hand, spatial, and task signals. These signals are fused into four interpretable context states:

- `Engaged`
- `Distracted`
- `Transitioning`
- `Idle`

The app then uses those states to show adaptive guidance, task support, completion feedback, and debug/evidence information inside the XR experience.

## Reproducibility Goal

Someone opening the public repository should be able to:

1. Install the correct Unity version.
2. Open the Unity project without Safe Mode.
3. Understand which scene to run.
4. Build the APK for Meta Quest.
5. Understand the purpose of each script group.
6. Continue development without reverse-engineering the project from scratch.
