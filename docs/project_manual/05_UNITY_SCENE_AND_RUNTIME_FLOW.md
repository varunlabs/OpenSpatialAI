# Unity Scene And Runtime Flow

[First](00_START_HERE.md) | [Previous](04_INITIAL_SETUP_STEPS.md) | [Next](06_BUILD_AND_RUN_ON_QUEST.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

## Main Scene

Runtime scene:

```text
Assets/Scenes/TrainingSimulation.unity
```

This scene contains the training task, XR app shell, task objects, receptacles, UI, and context/adaptation components.

## User Task

The user sorts three objects:

- cube,
- cylinder,
- sphere.

Each object has a matching receptacle. The task is intentionally simple so the research focus stays on context inference and adaptive XR guidance.

## Runtime Flow

1. Scene opens on Quest.
2. XR shell initializes.
3. User guide/onboarding appears.
4. Capture scripts collect gaze, body/head, hand, and spatial signals.
5. Signals are synchronized into a `SignalFrame`.
6. Feature extractors convert raw signals into feature vectors.
7. Context engine classifies state as `Engaged`, `Distracted`, `Transitioning`, or `Idle`.
8. Adaptation manager updates user guidance based on context.
9. Task manager tracks sorting progress.
10. Completion screen appears when all task objects are correctly sorted.

## Context States

### Engaged

User appears focused on task-relevant content or actively interacting with the task.

### Distracted

User attention appears away from task-relevant content.

### Transitioning

User appears to be moving between steps, objects, or interaction targets.

### Idle

User appears paused or inactive.

## Main Runtime Components

- `XRAppShellController`: ensures runtime shell and XR support objects exist.
- `TrainingSimulationUserGuide`: creates onboarding and guidance UI.
- `TrainingSimulationTaskManager`: tracks task sequence and completion.
- `AdaptationManager`: maps context state into adaptive responses.
- `DebugOverlayController`: shows runtime/debug evidence.
- `SignalSynchroniser`: produces synchronized signal frames.
- `ContextEngine`: evaluates state and confidence.

## Expected On-Device Result

The app should not stay on the Quest loading screen. It should enter the Unity scene and show the training simulation environment and UI.

[First](00_START_HERE.md) | [Previous](04_INITIAL_SETUP_STEPS.md) | [Next](06_BUILD_AND_RUN_ON_QUEST.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

