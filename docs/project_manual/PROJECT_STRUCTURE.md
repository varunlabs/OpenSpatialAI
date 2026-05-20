# Project Structure

## Repository Root

```text
OpenSpatialAI/
```

Main contents:

- `HarmonyXR_GazePoseContext/`: Unity XR project.
- `docs/`: project manual, final deliverables, and archived reference documentation.
- `Research_Evidence/`: evidence notes, run checklists, and validation artifacts.
- `README.md`: public project overview.

## Unity Project

```text
HarmonyXR_GazePoseContext/
```

Important folders:

- `Assets/Scenes`: Unity scenes.
- `Assets/Scripts`: implementation scripts grouped by project phase.
- `Assets/Plugins/Android`: Android manifest used for Quest builds.
- `Assets/Resources`: OVR/OpenXR runtime settings and project resources.
- `Assets/Settings`: URP render pipeline assets.
- `Assets/XR`: XR Management and OpenXR project settings.
- `Packages`: Unity package manifest and lock file.
- `ProjectSettings`: Unity project settings.

## Runtime Scene

Primary scene:

```text
Assets/Scenes/TrainingSimulation.unity
```

This is the scene used for Quest execution and final demo validation.

Disabled/non-primary scene:

```text
Assets/Scenes/SampleScene.unity
```

This scene is not needed for the current submission flow.

## Script Organization

Scripts are grouped by research implementation phase:

- `Phase1`: signal capture and synchronization.
- `Phase2`: feature extraction.
- `Phase2_3`: context inference, state machine, logging, and debug display.
- `Phase3`: XR application shell, adaptive behavior, task flow, UI, and simulation management.

## Resources

Keep these files:

- `Assets/Resources/OculusRuntimeSettings.asset`
- `Assets/Resources/OVRBuildConfig.asset`
- `Assets/Resources/OVROverlayCanvasSettings.asset`
- `Assets/Resources/OVRPlatformToolSettings.asset`
- `Assets/Resources/InputActions.asset`

Possible cleanup candidates after submission:

- `Assets/TextMesh Pro/Examples & Extras`
- `Assets/_Recovery`
- `Assets/Readme.asset`
- `Assets/TutorialInfo`

These are not currently blocking runtime behavior, so they were not deleted during final validation.

## Android Manifest

Critical file:

```text
Assets/Plugins/Android/AndroidManifest.xml
```

This file contains Quest permissions and the Unity 6 activity entry point. The activity must remain:

```text
com.unity3d.player.UnityPlayerGameActivity
```
