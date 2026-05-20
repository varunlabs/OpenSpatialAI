# GazePose-Context: Multimodal Context Inference for Adaptive XR Training in Unity and Meta Quest

Authors: Lavanya D., Varun Siddaraju, Dr. Alexandra Kitson (to confirm)  
Repository: `OpenSpatialAI`  
Branch: `research/gaze-pose-context-v0`  
Target format: IEEE VGTC / arXiv draft  
Primary category: `cs.HC`  
Secondary category: `cs.GR`

## Abstract

Adaptive extended reality (XR) systems need to respond to user attention, disengagement, and task flow without requiring explicit user input. This paper presents GazePose-Context, a Unity-based XR prototype that infers user context during a simple training task by combining gaze, head pose, body posture, hand interaction, and spatial context signals. The system organizes processing into three layers: sensor abstraction, context inference, and XR adaptation. Multimodal signals are synchronized into a shared `SignalFrame`, transformed into modality-specific feature vectors, fused through interpretable rules, and stabilized with a hysteresis-based state machine to estimate four context states: `Engaged`, `Distracted`, `Transitioning`, and `Idle`. These states drive lightweight adaptive behaviors such as subtle focus reinforcement, redirection prompts, next-step support, and rest-oriented controls. The current submission build targets Meta Quest deployment using Unity 6000.4.7f1, OpenXR 1.16.1, Meta XR Core SDK 201.0.0, URP 17.4.0, and OVRPlugin 1.201.0. This manuscript reports the implemented system architecture and evaluation protocol in full, and includes explicit placeholders for the final user-study results, verified literature citations, and statistical analysis that must be inserted before submission.

**Keywords:** XR, adaptive interfaces, multimodal interaction, gaze, body pose, hand tracking, context inference, Meta Quest

## 1. Introduction

Immersive systems are increasingly expected to understand what a user is doing and to react in ways that support attention, task completion, and comfort. In XR training environments, these expectations are especially strong because users often move between focus, transition, confusion, interruption, and rest without explicitly announcing those states. A system that can infer context from ongoing behavior can potentially guide the user without interrupting task flow or overloading the interface.

Most current XR experiences still rely on explicit UI actions, hard-coded task triggers, or isolated interaction signals. A single modality rarely captures the full structure of user behavior. Gaze alone cannot distinguish between task engagement and exploratory scanning. Hand interaction alone cannot represent attention drift. Body posture or locomotion alone can be ambiguous without knowing where the user is looking or whether they are interacting with task objects. This motivates a multimodal approach to context inference.

This paper presents **GazePose-Context**, an XR prototype developed in Unity for Meta Quest-class devices. The prototype combines gaze-related cues, head pose, posture and joint movement, hand pinch interactions, and spatial boundary context to infer one of four user states:

- `Engaged`
- `Distracted`
- `Transitioning`
- `Idle`

These states are then mapped to lightweight adaptive behaviors inside a simple training simulation. The task environment is intentionally constrained: the user sorts three virtual objects into matching receptacles. This simple task reduces gameplay complexity while making attention, object interaction, and task transitions easier to observe and interpret.

The paper's intended contribution is an empirical XR systems paper rather than a purely conceptual framework paper. Accordingly, the manuscript is organized around implementation and evaluation. The repository already supports the core system and logging pipeline. However, the repository alone does not yet provide the completed user-study results or the final verified literature bibliography. This draft therefore provides the full paper structure and implementation-grounded content, while marking the exact places where empirical evidence must be inserted before submission.

The contributions of this work are:

1. A three-layer XR architecture for multimodal context inference and adaptive support.
2. A synchronized runtime signal representation that combines gaze, head, body, hand, and spatial context data.
3. An interpretable rule-based context engine that estimates `Engaged`, `Distracted`, `Transitioning`, and `Idle` states with confidence scores.
4. A boundary-aware adaptation layer that changes feedback strategy based on user state and spatial setup.
5. A logging and evaluation design aligned with a within-subjects adaptive-vs-baseline user study.

## 2. Related Work

