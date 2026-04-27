# Implementation Details

## Platform

* Unity (3D)
* C#

## Scripts

### SignalCapture.cs

Captures signals:

* Speed
* Angle
* Interaction

### ContextStateDetector.cs

Applies classification logic.

### XRAdaptation.cs

Controls visual feedback.

### GazeRaycast.cs

Detects which object user is looking at.

## Input Simulation

* Mouse → Look direction
* WASD → Movement
* Left Click → Interaction (Pinch simulation)

## Note

Body motion is approximated using head movement in the current prototype.
