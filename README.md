# OpenSpatialAI

OpenSpatialAI is a research repository for spatial intelligence, adaptive XR, and context-aware immersive systems. The repository is used to develop and document XR prototypes that combine headset runtime signals, scene context, and interaction evidence to support intelligent behavior inside immersive applications.

The current active prototype in this repository is **HarmonyXR_GazePoseContext**, a Unity and Meta Quest proof-of-concept for real-time multimodal context inference.

## Repository Scope

This repository is organized to support three areas of work:

- Unity XR implementation and Quest build validation.
- Research documentation, project reports, and paper outputs.
- Evidence collection, processed validation tables, and runtime QA notes.

The repository is not only an application source folder. It also contains the documentation and evidence needed for review, reproduction, and continuation of the research work.

## Repository Structure

```text
OpenSpatialAI/
  HarmonyXR_GazePoseContext/      Unity XR project
  docs/                           project manual, final documents, and archive
  Research_Evidence/              headset evidence, logs, processed tables, and notes
  README.md                       repository overview
  LICENSE                         Apache-2.0 license
```

## Current Project

### HarmonyXR_GazePoseContext

HarmonyXR_GazePoseContext demonstrates real-time XR context inference using multimodal runtime signals:

- gaze and area-of-interest attention,
- head/body posture and movement,
- hand interaction and pinch activity,
- spatial/boundary context,
- task progress during a guided training scene.

The prototype uses a simple object-sorting task so the system can observe user behavior and infer user context without adding unnecessary gameplay complexity.

## Research Objective

The research goal is to test whether lightweight runtime signals available inside an XR application can be combined to infer user context and drive adaptive XR guidance.

The system classifies behavior into four context states:

- `Engaged`
- `Distracted`
- `Transitioning`
- `Idle`

These states are used to trigger adaptive guidance, task feedback, idle support, completion messaging, and debug/evidence output.

## Architecture

The implementation follows a three-layer architecture:

1. Sensor Abstraction Layer: captures gaze, body/head, hand, and spatial signals.
2. Context Engine Layer: extracts features and infers context state.
3. XR Application Layer: converts inferred state into adaptive XR behavior.

## Verified Build Status

Current verified environment:

- Unity Editor: `6000.4.7f1`
- OpenXR Plugin: `1.16.1`
- Meta XR Core SDK: `201.0.0`
- Universal Render Pipeline: `17.4.0`
- Target device: Meta Quest 2
- Main scene: `Assets/Scenes/TrainingSimulation.unity`

The project has been built, installed, launched, and verified on Meta Quest 2.

## Unity Project Location

Open this folder in Unity Hub:

```text
HarmonyXR_GazePoseContext
```

Use Unity:

```text
6000.4.7f1
```

Main runtime scene:

```text
Assets/Scenes/TrainingSimulation.unity
```

## Important Build Note

The Quest Android manifest must use Unity 6 GameActivity:

```text
com.unity3d.player.UnityPlayerGameActivity
```

This is configured in:

```text
HarmonyXR_GazePoseContext/Assets/Plugins/Android/AndroidManifest.xml
```

Using the removed App UI activity class can cause the APK to remain on the Quest loading screen.

## Documentation

Start with the documentation index:

```text
docs/README.md
```

Complete project manual:

```text
docs/project_manual/
```

Recommended first page:

```text
docs/project_manual/00_START_HERE.md
```

Current paper and project report deliverables:

```text
docs/final_submission/lavanya/
```

Older drafts and reference exports:

```text
docs/archive/
```

## Evidence

Validation evidence is stored in:

```text
Research_Evidence/
```

Start with:

```text
Research_Evidence/00_EVIDENCE_INDEX.md
Research_Evidence/EVIDENCE_SUMMARY_20260514.md
```

Evidence includes headset video, console logs, filtered QA evidence, context-state coverage tables, adaptation event tables, and stability/latency notes.

## Current Status

The current Unity 6 Quest build is working on-device. Remaining work is primarily final submission packaging, final report/paper export regeneration if needed, demo capture, and repository backup.

## License

This repository is licensed under the Apache-2.0 License.
