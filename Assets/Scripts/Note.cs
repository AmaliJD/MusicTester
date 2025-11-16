using Unity.Mathematics;
using UnityEngine;

public class Note
{
    public float phase;
    public float frequency;
    public float attack = 0, decay = 0, sustain = 1, release = 0, velocity = 1;
    public float lastEnvelopeValue;
    public bool on = true;
    public double refTime = AudioSettings.dspTime;

    public Note(uint octave, int index, int edo, float? a = null, float? d = null, float? s = null, float? r = null, float? v = null, float octaveRatio = 2, float? octaveStartRatio = null)
    {
        if (edo < 1)
            edo = 1;

        if (octaveRatio < 1)
            octaveRatio = 1;

        phase = 0;
        frequency = SynthPlayer.baseFrequency * Mathf.Pow(octaveStartRatio ?? octaveRatio, octave) * Mathf.Pow(Mathf.Pow(octaveRatio, 1 / (float)edo), (float)index);

        SetASDRV(a, d, s, r, v);
    }

    public Note(uint octave, float ratio, float? a = null, float? d = null, float? s = null, float? r = null, float? v = null, float octaveRatio = 2, float? octaveStartRatio = null)
    {
        if (ratio < 0)
            ratio = 1;

        phase = 0;
        frequency = SynthPlayer.baseFrequency * Mathf.Pow(octaveStartRatio ?? octaveRatio, octave) * ratio;

        SetASDRV(a, d, s, r, v);
    }

    public Note(uint octave, int cents, float? a = null, float? d = null, float? s = null, float? r = null, float? v = null, float octaveRatio = 2, float? octaveStartRatio = null)
    {
        phase = 0;

        if (cents != 0)
            frequency = SynthPlayer.baseFrequency * Mathf.Pow(octaveStartRatio ?? octaveRatio, octave) * Mathf.Pow(2, 1 / (1200 / (float)cents));
        else
            frequency = SynthPlayer.baseFrequency * Mathf.Pow(octaveStartRatio ?? octaveRatio, octave);

        SetASDRV(a, d, s, r, v);
    }

    public Note(float freq, float? a = null, float? d = null, float? s = null, float? r = null, float? v = null)
    {
        phase = 0;
        frequency = freq;

        SetASDRV(a, d, s, r, v);
    }

    public void SetASDRV(float? a = null, float? d = null, float? s = null, float? r = null, float? v = null)
    {
        attack = a ?? attack;
        decay = d ?? decay;
        sustain = s ?? v ?? sustain;
        release = r ?? release;
        velocity = v ?? s ?? velocity;
    }

    public void TurnOff()
    {
        refTime = AudioSettings.dspTime;
        on = false;
    }

    public void UpdatePhase()
    {
        phase += frequency / SynthPlayer.sampleRate;
        phase = Mathf.Repeat(phase, 1);
    }

}