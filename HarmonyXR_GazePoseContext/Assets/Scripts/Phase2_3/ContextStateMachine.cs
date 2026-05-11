using UnityEngine;

public class ContextStateMachine
{
    private ContextState currentState = ContextState.Idle;
    private float stateEnterTime = 0f;
    private bool hasInitialized;

    private ContextState pendingState = ContextState.Idle;
    private float pendingStartTime = 0f;

    public ContextResult Update(ContextResult newResult)
    {
        float now = Time.time;
        if (!hasInitialized)
        {
            hasInitialized = true;
            currentState = newResult.state;
            pendingState = newResult.state;
            stateEnterTime = now;
            pendingStartTime = now;
            return BuildResult(currentState, newResult.confidence);
        }

        if (newResult.state == currentState)
        {
            pendingState = currentState;
            pendingStartTime = 0f;
            return BuildResult(currentState, newResult.confidence);
        }

        if (pendingState != newResult.state)
        {
            pendingState = newResult.state;
            pendingStartTime = now;
        }

        if (now - stateEnterTime < GetMinimumStateHold(currentState))
        {
            return BuildResult(currentState, newResult.confidence);
        }

        if (now - pendingStartTime >= GetPendingConfirmationTime(currentState, pendingState))
        {
            currentState = pendingState;
            stateEnterTime = now;
        }

        return BuildResult(currentState, newResult.confidence);
    }

    private static ContextResult BuildResult(ContextState state, float confidence)
    {
        return new ContextResult
        {
            state = state,
            confidence = confidence
        };
    }

    private static float GetMinimumStateHold(ContextState state)
    {
        switch (state)
        {
            case ContextState.Engaged:
                return 1.0f;
            case ContextState.Transitioning:
                return 1.25f;
            case ContextState.Distracted:
                return 1.75f;
            case ContextState.Idle:
            default:
                return 2.0f;
        }
    }

    private static float GetPendingConfirmationTime(ContextState fromState, ContextState candidateState)
    {
        switch (candidateState)
        {
            case ContextState.Engaged:
                return 0.45f;
            case ContextState.Transitioning:
                return fromState == ContextState.Engaged ? 0.4f : 0.6f;
            case ContextState.Distracted:
                return 0.75f;
            case ContextState.Idle:
            default:
                return 1.1f;
        }
    }
}
