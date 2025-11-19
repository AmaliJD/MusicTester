using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using System;

public class Note
{
    public float frequency;
    public List<float> phases = new(1);
    public ADSR adsr;
    public Synth synth;

    public double refTime = AudioSettings.dspTime;
    public float lastEnvelopeValue;
    public bool on = true;

    public Note(Octave octave, int index, int edo, Synth synth, ADSR adsr = null)
    {
        if (edo < 1)
            edo = 1;

        phases = new List<float>(new float[synth.voiceCount]).Randomize();
        frequency = SynthPlayer.baseFrequency * Mathf.Pow(octave.root, octave.value) * Mathf.Pow(Mathf.Pow(octave.scale, 1 / (float)edo), (float)index);
        this.adsr = adsr;
        this.synth = synth;
    }

    public Note(Octave octave, float ratio, Synth synth, ADSR adsr = null)
    {
        if (ratio < 0)
            ratio = 1;

        phases = new List<float>(new float[synth.voiceCount]).Randomize();
        frequency = SynthPlayer.baseFrequency * Mathf.Pow(octave.root, octave.value) * ratio;
        this.adsr = adsr;
        this.synth = synth;
    }

    public Note(float freq, Synth synth, ADSR adsr = null)
    {
        phases = new List<float>(new float[synth.voiceCount]).Randomize();
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
        if (phases.Count == 1)
        {
            phases[0] += frequency.AddOctaves(synth.octaveShift) / SynthPlayer.sampleRate;
            phases[0] = Mathf.Repeat(phases[0], 1);
        }
        else
        {
            float detune = synth.detune;
            float detuneStep = (detune * 2) / (phases.Count - 1);
            for (int i = 0; i < phases.Count; i++)
            {
                phases[i] += frequency.AddCents(-detune + detuneStep * i).AddOctaves(synth.octaveShift) / SynthPlayer.sampleRate;
                phases[i] = Mathf.Repeat(phases[i], 1);
            }
        }
    }

    public float GetValue()
    {
        float value = 0;
        foreach (float phase in phases)
        {
            value += synth.GetWaveformValue(phase);
        }
        value /= Mathf.Sqrt(phases.Count);

        return value;
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

    public static float AddCents(this float frequency, float cents)
    {
        if (cents != 0)
            return frequency * Mathf.Pow(2, 1 / (1200 / (float)cents));
        else
            return frequency;
    }

    public static float AddOctaves(this float frequency, int octaves)
    {
        return frequency * Mathf.Pow(2, octaves);
    }

    public static List<float> Randomize(this List<float> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i] = UnityEngine.Random.Range(0f, 1f);
        }
        return list;
    }
}
