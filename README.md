# HarmonyXR_GazePoseContext

## Project Overview

HarmonyXR_GazePoseContext is a Unity XR proof-of-concept for multimodal context inference.
It combines gaze, head pose, body posture, hand interactions, and spatial context to estimate what the user is doing during a simple training activity.

The current proof-of-concept scene uses a short sorting task because it provides a controlled way to observe attention, interaction, and task transitions without adding extra gameplay complexity.

## Objective

The research goal is not only to complete the sorting task. The goal is to test whether multimodal XR signals can infer user context in real time and support adaptive XR behavior without asking the user to manually explain their state.

The system classifies behavior into four states:

* Engaged
* Distracted
* Transitioning
* Idle

Plain-language meaning:

* Engaged: the user appears focused on the active task
* Distracted: attention appears to have shifted away from the task
* Transitioning: the user appears to be moving between task steps or targets
* Idle: the user appears paused with little task-directed activity

These states are then used to trigger lightweight adaptive XR responses such as focus nudges, transition support, or rest-oriented controls.

## Why The Task Exists

The sorting task is intentionally simple.
It exists to demonstrate the value of context inference, not to act as a standalone gameplay experience.

During the task, the prototype shows:

* what the user is being asked to do
* what the system is detecting from existing signals
* how context states change as the user focuses, shifts attention, transitions, or pauses
* why adaptive behavior matters for usability and responsiveness in XR

## Architecture

The system follows a 3-layer architecture:

1. Sensor Abstraction Layer (signal capture)
2. Context Engine (feature extraction and state inference)
3. XR Application Layer (adaptive behaviors)

## Repository Details

* Repository: OpenSpatialAI
* Base branch for this proof-of-concept line: `research/gaze-pose-context-v0`
* Follow-up communication branch: `research/gaze-pose-context-communication-v0`
* Unity 6 migration branch: `research/harmonyxr/unity-6.4-migration`

## Current Verified Build

The project has been migrated and verified on:

* Unity Editor: `6000.4.7f1`
* OpenXR Plugin: `1.16.1`
* Meta XR Core SDK: `201.0.0`
* Target device: Meta Quest 2
* Main scene: `Assets/Scenes/TrainingSimulation.unity`

The Quest build was verified to install, launch, and run the main simulation flow after updating the Android manifest to use Unity 6's `UnityPlayerGameActivity`.

## Project Manual

Detailed project documentation is available in:

* `docs/README.md`
* `docs/project_manual/00_START_HERE.md`
* `docs/project_manual/01_PROJECT_OVERVIEW.md`
* `docs/project_manual/02_REQUIRED_SOFTWARE_AND_SDKS.md`
* `docs/project_manual/03_REPOSITORY_AND_PROJECT_STRUCTURE.md`
* `docs/project_manual/04_INITIAL_SETUP_STEPS.md`
* `docs/project_manual/05_UNITY_SCENE_AND_RUNTIME_FLOW.md`
* `docs/project_manual/06_BUILD_AND_RUN_ON_QUEST.md`
* `docs/project_manual/07_SCRIPT_GUIDE.md`
* `docs/project_manual/08_TROUBLESHOOTING_AND_WARNINGS.md`
* `docs/project_manual/09_FINAL_OUTPUT_AND_SUBMISSION.md`

Current report and paper deliverables are organized under:

* `docs/final_submission/lavanya/paper`
* `docs/final_submission/lavanya/project_report`

Older drafts and reference files are kept under:

* `docs/archive`

## Research Context

This project is part of the Harmony XR + AI framework and contributes to the GazePose-Context prototype for XR behavior modeling.

## Quest APK Note

The Android APK is intended for Meta Quest headset deployment.
If the APK is opened on a normal phone, a black screen is expected because the build depends on headset XR runtime services and headset-rendered scene flow rather than standard mobile app presentation.

## Context Logs

Runtime context logs are written as JSON Lines (`.jsonl`) in Unity's persistent data path. Each line is a complete JSON record, which keeps headset logging stream-safe while remaining parseable by Python, Unity, and standard data tools.

