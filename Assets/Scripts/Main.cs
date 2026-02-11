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
    public enum PartitionMode
    {
        Edo, Cent, Custom
    }
    public PartitionMode partitionMode;

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
            if (partitionMode == PartitionMode.Edo)
                cents = GetCentDifference(SynthPlayer.baseFrequency, SynthPlayer.baseFrequency.AddInterval(edo, 1, Period));

            return cents;
        }
    }

    public float cents;

    public TMP_FontAsset font;
    public Slider edoSlider;
    public Slider periodNSlider;
    public Slider periodDSlider;
    public Slider extendSlider;
    public Slider shiftSlider;
    public Slider driftSlider;

    Vector2 NTextPosition;
    Vector2 DTextPosition;
    Vector2 XTextPosition;
    Vector2 ShiftTextPosition;

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
        ShiftTextPosition = Camera.main.ScreenToWorldPoint(driftSlider.transform.GetChild(0).position);

        //AdjustShiftRange();
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
            NextSynth();
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
            //edoSlider.value++;
            shift++;
            AdjustDrift();
        }

        if (Keyboard.current.numpadMinusKey.wasPressedThisFrame)
        {
            //edoSlider.value--;
            shift--;
            AdjustDrift();
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

        //shift = Mathf.RoundToInt(octaveShift * edo * (2 / Period));
        //shift = Mathf.RoundToInt(GetIntervalDifference(SynthPlayer.baseFrequency, SynthPlayer.baseFrequency.AddCents(1200 * drift), edo, Period));
    }

    public void NextSynth()
    {
        synthPlayer.IncrementSynthIndex();
    }

    //float GetFrequencyFromPosition(float xPos)
    //{
    //    return 220f.AddCents(Mathf.Lerp(0, 1200, Mathf.InverseLerp(Camera.main.ScreenToWorldPoint(new Vector2(0, 0)).x, Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, 0)).x, xPos)));
    //}

    float GetFrequencyFromKeyPosition(Vector2 position)
    {
        switch (partitionMode)
        {
            case PartitionMode.Edo:
                return SynthPlayer.baseFrequency.AddInterval(edo, keyboardDraw.GetKey(position) + shift, Period);
            case PartitionMode.Cent:
                return SynthPlayer.baseFrequency.AddCents(cents * (keyboardDraw.GetKey(position) + shift));
            case PartitionMode.Custom:
                break;
        }

        return SynthPlayer.baseFrequency;
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

        AdjustShift();
    }

    public void SetPeriodN()
    {
        periodN = (int)periodNSlider.value;
        periodDSlider.maxValue = periodN;
        SetPeriodD();

        AdjustShift();
    }
    public void SetPeriodD()
    {
        periodD = (int)periodDSlider.value;

        AdjustShift();
    }
    public void SetExtend() => extend = (int)extendSlider.value;
    public void SetShift() => shift = (int)shiftSlider.value;
    public void SetDrift() => AdjustShift();
    void AdjustShiftRange()
    {
        shiftSlider.maxValue = Mathf.Max(GetIntervalDifference(SynthPlayer.baseFrequency, maxFrequency, edo, Period) - (edo * 2), 0);
        shiftSlider.minValue = Mathf.Min(-GetIntervalDifference(minFrequency, SynthPlayer.baseFrequency, edo, Period), 0);
        SetShift();
    }
    void AdjustShift()
    {
        shift = Mathf.RoundToInt(GetIntervalDifference(SynthPlayer.baseFrequency, SynthPlayer.baseFrequency.AddCents(1200 * driftSlider.value), edo, Period));
    }

    void AdjustDrift()
    {
        float drift = GetCentDifference(SynthPlayer.baseFrequency, SynthPlayer.baseFrequency.AddInterval(edo, shift, Period)) / 1200;
        driftSlider.value = drift;
    }

    public void ResetDrift()
    {
        driftSlider.value = 0;
        AdjustShift();
    }

    public void ResetSliders()
    {
        extendSlider.value = 0;
        edoSlider.value = 12;
        SetEdo();

        driftSlider.value = 0;
        periodDSlider.value = 1;
        periodNSlider.value = 2;
        SetPeriodN();
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
        GLGizmos.DrawText($"{shift}", ShiftTextPosition, font, 3f, new TextBoxParams() { alignment = TextAlignmentOptions.Right, positionPivot = PositionPivot.Right });
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
