using UnityEngine;

public class ContextStateMachine
{
    private ContextState currentState = ContextState.Idle;
    private float stateEnterTime = 0f;

    private ContextState pendingState = ContextState.Idle;
    private float pendingStartTime = 0f;

    public ContextResult Update(ContextResult newResult)
    {
        float now = Time.time;

        if (now - stateEnterTime < 0.5f)
        {
            return new ContextResult
            {
                state = currentState,
                confidence = newResult.confidence
            };
        }

        if (newResult.state != currentState)
        {
            if (pendingState != newResult.state)
            {
                pendingState = newResult.state;
                pendingStartTime = now;
            }
            else
            {
                if (now - pendingStartTime >= 0.2f)
                {
                    currentState = pendingState;
                    stateEnterTime = now;
                }
            }
        }

        return new ContextResult
        {
            state = currentState,
            confidence = newResult.confidence
        };
    }
}
