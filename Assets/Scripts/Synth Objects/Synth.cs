using UnityEngine;

public class Synth
{
    public enum Waveform
    {
        Sine,
        Square,
        Triangle,
        Saw
    }
    protected Waveform waveform;

    public Synth(Waveform baseWaveform)
    {
        this.waveform = baseWaveform;
    }

    public float GetWaveformValue(float phase)
    {
        return waveform switch
        {
            Waveform.Sine => Sine(phase),
            Waveform.Square => Square(phase),
            Waveform.Triangle => Triangle(phase),
            Waveform.Saw => Saw(phase),
        };
    }

    float Sine(float phase) => Mathf.Sin(2 * Mathf.PI * phase);
    float Square(float phase) => phase >= .5f ? 1 : 0;
    float Triangle(float phase) => Mathf.Abs(phase * 4.0f - 2.0f) - 1.0f;
    float Saw(float phase) => phase * 2 - 1;
}
