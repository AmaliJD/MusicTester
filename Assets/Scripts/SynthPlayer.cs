using GLG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(AudioSource))]
public class SynthPlayer : MonoBehaviour
{
    private AudioSource audioSource;

    public static float sampleRate = 0;
    public static float baseFrequency = 220;
    private double time;
    int edo = 1;
    List<KeyControl> keys = new();

    Dictionary<int, Note> notes = new();
    List<int> offNoteIDs = new();

    List<Synth> synths = new();
    int synthIndex;
    ADSR adsr = new(.05f, .5f, .3f, .5f, 1f);

    // test variables
    float maxValue = 0;
    Note freeNote;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        sampleRate = AudioSettings.outputSampleRate;
        synths.Add(new(Synth.Waveform.Saw, adsr));
        synths.Add(new(Synth.Waveform.Saw, adsr.Clone(release: .8f), -1, 5, 8));
        synths.Add(new(Synth.Waveform.Square, adsr.Clone(attack: .2f)));
        synths.Add(new(Synth.Waveform.Triangle, adsr.Clone(release: 0)));
        synths.Add(new(Synth.Waveform.Sine, adsr.Clone(attack: .05f, decay: 1, release: 2f, sustain: 1, velocity: .2f)));
        freeNote = new(baseFrequency, synths[synthIndex]);

        keys = new()
        {
            Keyboard.current.escapeKey,
            Keyboard.current.f1Key,
            Keyboard.current.f2Key,
            Keyboard.current.f3Key,
            Keyboard.current.f4Key,
            Keyboard.current.f5Key,
            Keyboard.current.f6Key,
            Keyboard.current.f7Key,
            Keyboard.current.f8Key,
            Keyboard.current.f9Key,
            Keyboard.current.f10Key,
            Keyboard.current.f11Key,
            Keyboard.current.f12Key,
            //Keyboard.current.digit1Key,
            //Keyboard.current.digit2Key,
            //Keyboard.current.digit3Key,
            //Keyboard.current.digit4Key,
            //Keyboard.current.digit5Key,
            //Keyboard.current.digit6Key,
            //Keyboard.current.digit7Key,
            //Keyboard.current.digit8Key,
            //Keyboard.current.digit9Key,
            //Keyboard.current.digit0Key,
            //Keyboard.current.minusKey,
            //Keyboard.current.equalsKey,
        };
        edo = keys.Count - 1;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        time = AudioSettings.dspTime;
        for (int i = 0; i < data.Length; i += channels)
        {
            float value = CombineNotes();

            for (int c = 0; c < channels; c++)
                data[i + c] = value;

            maxValue = Mathf.Max(maxValue, value);

            foreach (var note in notes)
            {
                note.Value.UpdatePhase();
            }
        }
    }

    float CombineNotes()
    {
        float value = 0;
        float gain = .15f;
        double k = -3;
        foreach (var entry in notes)
        {
            Note note = entry.Value;
            Synth synth = note.synth;
            ADSR adsr = note.GetADSR();
            float envelope = adsr.sustain;

            if (note.on)
            {
                if (time - note.refTime < adsr.attack)
                {
                    envelope = (float)MathF.InverseLerpClamped(note.refTime, note.refTime + adsr.attack, time) * adsr.velocity;
                }
                else if (time - note.refTime < adsr.attack + adsr.decay)
                {
                    double t = MathF.InverseLerpClamped(note.refTime + adsr.attack, note.refTime + adsr.attack + adsr.decay, time);
                    double expT = Math.Exp(k * t);
                    envelope = (float)MathF.Lerp(adsr.sustain, adsr.velocity, expT);
                }

                note.lastEnvelopeValue = envelope;
            }
            else
            {
                if (time - note.refTime < adsr.release)
                {
                    double t = MathF.InverseLerpClamped(note.refTime, note.refTime + adsr.release, time);
                    double expT = Math.Exp(k * t);
                    envelope = (float)MathF.Lerp(0, note.lastEnvelopeValue, expT);
                    //envelope = (1 - (float)MathF.InverseLerpClamped(note.refTime, note.refTime + note.release, time)) * note.lastEnvelopeValue;
                }
                else
                {
                    envelope = 0;
                }
            }

            value += note.GetValue() * envelope * gain;
        }

        return value;
    }

    private void Update()
    {
        time = AudioSettings.dspTime;
        //Debug.Log($"Audio Value: {maxValue}");

        // change synth index
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            synthIndex = (synthIndex + 1) % synths.Count;
            freeNote = new(baseFrequency, synths[synthIndex]);
        }

        // keyboard
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].wasPressedThisFrame)
            {
                AddNote(i, new Note(/*(0, 1.67f, 2)*/0, i, edo, synths[synthIndex]).On());
                Debug.Log($"{notes[i].frequency} Hz");
            }
            else if (keys[i].wasReleasedThisFrame)
            {
                ReleaseNote(i);
            }
        }

        // free note
        float mouseScrollY = Mouse.current.scroll.value.y;
        if (mouseScrollY != 0)
            freeNote.AddCents(mouseScrollY);

        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            AddNote(-1, freeNote.Synth(synths[synthIndex]).On());
            Debug.Log($"{notes[-1].frequency} Hz");
        }
        else if (Keyboard.current.backquoteKey.wasReleasedThisFrame)
        {
            ReleaseNote(-1);
        }

        // remove finished notes
        for (int i = 0; i < offNoteIDs.Count; i++)
        {
            if (time >= notes[offNoteIDs[i]].refTime + notes[offNoteIDs[i]].GetADSR().release)
            {
                RemoveNote(offNoteIDs[i]);
                i--;
            }
        }

        // enable/disable AudioSource
        if (notes.Count > 0 && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        else if (notes.Count == 0 && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void AddNote(int key, Note note)
    {
        if (notes.ContainsKey(key))
        {
            notes.Remove(key);

            if (offNoteIDs.Contains(key))
                offNoteIDs.Remove(key);
        }
        notes.Add(key, note);
    }

    void ReleaseNote(int key)
    {
        if (!notes.ContainsKey(key))
            return;

        notes[key].TurnOff();
        offNoteIDs.Add(key);
    }

    void RemoveNote(int key)
    {
        notes.Remove(key);
        offNoteIDs.Remove(key);
    }
}