This section should be completed using the verified citations from Lavanya's earlier draft and DOI-checked sources before submission. The implementation plan requires approximately ten verified references and explicitly forbids unverified citations. The structure below reflects the required framing and can be populated directly from the earlier literature survey once that file is provided.

### 2.1 Adaptive XR and Context-Aware Interfaces

Prior work in adaptive XR and context-aware interfaces shows that immersive systems can benefit from runtime adaptation when they are sensitive to attention, workload, task state, or environmental constraints. Relevant literature here should position the current work against prior systems that adapt interface layout, task guidance, or feedback strategies in VR, AR, or MR settings.  
**Insert verified citations here: [RW1], [RW2], [RW3].**

### 2.2 Gaze, Head, and Attention Modeling

Gaze and head-motion signals are widely used as proxies for attention and cognitive state in immersive systems. However, gaze alone is often insufficient for robust behavioral interpretation, particularly when gaze must be disambiguated from transition behavior or environmental scanning. This subsection should discuss prior gaze-based attention tracking, fixation detection methods, and AOI-based gaze interpretation.  
**Insert verified citations here: [RW4], [RW5], [RW6].**

The current implementation uses an Identification by Velocity Threshold (I-VT) style fixation detector in the feature extraction layer, with a 30 degrees/second threshold and an 80 ms minimum fixation duration. These parameter choices should be linked to the canonical fixation literature, specifically the Salvucci and Goldberg formulation cited in the implementation guide.  
**Verified citation required: Salvucci & Goldberg (2000).**

### 2.3 Multimodal Behavioral Inference

Multimodal context inference combines complementary signals to reduce ambiguity that cannot be resolved by any single channel alone. In the present work, gaze, posture, hand activity, and spatial context jointly support state inference. This subsection should position the work against existing multimodal inference pipelines in XR, HCI, and embodied interaction contexts.  
**Insert verified citations here: [RW7], [RW8], [RW9].**

### 2.4 XR Training, Evaluation, and Workload

Because the paper targets an adaptive training task, it should also be situated relative to XR training studies and user-experience evaluation methods. The current evaluation plan uses NASA-TLX as a secondary outcome measure and a within-subjects comparison between adaptive and baseline conditions.  
**Verified citation required for NASA-TLX: Hart & Staveland (1988).**  
**Insert additional training-system citations here: [RW10], [RW11].**

### 2.5 Positioning of This Work

Relative to the literature above, GazePose-Context contributes an interpretable rule-based XR context engine embedded inside a working Unity/Meta Quest prototype. Its novelty lies in the combination of:

- multimodal state inference during a concrete task,
- explicit spatial-context modulation,
- real-time adaptive feedback,
- and a logging pipeline designed for empirical study.

This positioning statement should be finalized after the verified reference set is restored from the earlier draft.

## 3. Problem Statement and Research Questions

The core problem addressed in this paper is how to infer task-relevant user context in real time during immersive interaction and use that inference to adapt XR behavior without requiring manual status input from the user.

The primary research question is:

**RQ1.** Can multimodal XR signals be fused in real time to infer task-relevant user context during a simple immersive training activity?

The secondary research question is:

**RQ2.** Does context-aware adaptive support improve task performance and subjective experience relative to a non-adaptive baseline?

These questions are operationalized through a sorting task in which the user places three objects into corresponding receptacles. The task provides a controlled environment in which gaze targets, hand interactions, posture changes, and task transitions can be observed with relatively low confound from game complexity.

## 4. System Architecture

The implemented prototype follows a three-layer architecture:

1. **Sensor Abstraction Layer**
2. **Context Engine**
3. **XR App Shell**

This architecture mirrors the planning documents and is also reflected in the current repository structure.

### 4.1 Layer 1: Sensor Abstraction

The sensor abstraction layer collects raw runtime signals and writes them into a synchronized data structure. The core contract between Layer 1 and Layer 2 is the `SignalFrame` struct. Each frame contains timestamped gaze, head, body, hand, spatial, and derived context fields.

The current `SignalFrame` includes:

