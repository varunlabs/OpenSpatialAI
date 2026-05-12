using UnityEngine;

public class ContextStateMachine
{
    private const float MinimumStateHoldSeconds = 0.5f;
    private const float PendingConfirmationSeconds = 0.2f;

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
        return MinimumStateHoldSeconds;
    }

    private static float GetPendingConfirmationTime(ContextState fromState, ContextState candidateState)
    {
        return PendingConfirmationSeconds;
    }
}
