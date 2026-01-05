using GLDebug;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
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
    [Min(1)]
    public int periodN = 2;
    [Min(1)]
    public int periodD = 1;
    
    public int extend = 0;
    public int shift = 0;

    float maxFrequency = 1760f;
    float minFrequency = 27.5f;

    public float Period
    {
        get
        {
            float value = (float)periodN / (float)periodD;
            return Mathf.Max(value, 1);
        }
    }

    public float CentsPerNote
    {
        get
        {
            return GetCentDifference(SynthPlayer.baseFrequency, SynthPlayer.baseFrequency.AddInterval(edo, 1, Period));
        }
    }

    public TMP_FontAsset font;
    public Slider edoSlider;
    public Slider periodNSlider;
    public Slider periodDSlider;
    public Slider extendSlider;
    public Slider shiftSlider;

    Vector2 NTextPosition;
    Vector2 DTextPosition;
    Vector2 XTextPosition;

    private void Awake()
    {
        Application.targetFrameRate = 120;

        KeyboardDraw.font = font;

        synthPlayer = GetComponent<SynthPlayer>();
        touchHandler = new();
        keyboardDraw = new(this, edo);

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

        SetPeriodN();
        NTextPosition = Camera.main.ScreenToWorldPoint(periodNSlider.transform.GetChild(0).position);
        DTextPosition = Camera.main.ScreenToWorldPoint(periodDSlider.transform.GetChild(0).position);
        XTextPosition = Camera.main.ScreenToWorldPoint(extendSlider.transform.GetChild(0).position);

        AdjustShiftRange();
    }

    private void OnValidate()
    {
        if (extend <= -edo)
        {
            extend = -edo + 1;
        }
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
                    float frequency = GetFrequencyFromKeyPosition(touchList.positions[i]);
                    synthPlayer.AddNote(new Note(frequency, 0, synthPlayer.GetSynth()), id);
                    Debug.Log($"{frequency} Hz");
                }
                else if(synthPlayer.NoteIDList.Contains(id))
                {
                    float newFrequency = GetFrequencyFromKeyPosition(touchList.positions[i]);
                    synthPlayer.SetNoteFrequency(id, newFrequency);

                    //float currFrequency = synthPlayer.GetNote(id).frequency;
                    //if (currFrequency != newFrequency)
                    //{
                    //    synthPlayer.ReleaseNote(id);
                    //    synthPlayer.AddNote(new Note(newFrequency, 0, synthPlayer.GetSynth()), id);
                    //}
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
        for (int i = 0; i < Mathf.Min(keyboardKeys.Count, edo + 1 + extend); i++)
        {
            int id = -i - 1;
            if (keyboardKeys[i].wasPressedThisFrame)
            {
                synthPlayer.AddNote(new Note(GetFrequencyFromKeyPosition(keyboardDraw.Positions[i]), 0, synthPlayer.GetSynth()), id);
                //Debug.Log($"{GetFrequencyFromKeyPosition(keyboardDraw.Positions[i])} Hz");
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
        return SynthPlayer.baseFrequency.AddInterval(edo, keyboardDraw.GetKey(position) + shift, Period);
    }

    bool TouchingKey(Vector2 position) => keyboardDraw.GetKey(position) != -1;

    public float GetCentDifference(float frequency1, float frequency2) => 1200 * Mathf.Log(frequency2 / frequency1, 2);
    public float GetIntervalDifference(float frequency1, float frequency2, int edo, float period) => edo * Mathf.Log(frequency2 / frequency1, period);

    public void SetEdo()
    {
        edo = (int)edoSlider.value;

        extendSlider.minValue = 0;// -edo + 1;
        extendSlider.maxValue = Mathf.Max(edo, 12);
        SetExtend();

        AdjustShiftRange();
    }

    public void SetPeriodN()
    {
        periodN = (int)periodNSlider.value;
        periodDSlider.maxValue = periodN;
        SetPeriodD();

        AdjustShiftRange();
    }
    public void SetPeriodD()
    {
        periodD = (int)periodDSlider.value;

        AdjustShiftRange();
    }
    public void SetExtend() => extend = (int)extendSlider.value;
    public void SetShift() => shift = (int)shiftSlider.value;
    void AdjustShiftRange()
    {
        shiftSlider.maxValue = Mathf.Max(GetIntervalDifference(SynthPlayer.baseFrequency, maxFrequency, edo, Period) - (edo * 2), 0);
        shiftSlider.minValue = Mathf.Min(-GetIntervalDifference(minFrequency, SynthPlayer.baseFrequency, edo, Period), 0);
        SetShift();
    }

    public void DrawGizmos()
    {
        GLGizmos.SetColor(Color.white);
        GLGizmos.DrawText($"{edo}ed", new Vector2(7.5f, 4f), font, 8, new TextBoxParams() { alignment = TextAlignmentOptions.Right, positionPivot = PositionPivot.Right });

        float period = Period;
        (string periodValue, float periodFontSize) = period == Mathf.RoundToInt(period) ? ($"{Mathf.RoundToInt(period)}", 5) : ($"{periodN}\n—\n{periodD}", 3);
        GLGizmos.DrawText(periodValue, new Vector2(8f, 4f), font, periodFontSize, new TextBoxParams() { lineSpacing = -28, fitTextToBox = true, textBoxSize = new Vector2(.5f, 1) });
        GLGizmos.DrawText($"()", new Vector2(8f, 4f), font, 9, new TextBoxParams() { characterSpacing = 20 });
        GLGizmos.DrawText($"{Mathf.Round(CentsPerNote * 10) / 10}c", new Vector2(8f, 3.4f), font, 2.5f);

        GLGizmos.DrawText($"{periodN}", NTextPosition, font, 4f, new TextBoxParams() { fontStyle = FontStyles.Bold });
        GLGizmos.DrawText($"{periodD}", DTextPosition, font, 4f, new TextBoxParams() { fontStyle = FontStyles.Bold });
        GLGizmos.DrawText($"{extend}", XTextPosition, font, 4f, new TextBoxParams() { fontStyle = FontStyles.Bold });
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }
}
