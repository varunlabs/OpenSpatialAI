# Evidence Run Notes Template

Run name: Live sensor validation evidence
Date/time: May 14, 2026
Tester:
Video file: ../03_Context_State_Coverage/20260514_run01_full-evidence.mp4
Log file: ../05_Stability_Latency/20260514_run01_console-evidence.txt
Processed table: ../07_Processed_Tables_Charts/qa_metrics_extract.csv

## Purpose
Document that the prototype captured live XR runtime signals during headset execution.

## Actions Performed
- Ran the proof-of-concept on Quest headset.
- Captured Unity runtime console logs using ADB logcat.
- Performed gaze, hand, pinch, task, and movement interactions during the sorting workflow.
- Generated QA_METRICS runtime evidence.

## Expected Evidence
- AOI / gaze target values.
- Boundary or spatial context values.
- Posture or spatial posture context values.
- Fixation and dwell values.
- Saccade/attention-shift values.
- Body activity values.
- Hand interaction count.
- Pinch state.
- Distance to task object.

## Observed Result
The QA_METRICS output captured all required runtime signal fields: AOI, POSTURE, BOUND, FIX, DWELL, SACC, BODY, HAND, PINCH, and DIST.

## Important Timestamps
| Time | Event | Evidence File |
|---|---|---|
| 16:16:46 | QA_METRICS started with AOI and state output | ../05_Stability_Latency/20260514_run01_console-evidence.txt |
| 16:19:53 | task_cube_1 AOI observed | ../05_Stability_Latency/20260514_run01_console-evidence.txt |
| 16:20:12 | task_cylinder_1 AOI observed | ../05_Stability_Latency/20260514_run01_console-evidence.txt |
| 16:20:15 | task_sphere_1 AOI observed | ../05_Stability_Latency/20260514_run01_console-evidence.txt |

## Issues / Limitations
- POSTURE=room_scale should be interpreted as spatial/boundary posture context, not strict sitting/standing classification.
- Sitting evidence appears in earlier lines, but sitting/standing should not be overclaimed without dedicated posture validation.

## Result
- [x] Passed
- [ ] Needs rerun


