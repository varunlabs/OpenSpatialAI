# Script Guide

[First](00_START_HERE.md) | [Previous](06_BUILD_AND_RUN_ON_QUEST.md) | [Next](08_TROUBLESHOOTING_AND_WARNINGS.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

This page explains what each script is for, why it exists, and where it fits in the runtime pipeline.

## Phase 1 - Signal Capture

### `Assets/Scripts/Phase1/SignalFrame.cs`

Common data container for one synchronized frame of runtime evidence. It stores gaze, head, body, hand, spatial context, stale-source flags, context state, confidence, and nearest interactable values.

### `Assets/Scripts/Phase1/GazeCapture.cs`

Captures gaze direction, gaze origin, fixation information, and AOI hit values. This provides attention evidence.

### `Assets/Scripts/Phase1/GazeHighlight.cs`

Shows visual feedback for the object or area currently targeted by gaze.

### `Assets/Scripts/Phase1/BodyPoseCapture.cs`

Captures posture and body/head movement values. It helps identify movement, stillness, and idle behavior.

### `Assets/Scripts/Phase1/HandCapture.cs`

Captures hand activity such as pinch state, interaction count, and object proximity. It helps identify whether the user is actively engaging with task objects.

### `Assets/Scripts/Phase1/SpatialContextDetector.cs`

Detects spatial context such as posture mode, boundary type, and locomotion movement.

### `Assets/Scripts/Phase1/SignalSynchroniser.cs`

Combines gaze, body, hand, and spatial data into one `SignalFrame`. This is the main bridge from raw capture to context inference.

### `Assets/Scripts/Phase1/XRDebugUI.cs`

Displays raw and synchronized signal values for debugging.

## Phase 2 - Feature Extraction

### `Assets/Scripts/Phase2/GazeFeatureExtractor.cs`

Converts raw gaze values into gaze features such as AOI dwell, fixation-on-AOI, and saccade-like activity.

### `Assets/Scripts/Phase2/BodyFeatureExtractor.cs`

Converts body/posture values into body features such as posture label and average joint velocity.

### `Assets/Scripts/Phase2/HandFeatureExtractor.cs`

Converts hand values into hand features such as interaction frequency, pinch state, and object proximity.

### `Assets/Scripts/Phase2/BodyHandFeatureExtractor.cs`

Combines body and hand evidence for logic that needs movement plus interaction together.

### `Assets/Scripts/Phase2/GazeFeatureDebugTester.cs`

Debug helper for validating gaze feature output.

### `Assets/Scripts/Phase2/BodyHandFeatureDebugTester.cs`

Debug helper for validating combined body and hand feature output.

## Phase 2.3 - Context Inference

### `Assets/Scripts/Phase2_3/ContextEngine.cs`

Core context classifier. It reads gaze, body, hand, and spatial features and returns a state plus confidence.

### `Assets/Scripts/Phase2_3/SpatialContextVector.cs`

Defines spatial context data and enums such as posture mode, boundary type, and locomotion activity.

### `Assets/Scripts/Phase2_3/ContextFusionEngine.cs`

Supports rule-based fusion of context evidence. It keeps decisions interpretable instead of using an opaque model.

### `Assets/Scripts/Phase2_3/ContextStateMachine.cs`

Stabilizes context changes over time so the UI does not flicker rapidly between states.

### `Assets/Scripts/Phase2_3/ContextLogger.cs`

Logs context output for validation and evidence review.

### `Assets/Scripts/Phase2_3/ContextDebugTester.cs`

Scene-level debug component that connects context inference with debug displays.

### `Assets/Scripts/Phase2_3/ContextStateUIDisplay.cs`

Displays the current inferred context state and confidence.

### `Assets/Scripts/Phase2_3/CubeStateDisplay.cs`

Displays cube/task state information for debugging.

## Phase 3 - XR Application Layer

### `Assets/Scripts/Phase3/XRAppShellController.cs`

Main app shell. It ensures required runtime objects exist, aligns the training scene, creates fallback UI/support components, and protects rendering by keeping a valid camera if XR rig bootstrapping is incomplete.

### `Assets/Scripts/Phase3/AdaptationManager.cs`

Turns context state into adaptive XR behavior. It controls guidance, idle support, focus cues, visual feedback, spatial cue behavior, and QA output.

### `Assets/Scripts/Phase3/TrainingSimulationUserGuide.cs`

Creates and manages onboarding, task instructions, context guidance, idle controls, and completion summary.

### `Assets/Scripts/Phase3/DebugOverlayController.cs`

Creates runtime debug overlay for signal/state/task values. Useful for validation, evidence capture, and troubleshooting.

### `Assets/Scripts/Phase3/TrainingSimulationSceneBuilder.cs`

Builds or aligns the simulation layout. It supports task object and receptacle placement.

### `Assets/Scripts/Phase3/TrainingSimulationTaskManager.cs`

Tracks task progress, active object, completed objects, and completion event.

### `Assets/Scripts/Phase3/SortableTaskObject.cs`

Marks a task object as sortable and assigns its object type.

### `Assets/Scripts/Phase3/ReceptacleTrigger.cs`

Detects whether a task object was placed into the correct receptacle.

### `Assets/Scripts/Phase3/TaskObjectType.cs`

Enum for task object categories: cube, cylinder, sphere.

### `Assets/Scripts/Phase3/AOIMarker.cs`

Marks objects as areas of interest for gaze/context logic.

### `Assets/Scripts/Phase3/XRContextSnapshot.cs`

Compact runtime snapshot of state, confidence, boundary type, posture mode, timestamp, and AOI hit.

### `Assets/Scripts/Phase3/HeadsetUiButton.cs`

Headset-friendly button helper for UI controls.

## Shared Scripts

### `Assets/Scripts/XRUILayoutApplier.cs`

Applies consistent sizing and layout to XR UI text and panels.

### `Assets/Scripts/SimpleCameraMove.cs`

Editor/development camera movement helper. It is not the final Quest movement system.

[First](00_START_HERE.md) | [Previous](06_BUILD_AND_RUN_ON_QUEST.md) | [Next](08_TROUBLESHOOTING_AND_WARNINGS.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

