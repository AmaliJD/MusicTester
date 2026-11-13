using GLDebug;
using System;
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

    public enum KeyboardMode
    {
        Logarithmic, Continuous, Linear
    }
    public KeyboardMode keyboardMode;

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
    public TMP_FontAsset font2;
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

    Action nullAction = () => { };

    UIButton edoSwipeButton;
    //UIButton edoIncrementButton;
    //UIButton edoDecrementButton;

    UIButton period_N_SwipeButton;
    UIButton period_D_SwipeButton;
    //UIButton periodNIncrementButton;
    //UIButton periodNDecrementButton;
    //UIButton periodDIncrementButton;
    //UIButton periodDDecrementButton;

    UIButton extendKeyboardIncrementButton;
    UIButton extendKeyboardDecrementButton;
    UIButton shiftKeyboardIncrementButton;
    UIButton shiftKeyboardDecrementButton;

    UIButton ResetEdoButton;
    UIButton KeyboardModeButton;
    UIButton WaveformButton;

    int adsr_select;
    UIButton A_SelectButton;
    UIButton D_SelectButton;
    UIButton S_SelectButton;
    UIButton R_SelectButton;
    UIButton ADSR_IncrementButton;
    UIButton ADSR_DecrementButton;

    UIButton UnisonCycleButton;
    //UIButton UnisonVoiceCountIncrementButton;
    //UIButton UnisonVoiceCountDecrementButton;
    //UIButton UnisonDetuneIncrementButton;
    //UIButton UnisonDetuneDecrementButton;

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

        edoSwipeButton = new(new Vector2(-.75f, 3.5f), new Vector2(1.5f, 1.5f), false, true);

        period_N_SwipeButton = new(new Vector2(.75f, 3.5f), new Vector2(1.5f, 1.5f), false, true);
        period_D_SwipeButton = new(new Vector2(2.25f, 3.5f), new Vector2(1.5f, 1.5f), false, true);

        extendKeyboardIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false, false);
        extendKeyboardDecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false, false);
        shiftKeyboardIncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false, false);
        shiftKeyboardDecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false, false);

        ResetEdoButton = new(new Vector2(0, 0), new Vector2(1, 1), true, false);
        KeyboardModeButton = new(new Vector2(0, 0), new Vector2(1, 1), true, false);
        WaveformButton = new(new Vector2(0, 0), new Vector2(1, 1), true, false);

        A_SelectButton = new(new Vector2(0, 0), new Vector2(1, 1), true, false);
        D_SelectButton = new(new Vector2(0, 0), new Vector2(1, 1), true, false);
        S_SelectButton = new(new Vector2(0, 0), new Vector2(1, 1), true, false);
        R_SelectButton = new(new Vector2(0, 0), new Vector2(1, 1), true, false);
        ADSR_IncrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false, false);
        ADSR_DecrementButton = new(new Vector2(0, 0), new Vector2(1, 1), false, false);

        UnisonCycleButton = new(new Vector2(4, 4), new Vector2(1, 1), true, false);
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

        //DrawGizmos();
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
        int index = keyboardDraw.GetKey(position) + shift;

        switch (keyboardMode)
        {
            case KeyboardMode.Logarithmic:
                switch (partitionMode)
                {
                    case PartitionMode.Edo:
                        return SynthPlayer.baseFrequency.AddInterval(edo, index, Period);
                    case PartitionMode.Cent:
                        return SynthPlayer.baseFrequency.AddCents(cents * index);
                    case PartitionMode.Custom:
                        break;
                }
                break;

            case KeyboardMode.Linear:
                float octave_index = ((index % edo) + edo) % edo;
                float octave = Mathf.Pow(Period, Mathf.Floor((float)index / (float)edo));
                float frequency = SynthPlayer.baseFrequency * (1 + (octave_index / (float)edo) * (Period - 1));
                //Debug.Log((frequency * octave) / SynthPlayer.baseFrequency);
                return frequency * octave;
                break;

            case KeyboardMode.Continuous:
                float cPos = position.x + shift * keyboardDraw.KeySize.x;
                float scale = (float)(extend + edo) / (float)edo;
                return SynthPlayer.baseFrequency.AddCents(Mathf.LerpUnclamped(0, 1200 * scale, Mathf.InverseLerp(Camera.main.ScreenToWorldPoint(new Vector2(0, 0)).x, Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, 0)).x, cPos))); ;
                break;
        }
        

        return SynthPlayer.baseFrequency;
    }

    public float GetFrequencyFromKeyIndex(int index)
    {
        switch (keyboardMode)
        {
            case KeyboardMode.Logarithmic:
                switch (partitionMode)
                {
                    case PartitionMode.Edo:
                        return SynthPlayer.baseFrequency.AddInterval(edo, index, Period);
                    case PartitionMode.Cent:
                        return SynthPlayer.baseFrequency.AddCents(cents * index);
                    case PartitionMode.Custom:
                        break;
                }
                break;

            case KeyboardMode.Linear:
                float octave_index = ((index % edo) + edo) % edo;
                float octave = Mathf.Pow(Period, Mathf.Floor((float)index / (float)edo));
                float frequency = SynthPlayer.baseFrequency * (1 + (octave_index / (float)edo) * (Period - 1));
                //Debug.Log((frequency * octave) / SynthPlayer.baseFrequency);
                return frequency * octave;
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

    void UpdateButton(UIButton button, TouchList touchList, Action activateAction, Action activateAction2, bool debugDraw)
    {
        button.UpdateState(touchList);

        if (button.Activated)
            activateAction.Invoke();

        if (button.Activated2)
            activateAction2.Invoke();

        if (debugDraw)
            button.DebugDraw();
    }
    void UpdateButtons(TouchList touchList)
    {
        UpdateButton(WaveformButton, touchList, UpdateWaveform, nullAction, true);
        UpdateButton(UnisonCycleButton, touchList, CycleUnisonVoiceCount, nullAction, true);

        UpdateButton(A_SelectButton, touchList, SelectA, nullAction, true);
        UpdateButton(D_SelectButton, touchList, SelectD, nullAction, true);
        UpdateButton(S_SelectButton, touchList, SelectS, nullAction, true);
        UpdateButton(R_SelectButton, touchList, SelectR, nullAction, true);
        UpdateButton(ADSR_IncrementButton, touchList, IncrementADSR, nullAction, true);
        UpdateButton(ADSR_DecrementButton, touchList, DecrementADSR, nullAction, true);

        UpdateButton(edoSwipeButton, touchList, IncrementEdo, DecrementEdo, true);

        UpdateButton(period_N_SwipeButton, touchList, IncrementPeriodN, DecrementPeriodN, true);
        UpdateButton(period_D_SwipeButton, touchList, IncrementPeriodD, DecrementPeriodD, true);

        UpdateButton(ResetEdoButton, touchList, ResetEdoParams, nullAction, true);
        UpdateButton(KeyboardModeButton, touchList, ToggleKeyboardMode, nullAction, true);

        UpdateButton(extendKeyboardIncrementButton, touchList, IncrementExtend, nullAction, true);
        UpdateButton(extendKeyboardDecrementButton, touchList, DecrementExtend, nullAction, true);
        UpdateButton(shiftKeyboardIncrementButton, touchList, IncrementShift, nullAction, true);
        UpdateButton(shiftKeyboardDecrementButton, touchList, DecrementShift, nullAction, true);
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
        keyboardMode = KeyboardMode.Logarithmic;
    }

    void ToggleKeyboardMode()
    {
        keyboardMode = keyboardMode switch
        {
            KeyboardMode.Logarithmic => KeyboardMode.Linear,
            KeyboardMode.Linear => KeyboardMode.Logarithmic,
            _ => KeyboardMode.Logarithmic,
        };
    }

    void UpdateWaveform()
    {
        Synth s = synthPlayer.GetSynth(0);
        s.IncrementWaveformSkipNoise();
    }

    void SelectA() => adsr_select = 0;
    void SelectD() => adsr_select = 1;
    void SelectS() => adsr_select = 2;
    void SelectR() => adsr_select = 3;

    void IncrementADSR()
    {
        switch (adsr_select)
        {
            case 0:
                IncrementA(); break;
            case 1:
                IncrementD(); break;
            case 2:
                IncrementS(); break;
            case 3:
                IncrementR(); break;
        }
    }

    void DecrementADSR()
    {
        switch (adsr_select)
        {
            case 0:
                DecrementA(); break;
            case 1:
                DecrementD(); break;
            case 2:
                DecrementS(); break;
            case 3:
                DecrementR(); break;
        }
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
    void CycleUnisonVoiceCount()
    {
        Synth s = synthPlayer.GetSynth(0);
        s.voiceCount = Mathf.Clamp((s.voiceCount + 1) % 4, 1, 3);
        s.detune = s.voiceCount switch
        {
            1 => 0,
            2 => 8,
            3 => 12
        };
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
        Color white = new Color(1, 1, 1);
        Color white_8 = new Color(1, 1, 1, .8f);
        Color white_5 = new Color(1, 1, 1, .5f);
        Color white_3 = new Color(1, 1, 1, .3f);
        Color white_1 = new Color(1, 1, 1, .1f);
        Color white_02 = new Color(1, 1, 1, .02f);
        Color blue = new Color(0, .5f, 1);
        Color blue_5 = new Color(0, .5f, 1, .5f);
        Color blue_c = new Color(.5f, .8f, 1, .8f);

        Vector2 textOffset = new Vector2(0, -.05f);

        float edgeRadius = .1f;
        float borderWidth = .1f;

        float padding = .2f;

        // ----------------------------------------- Debug
        // GLGizmos.DrawOpenBox(uiPosition, uiDimensions);


        // ----------------------------------------- Waveform Button
        Vector2 waveform_size = new Vector2(1.5f, 1.5f);
        Vector2 waveform_pos = GLGizmos.GetBoxPositionByPivot(new Vector2(uiLeft + padding + .2f, 4.1f), waveform_size, 0, PositionPivot.Left);
        if (!initButtons)
        {
            WaveformButton.SetPositionAndSize(waveform_pos, waveform_size);
        }
        Vector2 waveform_visual_size = new Vector2(1.4f, 1.3f);
        GLGizmos.SetColor(WaveformButton.Pressed ? blue : white_02);
        GLGizmos.DrawSolidBoxEdgeRadius(waveform_pos, waveform_visual_size, edgeRadius * 2, 0, true, BorderType.Inside);
        GLGizmos.SetColor(WaveformButton.Pressed ? blue_c : white_5);
        GLGizmos.DrawOpenBoxEdgeRadius(waveform_pos, waveform_visual_size, edgeRadius * 2, 0, false, BorderType.Inside);
        GLGizmos.SetColor(WaveformButton.Pressed ? white : white_8);
        TextBoxParams waveform_text_tbp = new TextBoxParams()
        {
            textBoxSize = waveform_visual_size,
            fitTextToBox = true,
            alignment = TextAlignmentOptions.Top,
        };
        Synth.Waveform wav = synth.GetWaveform();
        string waveform_type = wav switch
        {
            Synth.Waveform.Sine => "Sine",
            Synth.Waveform.Triangle => "Triangle",
            Synth.Waveform.Square => "Square",
            Synth.Waveform.Saw => "Saw",
        };
        GLGizmos.DrawText(waveform_type, waveform_pos - Vector2.up * .07f, font, 2.2f, waveform_text_tbp);

        Vector2 icon_pos = waveform_pos - Vector2.up * .1f;
        float icon_width = .8f;
        switch (wav)
        {
            case Synth.Waveform.Sine:
                GLGizmos.DrawWeightedCircle(icon_pos, icon_width * .5f, .1f, BorderType.Inside, -2);
                break;
            case Synth.Waveform.Triangle:
                GLGizmos.DrawWeightedTriangle(icon_pos, Vector2.zero, icon_width, icon_width, 0, 0, .1f, BorderType.Inside);
                break;
            case Synth.Waveform.Square:
                GLGizmos.DrawWeightedBox(icon_pos, Vector2.one * icon_width, .1f, BorderType.Inside);
                break;
            case Synth.Waveform.Saw:
                GLGizmos.DrawWeightedTriangle(icon_pos, Vector2.zero, icon_width, icon_width + .2f, -1, 0, .1f, BorderType.Inside);
                break;
        }


        //  ----------------------------------------- Unision
        Vector2 unison_size = new Vector2(1.5f, 1f);
        Vector2 unison_pos = GLGizmos.GetBoxPositionByPivot(new Vector2(uiLeft + padding + .2f, 2.75f), unison_size, 0, PositionPivot.Left);
        if (!initButtons)
        {
            UnisonCycleButton.SetPositionAndSize(unison_pos, unison_size);
        }
        Vector2 unison_visual_size = new Vector2(1.2f, .8f);
        GLGizmos.SetColor(UnisonCycleButton.Pressed ? blue : white_02);
        GLGizmos.DrawSolidBoxEdgeRadius(unison_pos, unison_visual_size, edgeRadius * 2, 0, true, BorderType.Inside);
        GLGizmos.SetColor(UnisonCycleButton.Pressed ? blue_c : white_5);
        GLGizmos.DrawOpenBoxEdgeRadius(unison_pos, unison_visual_size, edgeRadius * 2, 0, false, BorderType.Inside);
        GLGizmos.SetColor(UnisonCycleButton.Pressed ? white : white_8);
        TextBoxParams unison_text_tbp = new TextBoxParams()
        {
            textBoxSize = unison_visual_size,
            fitTextToBox = true,
            alignment = TextAlignmentOptions.Top,
        };
        TextBoxParams voice_count_tbp = new TextBoxParams()
        {
            fontStyle = FontStyles.Bold,
            textBoxSize = unison_visual_size,
            fitTextToBox = true,
        };
        GLGizmos.DrawText("Unison", unison_pos - Vector2.up * .07f, font, 2.2f, unison_text_tbp);
        GLGizmos.DrawText($"{synthPlayer.GetSynth(0).voiceCount}", unison_pos - Vector2.up * .15f, font, 5.5f, voice_count_tbp);


        // ----------------------------------------- ADSR
        // select
        Vector2 adsr_select_size = new Vector2(.8f, .8f);
        float adsr_select_padding = 0f;
        Vector2 a_select_pos = GLGizmos.GetBoxPositionByPivot(new Vector2(uiLeft + padding + 2.5f, uiPosition.y + (uiDimensions.y / 2) - .1f), adsr_select_size, 0, PositionPivot.TopLeft);
        Vector2 d_select_pos = a_select_pos + Vector2.right * (adsr_select_size.x + adsr_select_padding);
        Vector2 s_select_pos = d_select_pos + Vector2.right * (adsr_select_size.x + adsr_select_padding);
        Vector2 r_select_pos = s_select_pos + Vector2.right * (adsr_select_size.x + adsr_select_padding);
        if (!initButtons)
        {
            A_SelectButton.SetPositionAndSize(a_select_pos, adsr_select_size);
            D_SelectButton.SetPositionAndSize(d_select_pos, adsr_select_size);
            S_SelectButton.SetPositionAndSize(s_select_pos, adsr_select_size);
            R_SelectButton.SetPositionAndSize(r_select_pos, adsr_select_size);
        }
        GLGizmos.SetColor(white_02);
        GLGizmos.DrawSolidBoxEdgeRadius(a_select_pos, adsr_select_size, edgeRadius, 0, true, BorderType.Inside).SetColor(A_SelectButton.Pressed ? blue : (adsr_select == 0 ? blue_5 : white_02));
        GLGizmos.DrawSolidBoxEdgeRadius(d_select_pos, adsr_select_size, edgeRadius, 0, true, BorderType.Inside).SetColor(D_SelectButton.Pressed ? blue : (adsr_select == 1 ? blue_5 : white_02));
        GLGizmos.DrawSolidBoxEdgeRadius(s_select_pos, adsr_select_size, edgeRadius, 0, true, BorderType.Inside).SetColor(S_SelectButton.Pressed ? blue : (adsr_select == 2 ? blue_5 : white_02));
        GLGizmos.DrawSolidBoxEdgeRadius(r_select_pos, adsr_select_size, edgeRadius, 0, true, BorderType.Inside).SetColor(R_SelectButton.Pressed ? blue : (adsr_select == 3 ? blue_5 : white_02));
        GLGizmos.SetColor(white_5);
        GLGizmos.DrawOpenBoxEdgeRadius(a_select_pos, adsr_select_size, edgeRadius, 0, false, BorderType.Inside).SetColor(A_SelectButton.Pressed ? blue_c : (adsr_select == 0 ? blue_c : white_5));
        GLGizmos.DrawOpenBoxEdgeRadius(d_select_pos, adsr_select_size, edgeRadius, 0, false, BorderType.Inside).SetColor(D_SelectButton.Pressed ? blue_c : (adsr_select == 1 ? blue_c : white_5));
        GLGizmos.DrawOpenBoxEdgeRadius(s_select_pos, adsr_select_size, edgeRadius, 0, false, BorderType.Inside).SetColor(S_SelectButton.Pressed ? blue_c : (adsr_select == 2 ? blue_c : white_5));
        GLGizmos.DrawOpenBoxEdgeRadius(r_select_pos, adsr_select_size, edgeRadius, 0, false, BorderType.Inside).SetColor(R_SelectButton.Pressed ? blue_c : (adsr_select == 3 ? blue_c : white_5));
        GLGizmos.SetColor(white_5);
        Vector2 adsr_text_offset_y = Vector2.up * .1f;
        TextBoxParams bold_tbp = new TextBoxParams()
        {
            fontStyle = FontStyles.Bold,
        };
        GLGizmos.DrawText("A", a_select_pos - adsr_text_offset_y, font, 7, adsr_select != 0 ? default : bold_tbp).SetColor(A_SelectButton.Pressed ? white : (adsr_select == 0 ? white_8 : white_5));
        GLGizmos.DrawText("D", d_select_pos - adsr_text_offset_y, font, 7, adsr_select != 1 ? default : bold_tbp).SetColor(D_SelectButton.Pressed ? white : (adsr_select == 1 ? white_8 : white_5));
        GLGizmos.DrawText("S", s_select_pos - adsr_text_offset_y, font, 7, adsr_select != 2 ? default : bold_tbp).SetColor(S_SelectButton.Pressed ? white : (adsr_select == 2 ? white_8 : white_5));
        GLGizmos.DrawText("R", r_select_pos - adsr_text_offset_y, font, 7, adsr_select != 3 ? default : bold_tbp).SetColor(R_SelectButton.Pressed ? white : (adsr_select == 3 ? white_8 : white_5));

        // bar value
        Vector2 bar_outline_size = new Vector2(adsr_select_size.x * 4 + adsr_select_padding * 3, .75f);
        Vector2 bar_outline_pos = new Vector2((d_select_pos.x + s_select_pos.x) * .5f, 3.5f);
        float adsr_bar_value = adsr_select switch
        {
            0 => synthPlayer.GetSynth(0).adsr.attack,
            1 => synthPlayer.GetSynth(0).adsr.decay,
            2 => synthPlayer.GetSynth(0).adsr.sustain,
            3 => synthPlayer.GetSynth(0).adsr.release,
            _ => 0
        };
        Vector2 bar_size = new Vector2(bar_outline_size.x * adsr_bar_value, bar_outline_size.y);
        Vector2 bar_pos = GLGizmos.GetBoxPositionByPivot(bar_outline_pos - Vector2.right * bar_outline_size.x * .5f, bar_size, 0, PositionPivot.Left);
        GLGizmos.SetColor(blue_5);
        GLGizmos.DrawSolidBox(bar_pos, bar_size);
        GLGizmos.SetColor(white_5);
        GLGizmos.DrawOpenBox(bar_outline_pos, bar_outline_size);

        TextBoxParams adsr_text_tbp = new TextBoxParams()
        {
            fontStyle = FontStyles.Bold,
            textBoxSize = bar_outline_size,
            fitTextToBox = true,
            alignment = TextAlignmentOptions.Left,
            positionPivot = PositionPivot.Left,
        };
        GLGizmos.SetColor(white);
        GLGizmos.DrawText($"{adsr_bar_value.ToString("#.##")}", bar_outline_pos - Vector2.right * (bar_outline_size.x * .5f - .1f), font2, 5, adsr_text_tbp);

        // increment
        float adsr_increment_padding = 0f;
        Vector2 adsr_increment_size = new Vector2((adsr_select_size.x * 2 + adsr_select_padding * 1.5f) - adsr_increment_padding * .5f, .8f);
        Vector2 adsr_decrement_pos = GLGizmos.GetBoxPositionByPivot(new Vector2(uiLeft + padding + 2.5f, uiPosition.y + (uiDimensions.y / 2) - 2f), adsr_increment_size, 0, PositionPivot.TopLeft);
        Vector2 adsr_increment_pos = adsr_decrement_pos + Vector2.right * (adsr_increment_size.x + adsr_increment_padding);
        if (!initButtons)
        {
            ADSR_DecrementButton.SetPositionAndSize(adsr_decrement_pos, adsr_increment_size);
            ADSR_IncrementButton.SetPositionAndSize(adsr_increment_pos, adsr_increment_size);
        }

        Vector2 adsr_increment_visual_size = new Vector2(adsr_increment_size.x, .7f);
        GLGizmos.SetColor(white_02);
        GLGizmos.DrawSolidBoxEdgeRadius(adsr_decrement_pos, adsr_increment_visual_size, edgeRadius, 0, true, BorderType.Inside).SetColor(ADSR_DecrementButton.Pressed ? blue : white_02);
        GLGizmos.DrawSolidBoxEdgeRadius(adsr_increment_pos, adsr_increment_visual_size, edgeRadius, 0, true, BorderType.Inside).SetColor(ADSR_IncrementButton.Pressed ? blue : white_02);
        GLGizmos.SetColor(white_5);
        GLGizmos.DrawOpenBoxEdgeRadius(adsr_decrement_pos, adsr_increment_visual_size, edgeRadius, 0, false, BorderType.Inside).SetColor(ADSR_DecrementButton.Pressed ? blue_c : white_5);
        GLGizmos.DrawOpenBoxEdgeRadius(adsr_increment_pos, adsr_increment_visual_size, edgeRadius, 0, false, BorderType.Inside).SetColor(ADSR_IncrementButton.Pressed ? blue_c : white_5);
        GLGizmos.SetColor(white_8);
        GLGizmos.DrawText("-", adsr_decrement_pos + textOffset, font, 7, new() { fontStyle = FontStyles.Bold }).SetColor(ADSR_DecrementButton.Pressed ? white : white_8);
        GLGizmos.DrawText("+", adsr_increment_pos + textOffset, font, 6, new() { fontStyle = FontStyles.Bold }).SetColor(ADSR_IncrementButton.Pressed ? white : white_8);

        //GLGizmos.SetColor(white_8);
        //Vector2 adsr_arrow_size = new Vector2(1.2f, .08f);
        //float adsr_arrow_head_height = .15f;
        //GLGizmos.DrawSolidCapsule(adsr_decrement_pos, adsr_arrow_size, CapsuleDirection2D.Horizontal);
        //GLGizmos.DrawSolidCapsule(adsr_decrement_pos - Vector2.right * adsr_arrow_size.x * .5f, adsr_decrement_pos - Vector2.right * adsr_arrow_size.x * .3f + Vector2.up * adsr_arrow_head_height, .04f);
        //GLGizmos.DrawSolidCapsule(adsr_decrement_pos - Vector2.right * adsr_arrow_size.x * .5f, adsr_decrement_pos - Vector2.right * adsr_arrow_size.x * .3f - Vector2.up * adsr_arrow_head_height, .04f);
        //GLGizmos.DrawSolidCapsule(adsr_increment_pos, adsr_arrow_size, CapsuleDirection2D.Horizontal);
        //GLGizmos.DrawSolidCapsule(adsr_increment_pos + Vector2.right * adsr_arrow_size.x * .5f, adsr_increment_pos + Vector2.right * adsr_arrow_size.x * .3f + Vector2.up * adsr_arrow_head_height, .04f);
        //GLGizmos.DrawSolidCapsule(adsr_increment_pos + Vector2.right * adsr_arrow_size.x * .5f, adsr_increment_pos + Vector2.right * adsr_arrow_size.x * .3f - Vector2.up * adsr_arrow_head_height, .04f);


        // ----------------------------------------- Edo & Period
        Vector2 edo_period_swipe_size = new Vector2(1.5f, 1.5f);
        Vector2 edo_swipe_pos = new Vector2(-.75f, 3.5f);
        Vector2 n_swipe_pos = edo_swipe_pos + Vector2.right * edo_period_swipe_size.x;
        Vector2 d_swipe_pos = n_swipe_pos + Vector2.right * edo_period_swipe_size.x;
        if (!initButtons)
        {
            edoSwipeButton.SetPositionAndSize(edo_swipe_pos, edo_period_swipe_size);
            period_N_SwipeButton.SetPositionAndSize(n_swipe_pos, edo_period_swipe_size);
            period_D_SwipeButton.SetPositionAndSize(d_swipe_pos, edo_period_swipe_size);
        }
        TextBoxParams edo_swipe_tbp = new TextBoxParams()
        {
            fontStyle = FontStyles.Bold,
            textBoxSize = edo_period_swipe_size,
            fitTextToBox = true,
        };
        TextBoxParams slash_swipe_tbp = new TextBoxParams()
        {
            fontStyle = FontStyles.Italic,
            textBoxSize = edo_period_swipe_size,
            fitTextToBox = true,
        };

        Vector2 period_text_size = new Vector2(1.5f, .75f);
        TextBoxParams n_swipe_tbp = new TextBoxParams()
        {
            fontStyle = FontStyles.Bold,
            textBoxSize = period_text_size,
            fitTextToBox = true,
            alignment = TextAlignmentOptions.BottomRight,
            positionPivot = PositionPivot.BottomRight,
        };
        TextBoxParams d_swipe_tbp = new TextBoxParams()
        {
            fontStyle = FontStyles.Bold,
            textBoxSize = period_text_size,
            fitTextToBox = true,
            alignment = TextAlignmentOptions.TopLeft,
            positionPivot = PositionPivot.TopLeft,
        };

        TextBoxParams header_tbp = new TextBoxParams()
        {
            fontStyle = FontStyles.Underline,
            alignment = TextAlignmentOptions.Bottom,
        };

        Vector2 header_offset_y = Vector2.up * .8f;
        Vector2 slash_pos = n_swipe_pos + Vector2.right * edo_period_swipe_size.x * .5f;
        GLGizmos.SetColor(white_8);
        GLGizmos.DrawText($"divisions", edo_swipe_pos + header_offset_y, font, 2.2f, header_tbp);
        GLGizmos.DrawText($"period", slash_pos + header_offset_y, font, 2.2f, header_tbp);

        GLGizmos.DrawText("/", slash_pos, font2, 16, slash_swipe_tbp).SetColor(white_3); ;
        GLGizmos.DrawText($"{edo}", edo_swipe_pos, font2, 10, edo_swipe_tbp).SetColor(edoSwipeButton.Pressed ? Color.yellow : Color.white);

        Vector2 text_offset_y = Vector2.up * .1f;
        Vector2 text_offset_x = Vector2.right * .2f;
        GLGizmos.DrawText($"{periodN}", slash_pos - text_offset_y - text_offset_x, font2, 10, n_swipe_tbp).SetColor(period_N_SwipeButton.Pressed ? Color.yellow : white_3);
        GLGizmos.DrawText($"{periodD}", slash_pos + text_offset_y + text_offset_x, font2, 10, d_swipe_tbp).SetColor(period_D_SwipeButton.Pressed ? Color.yellow : white_3);


        // ----------------------------------------- Edo Display
        // edo
        GLGizmos.SetColor(Color.white);
        TextBoxParams edo_tbp = new TextBoxParams()
        {
            alignment = TextAlignmentOptions.Right,
            positionPivot = PositionPivot.Right
        };
        Vector2 edo_pos_right = new Vector2(uiRight - padding - 1.2f, 4f);
        GLGizmos.DrawText($"{edo}ed", edo_pos_right, font, 8, edo_tbp);

        // period
        float period = Period;
        float periodFontSize = 5;
        string periodValue = $"{Mathf.RoundToInt(period)}";

        if (period != Mathf.RoundToInt(period))
        {
            int GCD(int a, int b)
            {
                while (b != 0)
                {
                    int temp = b;
                    b = a % b;
                    a = temp;
                }

                return Math.Abs(a);
            }

            int gcd = GCD(periodN, periodD);
            periodFontSize = 3;
            periodValue = $"{periodN / gcd}\n—\n{periodD / gcd}";
        }
        //(string periodValue, float periodFontSize) = period == Mathf.RoundToInt(period) ? ($"{Mathf.RoundToInt(period)}", 5) : ($"{periodN}\n—\n{periodD}", 3);
        TextBoxParams period_tbp = new TextBoxParams()
        {
            lineSpacing = -28,
            fitTextToBox = true,
            textBoxSize = new Vector2(.5f, 1),
        };
        Vector2 period_pos = new Vector2(uiRight - padding - .7f, 4f);
        GLGizmos.DrawText(periodValue, period_pos, font, periodFontSize, period_tbp);

        // parentheses
        TextBoxParams period_parentheses_tbp = new TextBoxParams()
        {
            characterSpacing = 20
        };
        GLGizmos.DrawText($"()", period_pos, font, 9, period_parentheses_tbp);

        // cents
        Vector2 cents_pos = new Vector2(uiRight - padding - .7f, 3.4f);
        if (keyboardMode == KeyboardMode.Logarithmic)
            GLGizmos.DrawText($"{Mathf.Round(CentsPerNote * 10) / 10}c", cents_pos, font, 2.5f);


        //  ----------------------------------------- LogType Button
        Vector2 logtype_size = new Vector2(1.6f, .4f);
        Vector2 logtype_pos = GLGizmos.GetBoxPositionByPivot(new Vector2(uiRight - padding - 1.24f, 3.4f), logtype_size, 0, PositionPivot.Right);
        if (!initButtons)
        {
            KeyboardModeButton.SetPositionAndSize(logtype_pos, logtype_size);
        }

        // visual
        Vector2 logtype_visual_size = new Vector2(1.5f, .35f);
        TextBoxParams logtype_tbp = new TextBoxParams()
        {
            textBoxSize = logtype_visual_size,
            //fontStyle = FontStyles.Bold,
            characterSpacing = -5,
        };
        GLGizmos.DrawSolidCapsule(logtype_pos, logtype_visual_size, CapsuleDirection2D.Horizontal, 0).SetColor(KeyboardModeButton.Pressed ? blue : white_02);
        GLGizmos.DrawOpenCapsule(logtype_pos, logtype_visual_size, CapsuleDirection2D.Horizontal, 0).SetColor(KeyboardModeButton.Pressed ? blue_c : white_5);
        GLGizmos.DrawText(keyboardMode == KeyboardMode.Logarithmic ? "Logarithmic" : "Linear", logtype_pos, font, 2.2f, logtype_tbp).SetColor(KeyboardModeButton.Pressed ? white : white_5);


        //  ----------------------------------------- Reset Button
        float reset_visual_radius = .2f;
        Vector2 reset_size = new Vector2(.6f, .6f);
        Vector2 reset_pos = GLGizmos.GetBoxPositionByPivot(new Vector2(uiRight - padding - 3.2f, 4f), reset_size, 0, PositionPivot.Right);
        if (!initButtons)
        {
            ResetEdoButton.SetPositionAndSize(reset_pos, reset_size);
        }
        TextBoxParams reset_tbp = new TextBoxParams()
        {
            fontStyle = FontStyles.Bold,
        };
        GLGizmos.DrawSolidCircle(reset_pos, reset_visual_radius, -2).SetColor(ResetEdoButton.Pressed ? blue : white_02);
        GLGizmos.DrawOpenCircle(reset_pos, reset_visual_radius, -2).SetColor(ResetEdoButton.Pressed ? blue_c : white_3);
        GLGizmos.DrawText("C", reset_pos, font, 2, reset_tbp).SetColor(ResetEdoButton.Pressed ? white : white_3);


        //  ----------------------------------------- Shift Buttons
        Vector2 shiftButtonSize = new Vector2(1.8f, .8f);
        Vector2 shiftRightPos = GLGizmos.GetBoxPositionByPivot(new Vector2(uiRight, uiBottom), shiftButtonSize, 0, PositionPivot.BottomRight);
        Vector2 shiftLeftPos = shiftRightPos - Vector2.right * shiftButtonSize.x;

        Vector2 shiftButtonVisualSize = new Vector2(shiftButtonSize.x, .7f);
        Vector2 shiftRightVisualPos = GLGizmos.GetBoxPositionByPivot(new Vector2(uiRight, uiBottom), shiftButtonVisualSize, 0, PositionPivot.BottomRight);
        Vector2 shiftLeftVisualPos = shiftRightVisualPos - Vector2.right * shiftButtonVisualSize.x;
        GLGizmos.SetColor(white_02);
        GLGizmos.DrawSolidBoxEdgeRadius(shiftRightVisualPos, shiftButtonVisualSize, edgeRadius, 0, true, BorderType.Inside).SetColor(shiftKeyboardIncrementButton.Pressed ? blue : white_02);
        GLGizmos.DrawSolidBoxEdgeRadius(shiftLeftVisualPos, shiftButtonVisualSize, edgeRadius, 0, true, BorderType.Inside).SetColor(shiftKeyboardDecrementButton.Pressed ? blue : white_02);
        GLGizmos.SetColor(white_5);
        GLGizmos.DrawOpenBoxEdgeRadius(shiftRightVisualPos, shiftButtonVisualSize, edgeRadius, 0, false, BorderType.Inside).SetColor(shiftKeyboardIncrementButton.Pressed ? blue_c : white_5);
        GLGizmos.DrawOpenBoxEdgeRadius(shiftLeftVisualPos, shiftButtonVisualSize, edgeRadius, 0, false, BorderType.Inside).SetColor(shiftKeyboardDecrementButton.Pressed ? blue_c : white_5);

        Vector2 shift_arrow_size = new Vector2(1.2f, .08f);
        float shift_arrow_head_height = .15f;
        GLGizmos.SetColor(shiftKeyboardDecrementButton.Pressed ? white : white_8);
        GLGizmos.DrawSolidCapsule(shiftLeftVisualPos, shift_arrow_size, CapsuleDirection2D.Horizontal);
        GLGizmos.DrawSolidCapsule(shiftLeftVisualPos - Vector2.right * shift_arrow_size.x * .5f, shiftLeftVisualPos - Vector2.right * shift_arrow_size.x * .3f + Vector2.up * shift_arrow_head_height, .04f);
        GLGizmos.DrawSolidCapsule(shiftLeftVisualPos - Vector2.right * shift_arrow_size.x * .5f, shiftLeftVisualPos - Vector2.right * shift_arrow_size.x * .3f - Vector2.up * shift_arrow_head_height, .04f);
        GLGizmos.SetColor(shiftKeyboardIncrementButton.Pressed ? white : white_8);
        GLGizmos.DrawSolidCapsule(shiftRightVisualPos, shift_arrow_size, CapsuleDirection2D.Horizontal);
        GLGizmos.DrawSolidCapsule(shiftRightVisualPos + Vector2.right * shift_arrow_size.x * .5f, shiftRightVisualPos + Vector2.right * shift_arrow_size.x * .3f + Vector2.up * shift_arrow_head_height, .04f);
        GLGizmos.DrawSolidCapsule(shiftRightVisualPos + Vector2.right * shift_arrow_size.x * .5f, shiftRightVisualPos + Vector2.right * shift_arrow_size.x * .3f - Vector2.up * shift_arrow_head_height, .04f);


        //  ----------------------------------------- Extend Buttons
        float extendButtonPadding = .1f;
        Vector2 extendButtonSize = new Vector2(.8f, .8f);
        Vector2 extendIncrementPos = GLGizmos.GetBoxPositionByPivot(new Vector2(uiRight - shiftButtonSize.x * 2 - extendButtonPadding - extendButtonSize.x * .5f, uiBottom), extendButtonSize, 0, PositionPivot.Bottom);
        Vector2 extendDecrementPos = extendIncrementPos - Vector2.right * extendButtonSize.x;

        Vector2 extendButtonVisualSize = new Vector2(.7f, .7f);
        Vector2 extendIncrementVisualPos = GLGizmos.GetBoxPositionByPivot(extendIncrementPos - Vector2.up * extendButtonSize.y * .5f, extendButtonVisualSize, 0, PositionPivot.Bottom);
        Vector2 extendDecrementVisualPos = GLGizmos.GetBoxPositionByPivot(extendDecrementPos - Vector2.up * extendButtonSize.y * .5f, extendButtonVisualSize, 0, PositionPivot.Bottom);

        GLGizmos.SetColor(white_02);
        GLGizmos.DrawSolidBoxEdgeRadius(extendIncrementVisualPos, extendButtonVisualSize, edgeRadius, 0, true, BorderType.Inside).SetColor(extendKeyboardIncrementButton.Pressed ? blue : white_02);
        GLGizmos.DrawSolidBoxEdgeRadius(extendDecrementVisualPos, extendButtonVisualSize, edgeRadius, 0, true, BorderType.Inside).SetColor(extendKeyboardDecrementButton.Pressed ? blue : white_02);
        GLGizmos.SetColor(white_5);
        GLGizmos.DrawOpenBoxEdgeRadius(extendIncrementVisualPos, extendButtonVisualSize, edgeRadius, 0, false, BorderType.Inside).SetColor(extendKeyboardIncrementButton.Pressed ? blue_c : white_5);
        GLGizmos.DrawOpenBoxEdgeRadius(extendDecrementVisualPos, extendButtonVisualSize, edgeRadius, 0, false, BorderType.Inside).SetColor(extendKeyboardDecrementButton.Pressed ? blue_c : white_5);
        GLGizmos.SetColor(white_8);
        GLGizmos.DrawText("+", extendIncrementVisualPos + textOffset, font, 6, new() { fontStyle = FontStyles.Bold }).SetColor(extendKeyboardIncrementButton.Pressed ? white : white_8);
        GLGizmos.DrawText("-", extendDecrementVisualPos + textOffset, font, 7, new() { fontStyle = FontStyles.Bold }).SetColor(extendKeyboardDecrementButton.Pressed ? white : white_8);
        //GLGizmos.DrawSolidPlus(extendIncrementVisualPos, Vector2.one * .5f, .1f, 0);
        //GLGizmos.DrawSolidBox(extendDecrementVisualPos, new Vector2(.5f, .1f));

        if (!initButtons)
        {
            shiftKeyboardIncrementButton.SetPositionAndSize(shiftRightPos, shiftButtonSize);
            shiftKeyboardDecrementButton.SetPositionAndSize(shiftLeftPos, shiftButtonSize);

            extendKeyboardIncrementButton.SetPositionAndSize(extendIncrementPos, extendButtonSize);
            extendKeyboardDecrementButton.SetPositionAndSize(extendDecrementPos, extendButtonSize);
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
