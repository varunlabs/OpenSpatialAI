# HarmonyXR Evidence Summary - May 14, 2026

## Evidence Status
The headset evidence run produced usable console proof for the adaptive XR research prototype. The captured runtime log includes QA_METRICS lines, all four context states, confidence values, AOI values, posture/spatial context, hand interaction, pinch state, gaze timing features, body activity, and object distance.

## Primary Evidence Files
- Full console evidence: 05_Stability_Latency/20260514_run01_console-evidence.txt
- Filtered evidence: 05_Stability_Latency/20260514_run01_filtered-evidence.txt
- QA metrics table: 07_Processed_Tables_Charts/qa_metrics_extract.csv
- State coverage table: 07_Processed_Tables_Charts/state_coverage_table.csv
- Adaptation event table: 07_Processed_Tables_Charts/adaptation_event_table.csv
- Stability table: 07_Processed_Tables_Charts/stability_latency_table.csv

## Context State Coverage
- Engaged: Yes, count 15, first 05-14 16:19:18.956, last 05-14 16:21:33.401
- Distracted: Yes, count 30, first 05-14 16:16:46.104, last 05-14 16:21:55.453
- Transitioning: Yes, count 100, first 05-14 16:19:28.044, last 05-14 16:21:48.434
- Idle: Yes, count 24, first 05-14 16:16:49.126, last 05-14 16:22:02.470

## Runtime Signal Coverage
- AOI: Present in QA_METRICS
- POSTURE: Present in QA_METRICS
- BOUND: Present in QA_METRICS
- HAND: Present in QA_METRICS
- PINCH: Present in QA_METRICS
- FIX: Present in QA_METRICS
- DWELL: Present in QA_METRICS
- SACC: Present in QA_METRICS
- BODY: Present in QA_METRICS
- DIST: Present in QA_METRICS

## Paper-Ready Interpretation
This evidence supports the claim that the prototype captured multimodal XR runtime signals and used them to infer Engaged, Distracted, Transitioning, and Idle context states during headset execution. The same run also captured confidence values, AOI changes, hand/pinch interaction evidence, boundary/spatial context, and gaze-derived timing metrics.

## Video Evidence
The full headset evidence video is stored at:
03_Context_State_Coverage/20260514_run01_full-evidence.mp4

No separate video is required for the current evidence package.

## Notes For Paper
- Treat room_scale posture values as spatial/boundary context, not strict sitting/standing classification.
- Use POSTURE=sitting or POSTURE=Sitting lines only when discussing sitting/standing evidence.
- Use filtered evidence for tables and raw console evidence as backup.



