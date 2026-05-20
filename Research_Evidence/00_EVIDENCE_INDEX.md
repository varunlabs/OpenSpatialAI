# Evidence Index

This folder contains validation evidence for the HarmonyXR GazePose-Context prototype. Evidence is already organized by validation purpose, so raw files were not moved.

## Evidence Owner / Scope

- Project: HarmonyXR GazePose-Context
- Evidence set: Quest headset validation run
- Primary date: 2026-05-14
- Current documentation update: 2026-05-20
- Use: paper support, report support, demo validation, and final review.

## Folder Purpose

- `01_Build_Proof`: build, launch, and runtime proof notes.
- `02_Live_Sensor_Validation`: gaze, head, hand, body, and spatial tracking validation notes.
- `03_Context_State_Coverage`: primary headset evidence video and state coverage proof.
- `04_Adaptation_Behavior`: adaptive behavior evidence and notes.
- `05_Stability_Latency`: console evidence, filtered logs, and runtime stability notes.
- `06_Final_Demo`: final demo references.
- `07_Processed_Tables_Charts`: processed CSV tables used for paper/report evidence.
- `08_Notes_Limitations`: limitations, interpretation notes, and reviewer caveats.

## Primary Evidence Files

- Full headset video: `03_Context_State_Coverage/20260514_run01_full-evidence.mp4`
- Full console evidence: `05_Stability_Latency/20260514_run01_console-evidence.txt`
- Filtered evidence: `05_Stability_Latency/20260514_run01_filtered-evidence.txt`
- QA metrics table: `07_Processed_Tables_Charts/qa_metrics_extract.csv`
- State coverage table: `07_Processed_Tables_Charts/state_coverage_table.csv`
- Adaptation event table: `07_Processed_Tables_Charts/adaptation_event_table.csv`
- Stability/latency table: `07_Processed_Tables_Charts/stability_latency_table.csv`

## What To Use For Final Submission

Use these first:

```text
README.md
EVIDENCE_SUMMARY_20260514.md
00_EVIDENCE_INDEX.md
03_Context_State_Coverage/
05_Stability_Latency/
07_Processed_Tables_Charts/
```

Use the remaining folders for supporting notes and limitations.

## Rule For Future Evidence

Use this naming pattern:

```text
YYYYMMDD_run##_short-description.ext
```

Keep raw logs/videos in their validation-purpose folder. Put processed tables in `07_Processed_Tables_Charts`.

