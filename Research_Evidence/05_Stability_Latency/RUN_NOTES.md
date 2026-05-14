# Evidence Run Notes Template

Run name: Full headset QA and console evidence run
Date/time: May 14, 2026
Tester:
Video file: ../03_Context_State_Coverage/20260514_run01_full-evidence.mp4
Log file: 20260514_run01_console-evidence.txt
Filtered log file: 20260514_run01_filtered-evidence.txt

## Purpose
Capture runtime console evidence for the HarmonyXR adaptive XR proof-of-concept, including context states, QA metrics, sensor values, and adaptive runtime behavior indicators.

## Actions Performed
- Launched the XR proof-of-concept on Quest headset.
- Captured Unity runtime logs through ADB logcat.
- Completed the guided sorting task flow.
- Triggered and captured all four context states.
- Captured QA_METRICS runtime signal output.

## Expected Evidence
- Engaged state evidence.
- Distracted state evidence.
- Transitioning state evidence.
- Idle state evidence.
- AOI, posture/spatial context, boundary, hand, pinch, gaze timing, body activity, and distance values.
- Phase 3 runtime/adaptive system messages.

## Observed Result
The run captured all four context states and complete QA_METRICS runtime values. The filtered evidence file confirms Engaged, Distracted, Transitioning, and Idle state coverage.

## Important Timestamps
| Time | Event | Evidence File |
|---|---|---|
| 16:16:46 | Distracted state observed | 20260514_run01_filtered-evidence.txt |
| 16:16:49 | Idle state observed | 20260514_run01_filtered-evidence.txt |
| 16:19:18 | Engaged state observed | 20260514_run01_filtered-evidence.txt |
| 16:19:28 | Transitioning state observed | 20260514_run01_filtered-evidence.txt |

## Issues / Limitations
- Full headset evidence video is stored at ../03_Context_State_Coverage/20260514_run01_full-evidence.mp4.
- room_scale posture values should be interpreted as spatial/boundary context, not strict sitting/standing classification.

## Result
- [x] Passed
- [ ] Needs rerun




