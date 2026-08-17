using UnityEngine;
using System;
using GLDebug;
using static TouchHandler;

public class UIButton
{
    Vector2 position;
    Vector2 size;

    UIButtonState state;
    UIButtonState prevState;
    bool suppressHold;

    const float HOLD_THRESHOLD = .25f;
    const float HOLD_PADDING = .05f;
    float timeHeld;

    int currentTouchId = -1;
    public bool Activated { get; private set; }
    public int ActivateCount { get; private set; }
    public bool Pressed => state != UIButtonState.Off;

    public UIButton(Vector2 position, Vector2 size, bool suppressHold)
    {
        state = UIButtonState.Off;
        this.position = position;
        this.size = size;
        this.suppressHold = suppressHold;
    }

    public void SetPositionAndSize(Vector2 pos, Vector2 s)
    {
        position = pos;
        size = s;
    }

    public void UpdateState(TouchList touchList)
    {
        int activeIDIndex = Array.IndexOf(touchList.ids, currentTouchId);

        if (activeIDIndex != -1)
        {
            bool withinBounds = WithinVector2Bounds(touchList.positions[activeIDIndex]);
            if (withinBounds)
            {
                timeHeld += Time.deltaTime;
            }
            else
            {
                timeHeld = 0;
                currentTouchId = -1;
                state = UIButtonState.Off;
            }
        }
        else
        {
            if (currentTouchId != -1)
            {
                currentTouchId = -1;
                state = UIButtonState.Off;
            }
        }

        if (currentTouchId == -1 && touchList.Count > 0)
        {
            for (int i = 0; i < touchList.Count; i++)
            {
                int id = touchList.ids[i];
                bool withinBounds = WithinVector2Bounds(touchList.positions[i]);
                bool pressedThisFrame = touchList.wasPressedThisFrame[i];

                if(withinBounds && pressedThisFrame && currentTouchId == -1)
                {
                    currentTouchId = id;
                    state = UIButtonState.Pressed;
                    timeHeld = 0;
                    break;
                }
            }
        }

        switch (state)
        {
            case UIButtonState.Pressed:
                if (timeHeld >= HOLD_THRESHOLD)
                {
                    state = UIButtonState.Held;
                    timeHeld = 0;
                }
                break;
            case UIButtonState.Held:
                if (timeHeld >= HOLD_PADDING)
                {
                    timeHeld = 0;
                }
                break;
        }

        Activated = timeHeld == 0 && (suppressHold ? state == UIButtonState.Pressed : state != UIButtonState.Off);
        if (Activated)
            ActivateCount++;

        prevState = state;
    }

    bool WithinVector2Bounds(Vector2 pos)
    {
        return pos.x > position.x - size.x * .5f &&
               pos.x < position.x + size.x * .5f &&
               pos.y > position.y - size.y * .5f &&
               pos.y < position.y + size.y * .5f;
    }

    public void PrintState()
    {
        Debug.Log($"{state.ToString()}: {timeHeld}");
    }

    public void DebugDraw()
    {
        GLGizmos.DrawSolidBox(position, size).SetColor(!Pressed ? Color.red : Color.green);
    }
}

public enum UIButtonState
{
    Off,
    Pressed,
    Held
}
