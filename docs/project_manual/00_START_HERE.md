# Start Here - HarmonyXR Project Manual

[First](00_START_HERE.md) | Previous: None | [Next](01_PROJECT_OVERVIEW.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

## Purpose Of This Manual

This manual explains the HarmonyXR GazePose-Context project from initial setup to final Quest output. It is written for a new developer, reviewer, or evaluator who opens the public repository and needs to understand how to reproduce the project independently.

## Recommended Reading Order

1. `00_START_HERE.md` - what this documentation folder contains.
2. `01_PROJECT_OVERVIEW.md` - what the project is and what problem it solves.
3. `02_REQUIRED_SOFTWARE_AND_SDKS.md` - exact Unity, SDK, package, and device requirements.
4. `03_REPOSITORY_AND_PROJECT_STRUCTURE.md` - where important files and folders are located.
5. `04_INITIAL_SETUP_STEPS.md` - how to open the project from a fresh machine.
6. `05_UNITY_SCENE_AND_RUNTIME_FLOW.md` - how the scene works at runtime.
7. `06_BUILD_AND_RUN_ON_QUEST.md` - how to build, install, and run the APK on Meta Quest.
8. `07_SCRIPT_GUIDE.md` - what each script does and why it exists.
9. `08_TROUBLESHOOTING_AND_WARNINGS.md` - known warnings, fixes, and common failures.
10. `09_FINAL_OUTPUT_AND_SUBMISSION.md` - what to submit and how to verify final output.

## Current Verified Project State

- Unity project opens in Unity `6000.4.7f1`.
- Main runtime scene is `Assets/Scenes/TrainingSimulation.unity`.
- Android/Quest build was verified on Meta Quest 2.
- The app launches past the Quest loading screen and runs the simulation flow.
- The Unity 6 Android manifest issue was fixed by using `UnityPlayerGameActivity`.

## What Not To Do Before Submission

- Do not delete scenes or packages just because Unity shows warnings.
- Do not clean generated folders unless Unity is closed and there is a reason.
- Do not remove TextMeshPro or Unity UI packages; the UI depends on them.
- Do not change the Android manifest activity away from `UnityPlayerGameActivity`.
- Do not rebuild repeatedly unless a project file changed.

[First](00_START_HERE.md) | Previous: None | [Next](01_PROJECT_OVERVIEW.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

