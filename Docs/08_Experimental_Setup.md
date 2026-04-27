# Experimental Setup

## Objective

To validate the correctness and stability of context state detection.

## Environment

* Unity simulation environment
* Single camera representing user head
* Target cube placed in scene

## Input Method

* Mouse → Head movement simulation
* WASD → Movement
* Left Click → Interaction (pinch simulation)

## Test Scenarios

### 1. Idle

User remains stationary with no interaction.

### 2. Exploring

User moves and scans environment without interaction.

### 3. Engaged

User looks at target and performs interaction.

### 4. Distracted

User looks away from target with no interaction.

## Evaluation Method

* Observed system logs
* Verified state transitions
* Validated UI response

## Outcome

All four states were correctly detected under controlled conditions.
