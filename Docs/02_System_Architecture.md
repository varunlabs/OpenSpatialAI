# System Architecture

## Overview

The system follows a real-time processing pipeline:

Signal Capture → Feature Extraction → Context Inference → XR Adaptation

## Components

### 1. Signal Capture

Captures:

* Head position and rotation
* Interaction input (simulated pinch)
* Target object reference

### 2. Feature Extraction

Computes:

* Movement speed
* Angle to target
* Interaction state

### 3. Context Inference

Applies rule-based logic to classify user state.

### 4. XR Adaptation

Applies visual feedback:

* Color changes
* UI panels

## Design Principle

Lightweight, real-time, and deployable on XR devices.
