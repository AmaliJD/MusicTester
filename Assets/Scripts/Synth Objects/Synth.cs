using System.Collections.Generic;
using UnityEngine;

public class Synth
{
    public enum Waveform
    {
        Sine,
        Triangle,
        Square,
        Saw,
        White
    }
    protected Waveform waveform;
    public ADSR adsr;
    public int voiceCount;
    public float detune;
    public int octaveShift;
    public float noise;
    public SortedList<double, float> frequencyMultipliers = new();
    AltRandom rand = new();

    public Synth(Waveform shape, ADSR adsr = null, int octaveShift = 0, int unison = 1, float detune = 12, float noise = 0)
    {
        this.waveform = shape;
        this.adsr = adsr ?? new();
        this.voiceCount = Mathf.Max(unison, 1);
        this.detune = Mathf.Max(detune, 0);
        this.octaveShift = octaveShift;
        this.noise = noise;
    }

    public float GetWaveformValue(float phase)
    {
        float value = waveform switch
        {
            Waveform.Sine => Sine(phase),
            Waveform.Square => Square(phase) * .5f,
            Waveform.Triangle => Triangle(phase),
            Waveform.Saw => Saw(phase),
            Waveform.White => rand.Roll(),
            _ => Test(phase),
        };

        // added noise
        if (noise > 0)
            value = Mathf.Lerp(value, rand.Roll(), noise);

        return value;
    }

    public void IncrementWaveformSkipNoise()
    {
        waveform++;
        if (waveform == Waveform.White)
            waveform = Waveform.Sine;
    }

    public Waveform GetWaveform() => waveform;

    float Sine(float phase) => Mathf.Sin(2 * Mathf.PI * phase);
    float Triangle(float phase) => Mathf.Abs(phase * 4.0f - 2.0f) - 1.0f;
    float Step(float phase) => phase >= .5f ? 1 : 0; // same as square but half amplitude
    float Square(float phase) => phase >= .5f ? 1 : -1;
    float Saw(float phase) => phase * 2 - 1;

    // TEST
    float Test(float phase)
    {
        return 0;
    }
}