- `timestamp_ms`
- `gaze_origin`
- `gaze_direction`
- `is_fixation`
- `fixation_duration_s`
- `aoi_hit`
- `head_position`
- `head_forward`
- `head_gaze_divergence_deg`
- `posture_class`
- `spine_angle_deg`
- `avg_joint_velocity`
- `left_pinch`
- `right_pinch`
- `interaction_count_10s`
- `nearest_object_dist_m`
- `posture_mode`
- `boundary_type`
- `locomotion_delta_m`
- `context_state`
- `context_confidence`
- `prev_context_state`
- `state_hold_duration_ms`
- `state_transition_count`
- `nearest_interactable`

The synchronizer emits a shared frame every 100 ms using `InvokeRepeating`, yielding a 10 Hz context-processing cadence in the current repository implementation.

### 4.2 Layer 2: Context Engine

The context engine transforms each synchronized frame into higher-level multimodal features, estimates a candidate context state through rule-based fusion, and stabilizes the result through a temporal state machine. This layer contains the main research logic of the prototype.

### 4.3 Layer 3: XR App Shell

The XR application layer consumes the current state and boundary context and selects the appropriate adaptive behavior. This layer is intentionally separated from the context engine so that inference logic remains independent of Unity-specific UI and scene objects.

## 5. Methodology

### 5.1 Task Environment

The application scene is a short sorting simulation implemented as `TrainingSimulation`. The user is presented with three task objects, a cube, cylinder, and sphere, which must be placed in the correct receptacles. Task objects and receptacles are tagged as Areas of Interest (AOIs), enabling the gaze subsystem to identify when task-relevant objects are being inspected. The scene is intentionally simple because the goal is not to study game mechanics but to isolate context inference during focused interaction.

### 5.2 Signal Acquisition

#### 5.2.1 Gaze and AOI Detection

The current repository implementation resolves a gaze ray using the center-eye camera or center-eye anchor and casts the ray into the scene against AOI colliders. This produces:

- gaze origin,
- normalized gaze direction,
- AOI hit name,
- a placeholder fixation estimate in Layer 1,
- and a rendered debug ray for QA.

The code comments state that this update path runs in Unity `Update()` to follow headset refresh behavior at approximately 60-90 Hz. The implementation guide expects eventual OpenXR eye-gaze integration; however, the current repository build should be described precisely as a center-eye gaze-ray implementation with downstream interfaces designed for hardware-compatible gaze integration.

#### 5.2.2 Head and Body Signals

Head pose is captured from the main camera or OVR camera rig. Body pose capture uses `OVRSkeleton` joints and computes:

- head position,
- head forward vector,
- head-gaze divergence,
- a coarse posture class,
- spine angle,
- and average joint velocity.

The current Layer 1 posture labels are `standing`, `leaning`, `reaching`, and `unknown`, produced from spine-angle heuristics. These are later reinterpreted into the Layer 2 posture labels used by the fusion engine.

#### 5.2.3 Hand Interaction Signals

Hand capture reads `OVRHand` input and controller fallback input. Pinch events are detected using a pinch-strength threshold of `0.7`, and interaction counts are accumulated over a ten-second sliding window. The hand subsystem also computes the distance between the dominant tracked hand and the nearest AOI-tagged object.

#### 5.2.4 Spatial Context Signals

Spatial context detection updates every five seconds and estimates:

- `boundary_type`: `stationary`, `room_scale`, `custom`, or `passthrough`
- `posture_mode`: `sitting`, `standing`, or `room_scale`

Boundary type is inferred from the configured guardian geometry area. Posture mode is inferred from headset height relative to a session calibration point, with a sitting threshold difference of `0.25 m`.

### 5.3 Signal Synchronization

The `SignalSynchroniser` collects the latest values from all capture scripts and writes them into a single `SignalFrame` every 100 ms. Missing values fall back to the most recent valid values or safe defaults. The current implementation does not yet expose an explicit per-signal staleness flag, which was requested in the implementation guide; this gap should be acknowledged as a limitation.

### 5.4 Feature Extraction

Feature extraction occurs in Layer 2 and converts raw frame values into inference-oriented features.

#### 5.4.1 Gaze Features

`GazeFeatureExtractor` computes:

