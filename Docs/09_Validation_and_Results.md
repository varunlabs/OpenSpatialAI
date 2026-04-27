# Validation and Results

## Overview

The system was evaluated using controlled test scenarios.

## Observations

* Engaged state triggered correctly with interaction + alignment
* Exploring state triggered during continuous movement
* Distracted state triggered when looking away (>40°)
* Idle state triggered after inactivity (~1.5 seconds)

## Stability

* No rapid state switching observed
* Temporal filtering ensured smooth transitions

## Performance

* Real-time execution within Unity update loop
* No noticeable latency during testing

## Conclusion

The prototype demonstrates that rule-based multimodal context inference can reliably detect user states in real time within a simulation environment.
