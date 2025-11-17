using Unity.Mathematics;
using UnityEngine;
using static Unity.Burst.Intrinsics.Arm;

public class Note
{
    public float phase;
    public float frequency;
    public ADSR adsr;
    public Synth synth;

    public double refTime = AudioSettings.dspTime;
    public float lastEnvelopeValue;
    public bool on = true;

    public Note(Octave octave, int index, int edo, Synth synth, ADSR adsr = null)
    {
        if (edo < 1)
            edo = 1;

        phase = 0;
        frequency = SynthPlayer.baseFrequency * Mathf.Pow(octave.root, octave.value) * Mathf.Pow(Mathf.Pow(octave.scale, 1 / (float)edo), (float)index);
        this.adsr = adsr;
        this.synth = synth;
    }

    public Note(Octave octave, float ratio, Synth synth, ADSR adsr = null)
    {
        if (ratio < 0)
            ratio = 1;

        phase = 0;
        frequency = SynthPlayer.baseFrequency * Mathf.Pow(octave.root, octave.value) * ratio;
        this.adsr = adsr;
        this.synth = synth;
    }

    public Note(float freq, Synth synth, ADSR adsr = null)
    {
        phase = 0;
        frequency = freq;
        this.adsr = adsr;
        this.synth = synth;
    }

    public void TurnOn()
    {
        refTime = AudioSettings.dspTime;
        on = true;
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

    public void AddCents(float cents)
    {
        if (cents != 0)
            frequency *= Mathf.Pow(2, 1 / (1200 / (float)cents));
    }
    
    public ADSR GetADSR() => adsr ?? synth.adsr;
}

public static class NoteExtensions
{
    public static Note On(this Note note)
    {
        note.TurnOn();
        return note;
    }

    public static Note Off(this Note note)
    {
        note.TurnOff();
        return note;
    }

    public static Note Synth(this Note note, Synth synth)
    {
        note.synth = synth;
        return note;
    }

}
