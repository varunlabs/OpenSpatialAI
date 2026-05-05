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
        ContextState outputState = currentState;

        if (now - stateEnterTime < 0.5f)
        {
            return new ContextResult
            {
                state = currentState,
                confidence = newResult.confidence
            };
        }

        bool isChangingState = newResult.state != currentState;

        // If fusion explicitly says Transitioning, surface it immediately.
        if (isChangingState && newResult.state == ContextState.Transitioning)
        {
            return new ContextResult
            {
                state = ContextState.Transitioning,
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
            
            // Show candidate stable state color/label while confirming hysteresis.
            outputState = pendingState;
        }
        else
        {
            pendingState = currentState;
            outputState = currentState;
        }

        return new ContextResult
        {
            state = outputState,
            confidence = newResult.confidence
        };
    }
}
