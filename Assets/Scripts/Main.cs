using GLDebug;
using System.Collections.Generic;
using System.Linq;
using System;
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

    UIButton edoIncrementButton;
    UIButton edoDecrementButton;

    UIButton periodNIncrementButton;
    UIButton periodNDecrementButton;
    UIButton periodDIncrementButton;
    UIButton periodDDecrementButton;

    UIButton extendKeyboardIncrementButton;
    UIButton extendKeyboardDecrementButton;
    UIButton shiftKeyboardIncrementButton;
    UIButton shiftKeyboardDecrementButton;

    UIButton ResetEdoButton;

    UIButton WaveformButton;

    UIButton AIncrementButton;
    UIButton ADecrementButton;
    UIButton DIncrementButton;
    UIButton DDecrementButton;
    UIButton SIncrementButton;
    UIButton SDecrementButton;
    UIButton RIncrementButton;
    UIButton RDecrementButton;

    UIButton UnisonVoiceCountIncrementButton;
    UIButton UnisonVoiceCountDecrementButton;
    UIButton UnisonDetuneIncrementButton;
    UIButton UnisonDetuneDecrementButton;

    bool initButtons;

    private void Awake()
    {
        Application.targetFrameRate = 120;

        KeyboardDraw.font = font;

        synthPlayer = GetComponent<SynthPlayer>();
        touchHandler = new();
        keyboardDraw = new(this, edo);
        //uiDraw = new(this);

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

        edoIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        edoDecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);

        periodNIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        periodNDecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        periodDIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        periodDDecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);

        extendKeyboardIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        extendKeyboardDecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        shiftKeyboardIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        shiftKeyboardDecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);

        ResetEdoButton = new(new Vector2(0, 0), new Vector2(1, 1), true);

        WaveformButton = new(new Vector2(0, 0), new Vector2(1, 1), true);

        AIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        ADecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        DIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        DDecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        SIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        SDecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        RIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);
        RDecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false);

        UnisonVoiceCountIncrementButton = new(new Vector2(1, 4), new Vector2(1, 1), false);
        UnisonVoiceCountDecrementButton = new(new Vector2(2, 4), new Vector2(1, 1), false);
        UnisonDetuneIncrementButton = new(new Vector2(3, 4), new Vector2(1, 1), false);
        UnisonDetuneDecrementButton = new(new Vector2(4, 4), new Vector2(1, 1), false);
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

        UpdateButtons(touchList);

        for (int i = 0; i < touchList.Count; i++)
        {
            int id = touchList.ids[i];
            if (TouchingKey(touchList.positions[i]))
            {
                if (touchList.wasPressedThisFrame[i] || !synthPlayer.NoteIDList.Contains(id))
                {
                    float frequency = GetFrequencyFromKeyPosition(touchList.positions[i]);
                    synthPlayer.AddNote(new Note(frequency, 0, synthPlayer.GetSynth()), id);
                    //Debug.Log($"{frequency} Hz");
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
        DrawUI();

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
        
        // w/buttons
        int maxShift = edo * 3;
        shift = Mathf.Clamp(shift, -maxShift, maxShift);
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

    void UpdateButton(UIButton button, TouchList touchList, Action activateAction, bool debugDraw)
    {
        button.UpdateState(touchList);

        if (button.Activated)
            activateAction.Invoke();

        if (debugDraw)
            button.DebugDraw();
    }
    void UpdateButtons(TouchList touchList)
    {
        UpdateButton(edoIncrementButton, touchList, IncrementEdo, true);
        UpdateButton(edoDecrementButton, touchList, DecrementEdo, true);

        UpdateButton(periodNIncrementButton, touchList, IncrementPeriodN, true);
        UpdateButton(periodNDecrementButton, touchList, DecrementPeriodN, true);
        UpdateButton(periodDIncrementButton, touchList, IncrementPeriodD, true);
        UpdateButton(periodDDecrementButton, touchList, DecrementPeriodD, true);

        UpdateButton(extendKeyboardIncrementButton, touchList, IncrementExtend, true);
        UpdateButton(extendKeyboardDecrementButton, touchList, DecrementExtend, true);
        UpdateButton(shiftKeyboardIncrementButton, touchList, IncrementShift, true);
        UpdateButton(shiftKeyboardDecrementButton, touchList, DecrementShift, true);

        UpdateButton(ResetEdoButton, touchList, ResetEdoParams, true);

        UpdateButton(WaveformButton, touchList, UpdateWaveform, false);

        UpdateButton(AIncrementButton, touchList, IncrementA, false);
        UpdateButton(ADecrementButton, touchList, DecrementA, false);
        UpdateButton(DIncrementButton, touchList, IncrementD, false);
        UpdateButton(DDecrementButton, touchList, DecrementD, false);
        UpdateButton(SIncrementButton, touchList, IncrementS, false);
        UpdateButton(SDecrementButton, touchList, DecrementS, false);
        UpdateButton(RIncrementButton, touchList, IncrementR, false);
        UpdateButton(RDecrementButton, touchList, DecrementR, false);

        UpdateButton(UnisonVoiceCountIncrementButton, touchList, IncrementUnisonVoiceCount, true);
        UpdateButton(UnisonVoiceCountDecrementButton, touchList, DecrementUnisonVoiceCount, true);
        UpdateButton(UnisonDetuneIncrementButton, touchList, IncrementUnisonDetune, true);
        UpdateButton(UnisonDetuneDecrementButton, touchList, DecrementUnisonDetune, true);
    }

    void IncrementEdo()
    {
        edo++;
        ClampExtend();
        AdjustShift();
    }
    void DecrementEdo()
    {
        edo--;
        edo = Mathf.Max(edo, 1);
        ClampExtend();
        AdjustShift();
    }
    void ClampExtend()
    {
        extend = Mathf.Clamp(extend, 0, Mathf.Max(edo, 12));
    }

    void IncrementPeriodN()
    {
        periodN++;
        periodD = Mathf.Clamp(periodD, 1, periodN);
        AdjustShift();
    }
    void DecrementPeriodN()
    {
        periodN--;
        periodN = Mathf.Max(periodN, 1);
        periodD = Mathf.Clamp(periodD, 1, periodN);
        AdjustShift();
    }
    void IncrementPeriodD()
    {
        periodD++;
        periodD = Mathf.Clamp(periodD, 1, periodN);
        AdjustShift();
    }
    void DecrementPeriodD()
    {
        periodD--;
        periodD = Mathf.Clamp(periodD, 1, periodN);
        AdjustShift();
    }

    void IncrementExtend()
    {
        extend++;
        ClampExtend();
    }
    void DecrementExtend()
    {
        extend--;
        ClampExtend();
    }

    void IncrementShift()
    {
        shift++;
        int maxShift = edo * 3;
        shift = Mathf.Clamp(shift, -maxShift, maxShift);
        AdjustDrift();
    }
    void DecrementShift()
    {
        shift--;
        int maxShift = edo * 3;
        shift = Mathf.Clamp(shift, -maxShift, maxShift);
        AdjustDrift();
    }

    public void ResetEdoParams()
    {
        extend = 0;
        shift = 0;
        edo = 12;
        driftSlider.value = 0;
        periodN = 2;
        periodD = 1;
    }

    void UpdateWaveform()
    {
        Synth s = synthPlayer.GetSynth(0);
        s.IncrementWaveformSkipNoise();
    }

    void IncrementA()
    {
        Synth s = synthPlayer.GetSynth(0);

        if (s.adsr.attack < .095)
            s.adsr.attack = Mathf.Clamp(s.adsr.attack + .01f, .01f, 1);
        else
            s.adsr.attack = Mathf.Clamp(s.adsr.attack + .1f, .01f, 1);
    }
    void DecrementA()
    {
        Synth s = synthPlayer.GetSynth(0);

        if (s.adsr.attack <= .1)
            s.adsr.attack = Mathf.Clamp(s.adsr.attack - .01f, .01f, 1);
        else
            s.adsr.attack = Mathf.Clamp(s.adsr.attack - .1f, .01f, 1);
    }
    void IncrementD()
    {
        Synth s = synthPlayer.GetSynth(0);

        if (s.adsr.decay < .095)
            s.adsr.decay = Mathf.Clamp(s.adsr.decay + .01f, .01f, 1);
        else
            s.adsr.decay = Mathf.Clamp(s.adsr.decay + .1f, .01f, 1);
    }
    void DecrementD()
    {
        Synth s = synthPlayer.GetSynth(0);

        if (s.adsr.decay <= .1)
            s.adsr.decay = Mathf.Clamp(s.adsr.decay - .01f, .01f, 1);
        else
            s.adsr.decay = Mathf.Clamp(s.adsr.decay - .1f, .01f, 1);
    }
    void IncrementS()
    {
        Synth s = synthPlayer.GetSynth(0);

        if (s.adsr.sustain < .095)
            s.adsr.sustain = Mathf.Clamp(s.adsr.sustain + .01f, .01f, 1);
        else
            s.adsr.sustain = Mathf.Clamp(s.adsr.sustain + .1f, .01f, 1);
    }
    void DecrementS()
    {
        Synth s = synthPlayer.GetSynth(0);

        if (s.adsr.sustain <= .1)
            s.adsr.sustain = Mathf.Clamp(s.adsr.sustain - .01f, .01f, 1);
        else
            s.adsr.sustain = Mathf.Clamp(s.adsr.sustain - .1f, .01f, 1);
    }
    void IncrementR()
    {
        Synth s = synthPlayer.GetSynth(0);

        if (s.adsr.release < .095)
            s.adsr.release = Mathf.Clamp(s.adsr.release + .01f, .01f, 1);
        else
            s.adsr.release = Mathf.Clamp(s.adsr.release + .1f, .01f, 1);
    }
    void DecrementR()
    {
        Synth s = synthPlayer.GetSynth(0);

        if (s.adsr.release <= .1)
            s.adsr.release = Mathf.Clamp(s.adsr.release - .01f, .01f, 1);
        else
            s.adsr.release = Mathf.Clamp(s.adsr.release - .1f, .01f, 1);
    }

    void IncrementUnisonVoiceCount()
    {
        Synth s = synthPlayer.GetSynth(0);
        s.voiceCount = Mathf.Clamp(s.voiceCount + 1, 1, 5);
    }
    void DecrementUnisonVoiceCount()
    {
        Synth s = synthPlayer.GetSynth(0);
        s.voiceCount = Mathf.Clamp(s.voiceCount - 1, 1, 5);
    }
    void IncrementUnisonDetune()
    {
        Synth s = synthPlayer.GetSynth(0);
        s.detune = Mathf.Clamp(s.detune + 1, 0, 25);
    }
    void DecrementUnisonDetune()
    {
        Synth s = synthPlayer.GetSynth(0);
        s.detune = Mathf.Clamp(s.detune - 1, 0, 25);
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

    void DrawUI()
    {
        //Vector2 screenDimensions = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 screenDimensions = new Vector2(2 * Camera.main.orthographicSize * (float)Screen.width / (float)Screen.height, 2 * Camera.main.orthographicSize);
        Vector2 uiDimensions = new Vector2(screenDimensions.x, screenDimensions.y * .3f);
        Vector2 uiPosition = new Vector2(0, screenDimensions.y * .35f);
        float uiLeft = uiPosition.x - uiDimensions.x * .5f;
        float uiRight = uiPosition.x + uiDimensions.x * .5f;
        float uiTop = uiPosition.y + uiDimensions.y * .5f;
        float uiBottom = uiPosition.y - uiDimensions.y * .5f;

        Synth synth = synthPlayer.GetSynth(0);

        float edgeRadius = .1f;

        // Debug
        GLGizmos.DrawOpenBox(uiPosition, uiDimensions);

        // Waveform Button
        GLGizmos.SetColor(new Color(.1f, .2f, .3f));
        float padding = .2f;
        Vector2 waveformButtonSize = new Vector2(2f, 1.8f);
        Vector2 waveformButtonPosition = new Vector2(uiLeft + padding + waveformButtonSize.x * .5f, uiPosition.y);
        GLGizmos.DrawSolidBoxEdgeRadius(waveformButtonPosition, waveformButtonSize, edgeRadius, 0, true, BorderType.Inside);
        GLGizmos.DrawSolidBoxEdgeRadius(waveformButtonPosition, waveformButtonSize, edgeRadius, 0, false, BorderType.Inside).SetColor(new Color(.2f, .4f, .6f));

        float textSize = 4f;
        Vector2 iconPos = waveformButtonPosition - Vector2.up * .22f;
        float iconWidth = 1f;

        GLGizmos.SetColor(Color.white);
        Synth.Waveform wav = synth.GetWaveform();
        switch (wav)
        {
            case Synth.Waveform.Sine:
                GLGizmos.DrawText("Sine", waveformButtonPosition + Vector2.up * waveformButtonSize.y * .5f, font, textSize, new() { positionPivot = PositionPivot.Top, fontStyle = FontStyles.Bold, characterSpacing = -1f, textBoxSize = new Vector2(waveformButtonSize.x, .8f) });
                GLGizmos.DrawWeightedCircle(iconPos, iconWidth * .5f, .2f, BorderType.Inside, -2);
                break;
            case Synth.Waveform.Triangle:
                GLGizmos.DrawText("Triangle", waveformButtonPosition + Vector2.up * waveformButtonSize.y * .5f, font, textSize, new() { positionPivot = PositionPivot.Top, fontStyle = FontStyles.Bold, characterSpacing = -1f, textBoxSize = new Vector2(waveformButtonSize.x, .8f) });
                GLGizmos.DrawWeightedTriangle(iconPos, Vector2.zero, iconWidth, iconWidth, 0, 0, .2f, BorderType.Inside);
                break;
            case Synth.Waveform.Square:
                GLGizmos.DrawText("Square", waveformButtonPosition + Vector2.up * waveformButtonSize.y * .5f, font, textSize, new() { positionPivot = PositionPivot.Top, fontStyle = FontStyles.Bold, characterSpacing = -1f, textBoxSize = new Vector2(waveformButtonSize.x, .8f) });
                GLGizmos.DrawWeightedBox(iconPos, Vector2.one * iconWidth, .2f, BorderType.Inside);
                break;
            case Synth.Waveform.Saw:
                GLGizmos.DrawText("Saw", waveformButtonPosition + Vector2.up * waveformButtonSize.y * .5f, font, textSize, new() { positionPivot = PositionPivot.Top, fontStyle = FontStyles.Bold, characterSpacing = -1f, textBoxSize = new Vector2(waveformButtonSize.x, .8f) });
                GLGizmos.DrawWeightedTriangle(iconPos, Vector2.zero, iconWidth, iconWidth, -1, 0, .2f, BorderType.Inside);
                break;
        }

        // ADSR
        Vector2 adsrButtonSize = new Vector2(3f, .5f);
        Vector2 adsrIncrementButtonSize = new Vector2(.5f, .5f);
        float adsrButtonPositionX = uiLeft + padding * 5 + waveformButtonSize.x + adsrButtonSize.x * .5f;
        float adsrDecrementButtonPositionX = uiLeft + padding * 5 + waveformButtonSize.x + adsrIncrementButtonSize.x * .5f;
        float adsrIncrementButtonPositionX = adsrButtonPositionX + (adsrButtonSize.x - adsrIncrementButtonSize.x) * .5f;
        
        float y = 3;
        for (int i = 0; i < 4; i++)
        {
            GLGizmos.SetColor(new Color(1, 1, 1, .3f));
            float yPos = uiPosition.y + uiDimensions.y * .9f * (y / 8f);

            Vector2 decrementPos = new Vector2(adsrDecrementButtonPositionX, yPos);
            Vector2 incrementPos = new Vector2(adsrIncrementButtonPositionX, yPos);

            if (!initButtons)
            {
                (i switch { 0 => ADecrementButton, 1 => DDecrementButton, 2 => SDecrementButton, 3 => RDecrementButton }).SetPositionAndSize(decrementPos, adsrIncrementButtonSize);
                (i switch { 0 => AIncrementButton, 1 => DIncrementButton, 2 => SIncrementButton, 3 => RIncrementButton }).SetPositionAndSize(incrementPos, adsrIncrementButtonSize);
            }

            GLGizmos.DrawText(i switch { 0 => "A:", 1 => "D:", 2 => "S:", 3 => "R:" }, new Vector2(uiLeft + padding * 3.5f + waveformButtonSize.x, yPos), font, 4);

            GLGizmos.DrawSolidBox(decrementPos, adsrIncrementButtonSize).SetColor(new Color(1f, .3f, 0,
                (i switch { 0 => ADecrementButton.Pressed, 1 => DDecrementButton.Pressed, 2 => SDecrementButton.Pressed, 3 => RDecrementButton.Pressed }) ? .4f : .2f
                ));
            GLGizmos.DrawSolidBox(incrementPos, adsrIncrementButtonSize).SetColor(new Color(0, 1f, .3f,
                (i switch { 0 => AIncrementButton.Pressed, 1 => DIncrementButton.Pressed, 2 => SIncrementButton.Pressed, 3 => RIncrementButton.Pressed }) ? .4f : .2f
                ));

            float adsrValueWidth = (i switch { 0 => synth.adsr.attack, 1 => synth.adsr.decay, 2 => synth.adsr.sustain, 3 => synth.adsr.release }) * (adsrButtonSize.x - adsrIncrementButtonSize.x * 2);
            float adsrValuePosX = adsrDecrementButtonPositionX + (adsrIncrementButtonSize.x + adsrValueWidth) * .5f;
            GLGizmos.DrawSolidBox(new Vector2(adsrValuePosX, yPos), new Vector2(adsrValueWidth, .5f)).SetColor(new Color(0, .4f, 1));


            GLGizmos.DrawOpenBox(new Vector2(adsrButtonPositionX, yPos), adsrButtonSize);
            GLGizmos.DrawOpenBox(decrementPos, adsrIncrementButtonSize);
            GLGizmos.DrawOpenBox(incrementPos, adsrIncrementButtonSize);

            GLGizmos.SetColor(Color.white);
            GLGizmos.DrawText("-", decrementPos, font, 4, new() { fontStyle = FontStyles.Bold });
            GLGizmos.DrawText("+", incrementPos, font, 4, new() { fontStyle = FontStyles.Bold });
            GLGizmos.DrawText((i switch { 0 => synth.adsr.attack, 1 => synth.adsr.decay, 2 => synth.adsr.sustain, 3 => synth.adsr.release }).ToString("0.##"),
                               decrementPos + Vector2.right * .25f, null, 3, new() { alignment = TextAlignmentOptions.Left, positionPivot = PositionPivot.Left});

            y -= 2;
        }

        

        if (!initButtons)
        {
            WaveformButton.SetPositionAndSize(waveformButtonPosition, waveformButtonSize);
        }

        initButtons = true;
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
