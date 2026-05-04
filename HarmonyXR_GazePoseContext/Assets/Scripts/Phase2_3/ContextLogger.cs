using System;
using System.IO;
using UnityEngine;

public class ContextLogger
{
    [Serializable]
    private struct ContextLogEntry
    {
        public long timestamp_ms;
        public string context_state;
        public float confidence;
        public string prev_context_state;
        public int state_hold_duration_ms;
        public float state_transition_count;
        public string nearest_interactable;
    }

    private readonly string logPath;

    private ContextState previousState;
    private bool hasPreviousState;
    private long currentStateStartTimestampMs;
    private float transitionCount;

    public ContextLogger()
    {
        logPath = Application.persistentDataPath + "/context_log.json";
    }

    public void Log(SignalFrame frame, ContextResult result)
    {
        long timestamp = frame.timestamp_ms;
        string currentState = result.state.ToString();

        if (!hasPreviousState)
        {
            previousState = result.state;
            hasPreviousState = true;
            currentStateStartTimestampMs = timestamp;
        }

        string prevStateString = previousState.ToString();

        if (result.state != previousState)
        {
            transitionCount += 1f;
            prevStateString = previousState.ToString();
            previousState = result.state;
            currentStateStartTimestampMs = timestamp;
        }

        int holdDurationMs = 0;
        long holdDelta = timestamp - currentStateStartTimestampMs;
        if (holdDelta > 0L)
        {
            holdDurationMs = holdDelta > int.MaxValue ? int.MaxValue : (int)holdDelta;
        }

        ContextLogEntry entry = new ContextLogEntry
        {
            timestamp_ms = timestamp,
            context_state = currentState,
            confidence = result.confidence,
            prev_context_state = prevStateString,
            state_hold_duration_ms = holdDurationMs,
            state_transition_count = transitionCount,
            nearest_interactable = string.IsNullOrWhiteSpace(frame.aoi_hit) ? "none" : frame.aoi_hit
        };

        string line = JsonUtility.ToJson(entry) + Environment.NewLine;
        File.AppendAllText(logPath, line);
    }
}
