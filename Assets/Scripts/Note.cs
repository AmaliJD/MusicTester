using UnityEngine;

public class Note
{
    public float phase;
    public float frequency;
    public float attack = 0, decay = 0, sustain = 1, release = 0, velocity = 1;
    public bool on = true;
    public double refTime = AudioSettings.dspTime;

    public Note(int octave, int index, int edo, float? a = null, float? d = null, float? s = null, float? r = null, float? v = null)
    {
        if (edo < 1)
            edo = 1;

        if (octave < 0)
            octave = 0;

        phase = 0;
        frequency = (SynthPlayer.baseFrequency * ((float)octave + 1)) * Mathf.Pow(Mathf.Pow(2, 1 / (float)edo), (float)index);

        SetASDRV(a, d, s, r, v);
    }

    public Note(int octave, float ratio, float? a = null, float? d = null, float? s = null, float? r = null, float? v = null)
    {
        if (octave < 0)
            octave = 0;

        if (ratio < 0)
            ratio = 1;

        phase = 0;
        frequency = (SynthPlayer.baseFrequency * (float)octave + 1) * ratio;

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

    public void UpdatePhase()
    {
        phase += frequency / SynthPlayer.sampleRate;
        phase = Mathf.Repeat(phase, 1);
    }

}