- `currently_on_task_aoi`
- `fixation_on_aoi`
- `fixation_duration_s`
- `head_gaze_divergence_deg`
- `aoi_dwell_ratio`
- `saccade_rate_per_s`

The current implementation uses:

- I-VT velocity threshold: `30 deg/s`
- minimum fixation duration: `80 ms`
- saccade window: `2000 ms`
- AOI dwell window: `5000 ms`

Task-relevant AOIs are identified by names beginning with `task_` or `receptacle_`, while `table_surface` and `instruction_panel` are explicitly excluded.

#### 5.4.2 Body Features

`BodyFeatureExtractor` maps raw posture and spine-angle values into:

- `Upright`
- `LeaningForward`
- `Slouched`
- `LeaningBack`

using the current thresholds:

- `< 10 deg`: `Upright`
- `< 30 deg`: `LeaningForward`
- `<= 60 deg`: `Slouched`
- `> 60 deg`: `LeaningBack`

It also passes forward average joint velocity.

#### 5.4.3 Hand Features

`HandFeatureExtractor` computes:

- interaction frequency = `interaction_count_10s / 10`
- object proximity = `nearest_object_dist_m < 0.3`
- pinch activity = `left_pinch OR right_pinch`

#### 5.4.4 Spatial Features

`SpatialContextExtractor` computes:

- posture mode,
- boundary type,
- locomotion activity.

Locomotion is discretized from inter-frame head movement using the thresholds:

- `< 0.1 m`: `None`
- `< 0.5 m`: `Low`
- `>= 0.5 m`: `Active`

### 5.5 Rule-Based Fusion

The current `ContextFusionEngine` applies prioritized rules to infer a candidate state.

#### 5.5.1 Engaged

The `Engaged` rule is supported by combinations of:

- task AOI focus,
- fixation on task AOI,
- active interaction,
- supportive posture,
- and room-scale engagement cues.

The rule also requires:

- head-gaze divergence `<= 18 deg`
- saccade rate `<= 2.5 / s`

#### 5.5.2 Distracted

The `Distracted` rule requires:

- gaze not on task AOI,
- no fixation on task AOI,
- AOI dwell ratio `< 0.20`,
- away-signal from divergence, saccade activity, or body movement,
- no active pinch,
- and low interaction frequency.

#### 5.5.3 Idle

The `Idle` rule requires:

- low body movement,
- no effective hand activity,
- no object proximity,
- no task-directed gaze,
- and very low AOI dwell.

#### 5.5.4 Transitioning

The `Transitioning` rule captures:

- high head-gaze divergence,
- unstable but task-adjacent gaze,
- short fixation durations,
- moderate AOI dwell,
- or preparatory hand proximity.

The first satisfied rule determines the candidate state.

### 5.6 Confidence Scoring

Each inferred state receives a confidence score initialized from `0.5` and incremented based on supporting multimodal evidence. Confidence is clamped to the range `[0,1]` and carried into the runtime snapshot, debugging overlay, and persistent logs.

### 5.7 Temporal Stabilization

The `ContextStateMachine` reduces oscillation through:

- minimum state hold time: `0.5 s`
- pending confirmation time: `0.2 s`

This means that a new state must persist before replacing the current state, which reduces rapid flicker in the adaptive interface.

## 6. Implementation

### 6.1 Software Stack

The repository currently documents the following verified submission versions:

- Unity Editor: `6000.4.7f1`
- OpenXR Plugin: `1.16.1`
- Meta XR Core SDK: `201.0.0`
- Universal Render Pipeline: `17.4.0`
- OVRPlugin: `1.201.0`

These values should be retained in the final paper's implementation section for the Unity 6 submission build.

### 6.2 Runtime Integration

The `XRAppShellController` binds the context source to the application layer, publishes `XRContextSnapshot` updates, aligns the training simulation in front of the user, and provides fallback simulation behavior for QA. If the live context source cannot be found and fallback is enabled, the system can publish simulated states for manual testing.

### 6.3 Task Logic

The task layer uses `ReceptacleTrigger`, `SortableTaskObject`, and `TrainingSimulationTaskManager`. Correct placements are counted, and the task completes when all three objects are sorted.

