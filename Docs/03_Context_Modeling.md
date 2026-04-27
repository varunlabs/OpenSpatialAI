# Context Modeling

## State Definitions

### Engaged

User is interacting and focused on the target.

### Exploring

User is moving and scanning the environment.

### Distracted

User is looking away from the task.

### Idle

User is inactive with minimal movement.

## Rule Logic

* Engaged → Interaction + aligned head direction
* Exploring → Movement + no interaction
* Distracted → Large head deviation (>40°)
* Idle → No movement + no interaction

## Stability Mechanisms

* Minimum hold time: 500 ms
* State confirmation: 200 ms
* Priority order:
  Engaged → Exploring → Distracted → Idle
