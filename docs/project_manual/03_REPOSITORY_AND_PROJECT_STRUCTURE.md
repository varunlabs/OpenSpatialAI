# Repository And Project Structure

[First](00_START_HERE.md) | [Previous](02_REQUIRED_SOFTWARE_AND_SDKS.md) | [Next](04_INITIAL_SETUP_STEPS.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

## Repository Root

```text
OpenSpatialAI/
```

Important folders:

- `HarmonyXR_GazePoseContext/`: Unity project.
- `docs/`: project manual, final deliverables, and archived reference documentation.
- `docs/project_manual/`: this project manual.
- `docs/final_submission/lavanya/`: current Lavanya paper and project report deliverables.
- `docs/archive/`: older drafts and reference exports.
- `Research_Evidence/`: evidence notes and validation material.
- `README.md`: public project overview.

## Unity Project Root

```text
OpenSpatialAI/HarmonyXR_GazePoseContext/
```

Important Unity folders:

- `Assets/Scenes`: Unity scenes.
- `Assets/Scripts`: project C# scripts.
- `Assets/Plugins/Android`: custom Android manifest for Quest.
- `Assets/Resources`: runtime settings and OVR assets.
- `Assets/Settings`: URP quality/render pipeline assets.
- `Assets/XR`: XR/OpenXR settings.
- `Packages`: Unity package dependency files.
- `ProjectSettings`: Unity project configuration.

## Main Scene

Use this scene:

```text
Assets/Scenes/TrainingSimulation.unity
```

This is the final runtime scene for Quest.

## Disabled Scene

This scene exists but is not required for the main build:

```text
Assets/Scenes/SampleScene.unity
```

It should not be used as the final output scene.

## Important Runtime Files

Keep these files:

```text
Assets/Plugins/Android/AndroidManifest.xml
Assets/Resources/OculusRuntimeSettings.asset
Assets/Resources/OVRBuildConfig.asset
Assets/Resources/InputActions.asset
Assets/Scenes/TrainingSimulation.unity
ProjectSettings/EditorBuildSettings.asset
Packages/manifest.json
Packages/packages-lock.json
```

## Files Reviewed But Not Removed

These were reviewed as possible cleanup candidates but kept because the project is already working:

```text
Assets/TextMesh Pro/Examples & Extras
Assets/_Recovery
Assets/Readme.asset
Assets/TutorialInfo
```

Keeping them is safer before final submission. They can be removed later on a cleanup branch if size becomes a problem.

[First](00_START_HERE.md) | [Previous](02_REQUIRED_SOFTWARE_AND_SDKS.md) | [Next](04_INITIAL_SETUP_STEPS.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)