### 6.4 Adaptive Behaviors

`AdaptationManager` maps current state and boundary type into adaptive behavior variants:

- `Engaged`: subtle vignette, reduced unnecessary hints
- `Distracted`: visual nudge, spatial audio cue, or minimal overlay depending on boundary mode
- `Transitioning`: next-task preload or content repositioning
- `Idle`: rest panel, continue/break/recenter controls, optional floor arrow

The manager also preserves state visibility durations to avoid overly frequent UI changes.

### 6.5 Logging

`ContextLogger` writes each processed frame to a timestamped `.jsonl` file under `Application.persistentDataPath`. Logged entries include:

- full signal frame payload,
- context state,
- confidence,
- previous state,
- hold duration,
- state transition count,
- nearest interactable.

This log structure supports later offline analysis and future intent-modeling extensions.

## 7. Evaluation Design

This section reflects the required empirical paper design from the implementation guide and WBS. Replace placeholders with the real study details before submission.

### 7.1 Study Design

The planned study is a **within-subjects comparison** with two conditions:

- **Baseline condition**: system active, adaptive behaviors disabled
- **Adaptive condition**: full multimodal inference and adaptive behaviors enabled

Condition order should be counterbalanced across participants.

### 7.2 Participants

Target participants: `8-10`  
Minimum usable sample for inferential analysis: `6`

**To insert before submission:**

- final participant count,
- recruitment source,
- age range,
- prior XR experience,
- exclusion criteria.

### 7.3 Ethics

This paper requires a confirmed statement on ethics / HREB approval status before submission. Insert:

- institution,
- approval identifier if granted,
- consent statement,
- anonymization workflow.

### 7.4 Procedure

Each participant session is planned as follows:

1. Consent and briefing
2. Familiarization with headset and sorting task
3. Condition A or B (8-12 minutes)
4. NASA-TLX and self-report ratings
5. Five-minute break
6. Remaining condition
7. NASA-TLX and self-report ratings
8. Debrief

### 7.5 Measures

Primary measure:

- context detection accuracy against participant self-report or ground-truth labeling

Secondary measures:

- task completion time
- number of sorting errors
- NASA-TLX workload score
- subjective engagement and distraction ratings

### 7.6 Statistical Analysis

The implementation guide specifies:

- descriptive summaries for all measures
- confusion matrix for state classification
- Wilcoxon signed-rank test for Adaptive vs Baseline comparisons

These analyses should only be reported once real data is available.

## 8. Results

This section is intentionally structured for direct completion once study data is available. Do not submit the paper with placeholder text left unresolved.

### 8.1 Participant Summary

**Placeholder:** Insert demographics and prior XR familiarity.

| Variable | Value |
|---|---|
| Participants | `TODO` |
| Mean age | `TODO` |
| XR experience distribution | `TODO` |
| Ethics approval ID | `TODO` |

### 8.2 Context Detection Accuracy

**Placeholder paragraph:**  
Insert overall state classification accuracy, per-state accuracy, and a brief interpretation of where misclassifications occurred.

| Metric | Baseline | Adaptive | Notes |
|---|---|---|---|
| Overall context accuracy | `TODO` | `TODO` | `TODO` |
| Engaged accuracy | `TODO` | `TODO` | `TODO` |
| Distracted accuracy | `TODO` | `TODO` | `TODO` |
| Transitioning accuracy | `TODO` | `TODO` | `TODO` |
| Idle accuracy | `TODO` | `TODO` | `TODO` |

### 8.3 Task Performance

**Placeholder paragraph:**  
Insert completion-time comparison and error counts.

| Metric | Baseline | Adaptive | Test statistic | p-value |
|---|---|---|---|---|
| Completion time | `TODO` | `TODO` | `TODO` | `TODO` |
| Sorting errors | `TODO` | `TODO` | `TODO` | `TODO` |

### 8.4 Workload and Subjective Ratings

**Placeholder paragraph:**  
Insert NASA-TLX and self-report findings.

