using GLDebug;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using static TouchHandler;

public class Main : MonoBehaviour
{
    TouchHandler touchHandler;
    KeyboardDraw keyboardDraw;
    SynthPlayer synthPlayer;

    List<KeyControl> keyboardKeys = new List<KeyControl>();

    [Min(1)]
    public int edo = 12;

    public TMP_FontAsset font;
    public Slider edoSlider;

    private void Awake()
    {
        KeyboardDraw.font = font;

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
            Keyboard.current.numpad0Key,
            Keyboard.current.numpadPeriodKey,
            Keyboard.current.numpad1Key,
            Keyboard.current.numpad2Key,
            Keyboard.current.numpad3Key,
            Keyboard.current.numpad4Key,
            Keyboard.current.numpad5Key,
            Keyboard.current.numpad6Key,
            Keyboard.current.numpad7Key,
            Keyboard.current.numpad8Key,
            Keyboard.current.numpad9Key,
        };
    }


    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            synthPlayer.IncrementSynthIndex();
        }

        if (Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            synthPlayer.ReleaseAllNotes();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Application.Quit();
        }

        if (Keyboard.current.numpadPlusKey.wasPressedThisFrame)
        {
            edoSlider.value++;
        }

        if (Keyboard.current.numpadMinusKey.wasPressedThisFrame)
        {
            edoSlider.value--;
        }

        edoSlider.value += Mouse.current.scroll.value.y;

        // touchscreen input
        TouchList touchList = touchHandler.GetTouchList();

        for (int i = 0; i < touchList.Count; i++)
        {
            int id = touchList.ids[i];
            if (TouchingKey(touchList.positions[i]))
            {
                if (touchList.wasPressedThisFrame[i] || !synthPlayer.NoteIDList.Contains(id))
                {
                    synthPlayer.AddNote(new Note(GetFrequencyFromKeyPosition(touchList.positions[i]), 0, synthPlayer.GetSynth()), id);
                }
                else if(synthPlayer.NoteIDList.Contains(id))
                {
                    synthPlayer.SetNoteFrequency(id, GetFrequencyFromKeyPosition(touchList.positions[i]));
                }
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
        int I = 0;
        for (int i = 0; i < Mathf.Min(keyboardKeys.Count, edo + 1); i++)
        {
            int id = -i - 1;
            if (keyboardKeys[i].wasPressedThisFrame)
            {
                synthPlayer.AddNote(new Note(GetFrequencyFromKeyPosition(keyboardDraw.Positions[i]), 0, synthPlayer.GetSynth()), id);
            }
            else if (keyboardKeys[i].wasReleasedThisFrame && synthPlayer.NoteIDList.Contains(id))
            {
                synthPlayer.ReleaseNote(id);
            }

            if (keyboardKeys[i].isPressed && i < keyboardDraw.Positions.Count)
                keyPressPositions.Add(keyboardDraw.Positions[i]);

            I = i;
        }

        if (synthPlayer.NoteIDList.Count > 0)
        {
            for (int i = I + 1; i < keyboardKeys.Count; i++)
            {
                int id = -i - 1;
                if (keyboardKeys[i].wasReleasedThisFrame && synthPlayer.NoteIDList.Contains(id))
                {
                    synthPlayer.ReleaseNote(id);
                }
            }
        }

        keyPressPositions.AddRange(touchList.positions.ToList());
        keyboardDraw.Draw(edo, keyPressPositions);

        DrawGizmos();
    }

    //float GetFrequencyFromPosition(float xPos)
    //{
    //    return 220f.AddCents(Mathf.Lerp(0, 1200, Mathf.InverseLerp(Camera.main.ScreenToWorldPoint(new Vector2(0, 0)).x, Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, 0)).x, xPos)));
    //}

    float GetFrequencyFromKeyPosition(Vector2 position)
    {
        return 200f.AddInterval(edo, keyboardDraw.GetKey(position));
    }

    bool TouchingKey(Vector2 position) => keyboardDraw.GetKey(position) != -1;

    public static float GetCentDifference(float frequency1, float frequency2) => 1200 * Mathf.Log(frequency2 / frequency1, 2);

    public void SetEdo()
    {
        edo = (int)edoSlider.value;
    }

    public void DrawGizmos()
    {
        GLGizmos.SetColor(Color.white);
        GLGizmos.DrawText($"{edo}edo", new Vector2(6.25f, 4f), font, 8, new TextBoxParams() { alignment = TextAlignmentOptions.Left, positionPivot = PositionPivot.Left });
    }
}
