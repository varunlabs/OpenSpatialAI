# Script Reference

This document explains the role of each project script so another developer can understand the Unity project without reverse-engineering it from the scene.

## Phase 1: Signal Capture

### `Assets/Scripts/Phase1/SignalFrame.cs`

Defines the shared runtime signal packet. It stores gaze, head, posture, hand, spatial, stale-source, and context-state fields. This is the common data shape passed through the pipeline.

### `Assets/Scripts/Phase1/GazeCapture.cs`

Captures gaze ray information such as origin, direction, fixation state, fixation duration, and area-of-interest hit. It provides gaze evidence for attention and context inference.

### `Assets/Scripts/Phase1/GazeHighlight.cs`

Provides visual feedback for gaze targeting. It is used to show which object or area is currently being looked at.

### `Assets/Scripts/Phase1/BodyPoseCapture.cs`

Captures body/head posture-related data, including posture class, spine/head information, and movement values. It supports body activity and idle/transition inference.

### `Assets/Scripts/Phase1/HandCapture.cs`

Captures hand interaction evidence such as pinch state, interaction frequency, and proximity to interactable objects. It supports task engagement detection.

### `Assets/Scripts/Phase1/SpatialContextDetector.cs`

Infers spatial context such as boundary type, posture mode, and locomotion activity. It helps distinguish standing, room-scale movement, and transition behavior.

### `Assets/Scripts/Phase1/SignalSynchroniser.cs`

Combines gaze, body, hand, and spatial capture outputs into a synchronized `SignalFrame` on a regular cadence. This is the bridge between raw capture scripts and the inference layer.

### `Assets/Scripts/Phase1/XRDebugUI.cs`

Displays runtime signal values for debugging in the Unity scene. Useful during editor and headset validation.

## Phase 2: Feature Extraction

### `Assets/Scripts/Phase2/GazeFeatureExtractor.cs`

Converts raw gaze signal fields into a `GazeFeatureVector`, including AOI fixation, dwell ratio, and saccade-like activity values used by the context engine.

### `Assets/Scripts/Phase2/BodyFeatureExtractor.cs`

Converts raw body/posture values into a `BodyFeatureVector`, including posture label and average joint movement. This supports idle and movement-state decisions.

### `Assets/Scripts/Phase2/HandFeatureExtractor.cs`

Converts hand capture values into a `HandFeatureVector`, including pinch state, interaction frequency, and object proximity.

### `Assets/Scripts/Phase2/BodyHandFeatureExtractor.cs`

Combines body and hand features into a joint feature vector for logic that needs both movement and interaction evidence.

### `Assets/Scripts/Phase2/GazeFeatureDebugTester.cs`

Debug helper for checking gaze feature extraction in the scene.

### `Assets/Scripts/Phase2/BodyHandFeatureDebugTester.cs`

Debug helper for checking body and hand feature extraction in the scene.

## Phase 2.3: Context Inference

### `Assets/Scripts/Phase2_3/ContextEngine.cs`

Core interpretable context engine. It evaluates gaze, body, hand, and spatial feature vectors and returns a `ContextState` plus confidence. States are `Engaged`, `Distracted`, `Idle`, and `Transitioning`.

### `Assets/Scripts/Phase2_3/SpatialContextVector.cs`

Defines spatial context enums and vector data such as posture mode, boundary type, and locomotion activity.

### `Assets/Scripts/Phase2_3/ContextFusionEngine.cs`

Provides rule/fusion support for combining multiple context signals. It is part of the inference layer for explainable context decisions.

### `Assets/Scripts/Phase2_3/ContextStateMachine.cs`

Stabilizes state transitions over time so the app does not rapidly flicker between states. It tracks state hold duration and transitions.

### `Assets/Scripts/Phase2_3/ContextLogger.cs`

Writes context output records for later analysis. This supports evidence generation and QA review.

### `Assets/Scripts/Phase2_3/ContextDebugTester.cs`

Scene-level test component for context inference. It links capture, feature extraction, UI display, and debug behavior.

### `Assets/Scripts/Phase2_3/ContextStateUIDisplay.cs`

Displays the current context state and confidence in the UI.

### `Assets/Scripts/Phase2_3/CubeStateDisplay.cs`

Displays cube/task state information for debugging and visual validation.

## Phase 3: XR Application Layer

### `Assets/Scripts/Phase3/XRAppShellController.cs`

Top-level runtime shell for the training simulation. It ensures the XR rig, user guide, adaptation manager, debug overlay, and training flow are present. It also contains the camera fallback added during Unity 6 migration so the scene keeps a valid render camera.

### `Assets/Scripts/Phase3/AdaptationManager.cs`

Maps inferred context state into user-facing adaptive responses. It manages guidance, focus nudges, idle support, spatial cues, visual effects, and QA metric output.

### `Assets/Scripts/Phase3/TrainingSimulationUserGuide.cs`

Builds and controls the headset-facing onboarding, instructions, guidance messages, idle support, and completion summary UI.

### `Assets/Scripts/Phase3/DebugOverlayController.cs`

Creates and updates a runtime debug/QA overlay with signal, state, task, and evidence values.

### `Assets/Scripts/Phase3/TrainingSimulationSceneBuilder.cs`

Builds or aligns the basic training simulation layout. It supports the cube/cylinder/sphere sorting task setup.

### `Assets/Scripts/Phase3/TrainingSimulationTaskManager.cs`

Tracks active task progress and completion. It knows which object should be sorted and when all tasks are complete.

### `Assets/Scripts/Phase3/SortableTaskObject.cs`

Marks a scene object as sortable and stores its `TaskObjectType`.

### `Assets/Scripts/Phase3/ReceptacleTrigger.cs`

Detects when a sortable task object enters a matching receptacle and raises placement/completion events.

### `Assets/Scripts/Phase3/TaskObjectType.cs`

Defines supported task object types: `Cube`, `Cylinder`, and `Sphere`.

### `Assets/Scripts/Phase3/AOIMarker.cs`

Marks a scene object as an area of interest so gaze and context logic can identify relevant targets.

### `Assets/Scripts/Phase3/XRContextSnapshot.cs`

Stores a compact snapshot of context state, confidence, boundary type, posture mode, timestamp, and AOI hit.

### `Assets/Scripts/Phase3/HeadsetUiButton.cs`

Provides headset-friendly button behavior and visual state handling for runtime UI controls.

## Shared Utility Scripts

### `Assets/Scripts/XRUILayoutApplier.cs`

Applies consistent XR UI layout, sizing, and text formatting to headset-facing panels.

### `Assets/Scripts/SimpleCameraMove.cs`

Development/editor helper for simple camera movement. It is not the primary Quest runtime movement system.

## Runtime Flow Summary

1. Capture scripts collect gaze, body/head, hand, and spatial signals.
2. `SignalSynchroniser` creates a shared `SignalFrame`.
3. Feature extractors convert raw signals into feature vectors.
4. `ContextEngine` evaluates the feature vectors.
5. `ContextStateMachine` stabilizes context state changes.
6. `AdaptationManager` and UI scripts convert the inferred state into visible XR guidance.
7. Task scripts track object sorting progress and completion.
8. Debug/logger scripts preserve evidence for validation.
