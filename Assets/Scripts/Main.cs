using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using static TouchHandler;

public class Main : MonoBehaviour
{
    TouchHandler touchHandler;
    KeyboardDraw keyboardDraw;
    SynthPlayer synthPlayer;

    List<KeyControl> keyboardKeys = new List<KeyControl>();

    [Min(1)]
    public int edo = 12;

    private void Awake()
    {
        synthPlayer = GetComponent<SynthPlayer>();
        touchHandler = new();
        keyboardDraw = new(edo);

        keyboardKeys = new List<KeyControl>()
        {
            Keyboard.current.backquoteKey,
            Keyboard.current.digit1Key,
            Keyboard.current.digit2Key,
            Keyboard.current.digit3Key,
            Keyboard.current.digit4Key,
            Keyboard.current.digit5Key,
            Keyboard.current.digit6Key,
            Keyboard.current.digit7Key,
            Keyboard.current.digit8Key,
            Keyboard.current.digit9Key,
            Keyboard.current.digit0Key,
            Keyboard.current.minusKey,
            Keyboard.current.equalsKey,
            Keyboard.current.qKey,
            Keyboard.current.wKey,
            Keyboard.current.eKey,
            Keyboard.current.rKey,
            Keyboard.current.tKey,
            Keyboard.current.yKey,
            Keyboard.current.uKey,
            Keyboard.current.iKey,
            Keyboard.current.oKey,
            Keyboard.current.pKey,
            Keyboard.current.leftBracketKey,
            Keyboard.current.rightBracketKey,
            Keyboard.current.backslashKey,
        };
    }


    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            synthPlayer.IncrementSynthIndex();
        }

        // touchscreen input
        TouchList touchList = touchHandler.GetTouchList();

        for (int i = 0; i < touchList.Count; i++)
        {
            if (touchList.wasPressedThisFrame[i])
            {
                synthPlayer.AddNote(new Note(GetFrequencyFromKeyPosition(touchList.positions[i]), 0, synthPlayer.GetSynth()), touchList.ids[i]);
            }
            else
            {
                synthPlayer.SetNoteFrequency(touchList.ids[i], GetFrequencyFromKeyPosition(touchList.positions[i]));
            }
        }

        foreach (int id in synthPlayer.NoteIDList)
        {
            if (!touchList.ids.Contains(id) && id >= 0)
            {
                synthPlayer.ReleaseNote(id);
            }
        }

        // keyboard input
        List<Vector2> keyPressPositions = new();
        for (int i = 0; i < Mathf.Min(keyboardKeys.Count, edo + 1); i++)
        {
            int id = -i - 1;
            if (keyboardKeys[i].wasPressedThisFrame)
            {
                synthPlayer.AddNote(new Note(GetFrequencyFromKeyPosition(keyboardDraw.Positions[i]), 0, synthPlayer.GetSynth()), id);
            }
            else if (keyboardKeys[i].wasReleasedThisFrame)
            {
                synthPlayer.ReleaseNote(id);
            }

            if (keyboardKeys[i].isPressed)
                keyPressPositions.Add(keyboardDraw.Positions[i]);
        }

        keyPressPositions.AddRange(touchList.positions.ToList());
        keyboardDraw.Draw(edo, keyPressPositions);
    }

    //float GetFrequencyFromPosition(float xPos)
    //{
    //    return 220f.AddCents(Mathf.Lerp(0, 1200, Mathf.InverseLerp(Camera.main.ScreenToWorldPoint(new Vector2(0, 0)).x, Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, 0)).x, xPos)));
    //}

    float GetFrequencyFromKeyPosition(Vector2 position)
    {
        return 200f.AddInterval(edo, keyboardDraw.GetKey(position));
    }
}
