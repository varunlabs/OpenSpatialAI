# Usage Guide

## How to Run

1. Open project in Unity
2. Open SampleScene
3. Click Play

## Controls

* Mouse → Look
* WASD → Move
* Left Click → Interact

## Expected Behavior

* Idle → No movement → Gray state
* Exploring → Move → Yellow state
* Distracted → Look away → Red state
* Engaged → Look + click → Green state

## Troubleshooting

* If no movement → check CameraController
* If no state change → check SignalCapture
* If no UI → check XRAdaptation
