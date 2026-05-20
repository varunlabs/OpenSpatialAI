# Project Overview

[First](00_START_HERE.md) | [Previous](00_START_HERE.md) | [Next](02_REQUIRED_SOFTWARE_AND_SDKS.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

## Project Name

HarmonyXR GazePose-Context

## Project Type

Unity XR proof-of-concept for Meta Quest.

## Research Goal

The project demonstrates how an XR system can infer a user's context during a task by combining multiple runtime signals:

- gaze and area-of-interest focus,
- head/body posture and motion,
- hand interaction and pinch activity,
- spatial context,
- task progress.

The goal is not to create a complex game. The simple sorting task exists so the system can observe user behavior and infer whether the user is focused, distracted, transitioning, or idle.

## Main Runtime States

The context system classifies behavior into four states:

- `Engaged`: user appears focused on task-relevant objects or guidance.
- `Distracted`: user attention appears away from the active task.
- `Transitioning`: user appears to be moving between task steps or targets.
- `Idle`: user appears paused or inactive.

## User Experience

The user enters a simple XR training scene and sorts objects into matching receptacles:

- cube,
- cylinder,
- sphere.

During the task, the app shows guidance, state feedback, adaptive support, and completion messaging.

## System Architecture

The project follows a three-layer architecture:

1. Sensor Abstraction Layer: captures gaze, body/head, hand, and spatial signals.
2. Context Engine Layer: converts signals into features and classifies context state.
3. XR Application Layer: uses the context state to guide the user and manage the training task.

## Current Verified Output

The project has been migrated to Unity `6000.4.7f1` and verified on Meta Quest 2. The APK installs, launches, and runs the main simulation flow.

[First](00_START_HERE.md) | [Previous](00_START_HERE.md) | [Next](02_REQUIRED_SOFTWARE_AND_SDKS.md) | [Last](09_FINAL_OUTPUT_AND_SUBMISSION.md)