| Metric | Baseline | Adaptive | Test statistic | p-value |
|---|---|---|---|---|
| NASA-TLX | `TODO` | `TODO` | `TODO` | `TODO` |
| Self-reported engagement | `TODO` | `TODO` | `TODO` | `TODO` |
| Self-reported distraction | `TODO` | `TODO` | `TODO` | `TODO` |

### 8.5 Figures to Insert

Insert the following figures once analysis is complete:

1. Architecture diagram
2. Training scene screenshot
3. Confusion matrix
4. Completion-time comparison chart
5. NASA-TLX comparison chart

## 9. Discussion

The intended interpretation of the system is that multimodal sensing provides a more meaningful representation of user context than isolated XR signals. The design of the prototype supports this claim at the system-architecture level because it combines attention, posture, interaction, and spatial setup before selecting adaptive support. The adaptive layer is deliberately lightweight: the goal is not to interrupt the user but to provide support that matches inferred state.

From an HCI perspective, the most important design principle in the current prototype is not the specific threshold values but the explicit separation between sensing, inference, and application behavior. This separation improves inspectability, modularity, and future extensibility. It also creates a path toward a plugin-style architecture in which the context engine can later be replaced by a learned model without rewriting the XR application layer.

If the planned study confirms improved task continuity or lower workload in the adaptive condition, the system would support the broader argument that XR interfaces can become more responsive by reacting to inferred context rather than relying exclusively on manual UI interactions. If the study instead shows weak or mixed benefits, that result would still be valuable because it would identify which states are robust enough for adaptation and which remain too ambiguous for dependable runtime intervention.

## 10. Limitations

Several limitations should be stated explicitly in the final paper.

First, the current repository implementation is still a proof-of-concept. It is not yet a production-grade general context engine. Second, the current task is deliberately simple, so ecological validity for richer training workflows remains limited. Third, the fusion model is manually authored and threshold-driven, which improves interpretability but may reduce generalization. Fourth, the current repository does not yet include explicit per-signal staleness flags, even though the implementation guide proposed them. Fifth, the current gaze implementation in the repository is a center-eye gaze-ray approximation rather than a clearly documented hardware eye-tracking pipeline; the paper should not overclaim beyond the implemented code path. Finally, no empirical results should be claimed until the study data and verified statistical analysis are inserted.

## 11. Future Work

The planning documents already identify several extensions that should be preserved in the final manuscript:

1. locomotion-aware context refinement,
2. face-tracking integration,
3. EEG or physiological augmentation,
4. multi-user or shared-space support,
5. personalization of thresholds and adaptation policies.

An important near-term next step is the creation of a labeled session dataset from the current logging pipeline. That dataset would support direct comparison between the current rule-based engine and future learned classifiers.

## 12. Conclusion

GazePose-Context presents a structured XR prototype for multimodal context inference and adaptive training support. The system combines gaze, head, body, hand, and spatial context signals inside a three-layer Unity architecture and maps the resulting inferred states to lightweight, boundary-aware adaptive behavior. The repository already supports synchronized signal capture, feature extraction, rule-based fusion, state stabilization, runtime adaptation, and persistent logging. The remaining steps to reach a submission-ready empirical paper are the insertion of verified related-work citations, ethics and participant details, and the completed user-study results.

## Acknowledgments

The final paper should include the AI-disclosure language required by the planning document. A draft acknowledgement is provided below and should be edited only if the actual tool usage changes:

> This research used AI-assisted tools in the following capacities: Claude for paper drafting assistance and research synthesis; GitHub Copilot for Unity C# code scaffolding, with all generated code tested and validated on hardware by the researchers; literature-discovery tools for reference discovery; transcription tools for interview transcription if applicable; and charting tools for results figure generation. All scientific claims, research contribution statements, experimental results, and final citations were produced and verified by the human authors.

## Submission Checklist

Before submission, confirm the following:

- all related-work citations are restored from the verified draft and DOI-checked,
- the abstract is rewritten to describe actual results rather than placeholders,
- ethics status is explicitly stated,
- participant count is inserted,
- all tables contain real values,
- all figures are added and referenced in the text,
- statistical analysis is inserted,
- title, author list, and affiliations are finalized,
- and the manuscript is transferred into the IEEE VGTC or arXiv LaTeX template.
