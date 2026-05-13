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

## Research Context

This project is part of the Harmony XR + AI framework and contributes to the GazePose-Context prototype for XR behavior modeling.

## Quest APK Note

The Android APK is intended for Meta Quest headset deployment.
If the APK is opened on a normal phone, a black screen is expected because the build depends on headset XR runtime services and headset-rendered scene flow rather than standard mobile app presentation.

## Context Logs

Runtime context logs are written as JSON Lines (`.jsonl`) in Unity's persistent data path. Each line is a complete JSON record, which keeps headset logging stream-safe while remaining parseable by Python, Unity, and standard data tools.

