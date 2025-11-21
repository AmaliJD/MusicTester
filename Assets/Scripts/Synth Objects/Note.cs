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
    public SortedList<double, float> frequencyMultipliers = new();

    public double refTime = AudioSettings.dspTime;
    public double startTime = AudioSettings.dspTime;
    public double duration;
    public float lastEnvelopeValue;
    public bool on = true;

    public Note(Octave octave, int index, int edo, float duration, Synth synth, ADSR adsr = null)
    {
        if (edo < 1)
            edo = 1;

        phases = new List<float>(new float[synth.voiceCount]).Randomize();
        frequency = SynthPlayer.baseFrequency * Mathf.Pow(octave.root, octave.value) * Mathf.Pow(Mathf.Pow(octave.scale, 1 / (float)edo), (float)index);
        this.adsr = adsr;
        this.synth = synth;
        this.duration = duration;
        InitFrequencyMultipliers(duration);
    }

    public Note(Octave octave, float ratio, float duration, Synth synth, ADSR adsr = null)
    {
        if (ratio < 0)
            ratio = 1;

        phases = new List<float>(new float[synth.voiceCount]).Randomize();
        frequency = SynthPlayer.baseFrequency * Mathf.Pow(octave.root, octave.value) * ratio;
        this.adsr = adsr;
        this.synth = synth;
        this.duration = duration;
        InitFrequencyMultipliers(duration);
    }

    public Note(float freq, float duration, Synth synth, ADSR adsr = null)
    {
        phases = new List<float>(new float[synth.voiceCount]).Randomize();
        frequency = freq;
        this.adsr = adsr;
        this.synth = synth;
        this.duration = duration;
        InitFrequencyMultipliers(duration);
    }

    public void TurnOn()
    {
        refTime = AudioSettings.dspTime;
        startTime = AudioSettings.dspTime;
        on = true;
    }

    public void TurnOff()
    {
        refTime = AudioSettings.dspTime;
        on = false;
    }

    public void UpdatePhase()
    {
        float adjFrequency = GetAdjustedFrequency();
        
        if (phases.Count == 1)
        {
            phases[0] += adjFrequency.AddOctaves(synth.octaveShift) / SynthPlayer.sampleRate;
            phases[0] = Mathf.Repeat(phases[0], 1);
        }
        else
        {
            float detune = synth.detune;
            float detuneStep = (detune * 2) / (phases.Count - 1);
            for (int i = 0; i < phases.Count; i++)
            {
                phases[i] += adjFrequency.AddCents(-detune + detuneStep * i).AddOctaves(synth.octaveShift) / SynthPlayer.sampleRate;
                phases[i] = Mathf.Repeat(phases[i], 1);
            }
        }
    }

    float GetAdjustedFrequency()
    {
        float adjFrequency = frequency;
        double time = AudioSettings.dspTime;
        double timeSinceStart = time - startTime;

        if (frequencyMultipliers.Count > 0)
        {
            if (timeSinceStart >= frequencyMultipliers.Keys[frequencyMultipliers.Count - 1])
            {
                adjFrequency = frequency * frequencyMultipliers.Values[frequencyMultipliers.Count - 1];
            }
            else
            {
                int fmEndIndex = 0;

                foreach (var kv in frequencyMultipliers)
                {
                    if (kv.Key >= timeSinceStart)
                        break;

                    fmEndIndex++;
                }

                (double timeStart, float valueStart) = fmEndIndex == 0 ? (0, 1) : (frequencyMultipliers.Keys[fmEndIndex - 1], frequencyMultipliers.Values[fmEndIndex - 1]);
                (double timeEnd, float valueEnd) = (frequencyMultipliers.Keys[fmEndIndex], frequencyMultipliers.Values[fmEndIndex]);
                float t = (float)MathF.InverseLerpClamped(timeStart, timeEnd, timeSinceStart);
                //adjFrequency = frequency * Mathf.Lerp(valueStart, valueEnd, Mathf.InverseLerp(timeStart, timeEnd, (float)timeSinceStart));
                adjFrequency = frequency * (valueStart * Mathf.Pow(valueEnd / valueStart, t));
            }
        }

        return adjFrequency;
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
        frequency *= Mathf.Pow(2, cents / 1200);
    }
    
    public ADSR GetADSR() => adsr ?? synth.adsr;

    void InitFrequencyMultipliers(float duration)
    {
        frequencyMultipliers.Clear();

        frequencyMultipliers.Add(0, 1);

        if (duration > 0)
            frequencyMultipliers.Add(duration, 1);
    }

    public void AddFrequencyMultiplier(float time, float mult)
    {
        if (frequencyMultipliers.ContainsKey(time))
            frequencyMultipliers[time] = mult;
        else
            frequencyMultipliers.Add(time, mult);
    }
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
        return frequency * Mathf.Pow(2, cents / 1200);
    }

    public static float AddOctaves(this float frequency, int octaves)
    {
        return frequency * Mathf.Pow(2, octaves);
    }

    public static Note AddFrequencyMultipliers(this Note note, List<Vector2> timeAndMultList)
    {
        foreach (Vector2 tm in timeAndMultList)
            note.AddFrequencyMultiplier(tm.x, tm.y);
        return note;
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
