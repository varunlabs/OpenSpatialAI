// Phase 2 - Gaze Feature Extraction
using System;
using System.Collections.Generic;
using UnityEngine;

public struct GazeFeatureVector
{
    public bool fixation_on_aoi;
    public float fixation_duration_s;
    public float head_gaze_divergence_deg;
    public float aoi_dwell_ratio;
    public float saccade_rate_per_s;
}

public sealed class GazeFeatureExtractor
{
    private const float VelocityThresholdDegPerSec = 30f;
    private const long FixationMinDurationMs = 80L;
    private const long SaccadeWindowMs = 2000L;
    private const long AoiDwellWindowMs = 5000L;

    private struct AoiSample
    {
        public long timestampMs;
        public bool onAoi;
    }

    private bool hasPreviousSample;
    private Vector3 previousGazeDirection;
    private long previousTimestampMs;
    private bool wasAboveVelocityThreshold;

    private bool fixationCandidateActive;
    private long fixationCandidateStartMs;

    private readonly Queue<long> saccadeEventTimestampsMs = new Queue<long>();
    private readonly Queue<AoiSample> aoiHistory = new Queue<AoiSample>();

    public GazeFeatureVector ExtractFeatures(SignalFrame frame)
    {
        long currentTimestampMs = frame.timestamp_ms;
        bool isOnAoi = !string.Equals(frame.aoi_hit, "none", StringComparison.OrdinalIgnoreCase);

        Vector3 currentGazeDirection = frame.gaze_direction;
        bool hasValidCurrentDirection = currentGazeDirection.sqrMagnitude > 0f;
        if (hasValidCurrentDirection)
        {
            currentGazeDirection.Normalize();
        }

        float gazeVelocityDegPerSec = 0f;
        if (hasPreviousSample && hasValidCurrentDirection)
        {
            long deltaMs = currentTimestampMs - previousTimestampMs;
            if (deltaMs >= 1)
            {
                float angularDifferenceDeg = Vector3.Angle(previousGazeDirection, currentGazeDirection);
                gazeVelocityDegPerSec = angularDifferenceDeg / (deltaMs / 1000f);
            }
        }

        // Implements I-VT fixation detection (Salvucci & Goldberg, 2000)
        // Velocity threshold = 30 deg/sec, fixation >= 80ms
        bool isBelowVelocityThreshold = hasValidCurrentDirection && gazeVelocityDegPerSec < VelocityThresholdDegPerSec;
        bool computedFixation = false;
        float computedFixationDurationS = 0f;

        if (isBelowVelocityThreshold)
        {
            if (!fixationCandidateActive)
            {
                fixationCandidateActive = true;
                fixationCandidateStartMs = currentTimestampMs;
            }

            long fixationCandidateDurationMs = currentTimestampMs - fixationCandidateStartMs;
            computedFixationDurationS = fixationCandidateDurationMs / 1000f;
            computedFixation = fixationCandidateDurationMs >= FixationMinDurationMs;
        }
        else
        {
            fixationCandidateActive = false;
            fixationCandidateStartMs = 0L;
            computedFixationDurationS = 0f;
            computedFixation = false;
        }

        bool isSaccadeNow = hasValidCurrentDirection && gazeVelocityDegPerSec > VelocityThresholdDegPerSec;
        if (isSaccadeNow && !wasAboveVelocityThreshold)
        {
            saccadeEventTimestampsMs.Enqueue(currentTimestampMs);
        }
        wasAboveVelocityThreshold = isSaccadeNow;

        long saccadeWindowStartMs = currentTimestampMs - SaccadeWindowMs;
        while (saccadeEventTimestampsMs.Count > 0 && saccadeEventTimestampsMs.Peek() < saccadeWindowStartMs)
        {
            saccadeEventTimestampsMs.Dequeue();
        }

        float saccadeRatePerS = 0f;
        if (saccadeEventTimestampsMs.Count > 0)
        {
            long oldestSaccadeMs = saccadeEventTimestampsMs.Peek();
            long effectiveWindowStartMs = Math.Max(saccadeWindowStartMs, oldestSaccadeMs);
            float windowDurationSeconds = Mathf.Max((currentTimestampMs - effectiveWindowStartMs) / 1000f, 0.001f);
            saccadeRatePerS = saccadeEventTimestampsMs.Count / windowDurationSeconds;
        }

        aoiHistory.Enqueue(new AoiSample
        {
            timestampMs = currentTimestampMs,
            onAoi = isOnAoi
        });

        long aoiWindowStartMs = currentTimestampMs - AoiDwellWindowMs;
        while (aoiHistory.Count > 0 && aoiHistory.Peek().timestampMs < aoiWindowStartMs)
        {
            aoiHistory.Dequeue();
        }

        float onAoiDurationMs = 0f;
        float totalDurationMs = 0f;

        AoiSample? previousSample = null;
        foreach (AoiSample sample in aoiHistory)
        {
            if (previousSample.HasValue)
            {
                AoiSample from = previousSample.Value;
                long segmentStartMs = Math.Max(from.timestampMs, aoiWindowStartMs);
                long segmentEndMs = sample.timestampMs;
                long segmentDurationMs = Math.Max(0L, segmentEndMs - segmentStartMs);

                totalDurationMs += segmentDurationMs;
                if (from.onAoi)
                {
                    onAoiDurationMs += segmentDurationMs;
                }
            }

            previousSample = sample;
        }

        if (previousSample.HasValue)
        {
            AoiSample last = previousSample.Value;
            long segmentStartMs = Math.Max(last.timestampMs, aoiWindowStartMs);
            long segmentEndMs = currentTimestampMs;
            long segmentDurationMs = Math.Max(0L, segmentEndMs - segmentStartMs);

            totalDurationMs += segmentDurationMs;
            if (last.onAoi)
            {
                onAoiDurationMs += segmentDurationMs;
            }
        }

        float aoiDwellRatio = totalDurationMs > 0f ? onAoiDurationMs / totalDurationMs : 0f;

        if (hasValidCurrentDirection)
        {
            previousGazeDirection = currentGazeDirection;
            previousTimestampMs = currentTimestampMs;
            hasPreviousSample = true;
        }

        return new GazeFeatureVector
        {
            fixation_on_aoi = computedFixation && isOnAoi,
            fixation_duration_s = computedFixationDurationS,
            head_gaze_divergence_deg = frame.head_gaze_divergence_deg,
            aoi_dwell_ratio = aoiDwellRatio,
            saccade_rate_per_s = saccadeRatePerS
        };
    }
}
