# GazePose-Context XR Prototype

## Overview

This project implements a behavior-centered multimodal framework for real-time context modeling in Extended Reality (XR) environments.

The system integrates:

* Head pose
* Motion signals
* Interaction input (click/pinch simulation)

to infer user context in real time without requiring specialized hardware.

## Objective

To demonstrate that lightweight, rule-based multimodal fusion can effectively classify user states in XR environments.

## Context States

The system classifies user behavior into four states:

* **Engaged** — User is focused and interacting with task objects
* **Exploring** — User is observing or scanning the environment
* **Distracted** — User attention is away from task
* **Idle** — User is inactive

## System Architecture

The system is structured into three layers:

1. **Signal Capture Layer**

   * Captures head movement, interaction signals, and object focus

2. **Context Engine**

   * Extracts features (speed, angle, interaction)
   * Applies rule-based classification

3. **XR Adaptation Layer**

   * Applies UI and feedback changes based on detected state

## Features Implemented

* Real-time state detection
* Rule-based fusion logic
* Temporal stability handling
* Basic adaptive UI feedback
* Debug logging of signals and states

## Technologies Used

* Unity (3D)
* C#
* XR Simulation (keyboard/mouse-based testing)

## Current Status

* Prototype: Functional and tested in Unity Editor
* Context detection: Working for all 4 states
* Adaptation: Basic UI responses implemented

## Limitations

* No gaze tracking (simulated using camera direction)
* No real hand tracking (interaction simulated via input)
* No user study conducted yet
* No latency benchmarking

## Future Work

* Integrate gaze tracking (OpenXR)
* Add hand tracking (OVRHand)
* Conduct user study and evaluation
* Add machine learning-based classification
* Optimize for Meta Quest deployment

## Repository Structure

```
Assets/
 ├── Scripts/
 │    ├── SignalCapture.cs
 │    ├── ContextStateDetector.cs
 │    ├── XRAdaptation.cs
 │    ├── GazeRaycast.cs
 │    └── ...
 ├── Scenes/
 │    └── SampleScene
```

## How to Run

1. Open project in Unity
2. Open `SampleScene`
3. Click Play
4. Use:

   * Mouse → Look direction
   * Click → Interaction
   * WASD → Movement

## Author

Lavanya D
VeeRuby Technologies

## Note

This project is part of the Harmony XR + AI research initiative and supports an arXiv preprint under development.
