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
    public static float baseFrequency = 55;
    private double time;
    int edo = 1;
    float maxValue = 0;
    List<KeyControl> keys = new();

    Dictionary<int, Note> notes = new();
    List<int> offNoteIDs = new();

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        sampleRate = AudioSettings.outputSampleRate;

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
            //Keyboard.current.f12Key,
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
            //Debug.Log($"Audio Value: {maxValue}");

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
            float envelope = note.sustain;

            if (note.on)
            {
                if (time - note.refTime < note.attack)
                {
                    envelope = (float)MathF.InverseLerpClamped(note.refTime, note.refTime + note.attack, time) * note.velocity;
                }
                else if (time - note.refTime < note.attack + note.decay)
                {
                    double t = MathF.InverseLerpClamped(note.refTime + note.attack, note.refTime + note.attack + note.decay, time);
                    double expT = Math.Exp(k * t);
                    envelope = (float)MathF.Lerp(note.sustain, note.velocity, expT);
                }

                note.lastEnvelopeValue = envelope;
            }
            else
            {
                if (time - note.refTime < note.release)
                {
                    double t = MathF.InverseLerpClamped(note.refTime, note.refTime + note.release, time);
                    double expT = Math.Exp(k * t);
                    envelope = (float)MathF.Lerp(0, note.lastEnvelopeValue, expT);
                    //envelope = (1 - (float)MathF.InverseLerpClamped(note.refTime, note.refTime + note.release, time)) * note.lastEnvelopeValue;
                }
                else
                {
                    envelope = 0;
                }
            }

            value += Saw(note.phase) * envelope * gain;
        }

        return value;
    }

    private void Update()
    {
        time = AudioSettings.dspTime;

        // keyboard
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].wasPressedThisFrame)
            {
                AddNote(i);
                Debug.Log($"{notes[i].frequency} Hz");
            }
            else if (keys[i].wasReleasedThisFrame)
            {
                ReleaseNote(i);
            }
        }

        // mouse
        //float cents = 200 * Mathf.Round(Mathf.Clamp((Camera.main.ScreenToWorldPoint(Mouse.current.position.value).y + Camera.main.orthographicSize) / 10, 0, 1) * 24);

        //if (Mouse.current.leftButton.wasPressedThisFrame)
        //    AddNote(-1, (int)cents);
        //else if  (Mouse.current.leftButton.wasReleasedThisFrame)
        //    ReleaseNote(-1);

        // FILTER
        //// In Note
        //public float filterOut = 0f;
        //public float filterCutoff = 1000f;

        //// In CombineNotes(), per Note
        //float raw = GetSaw(note.phase);
        //float alpha = Mathf.Exp(-2f * Mathf.PI * note.filterCutoff / sampleRate);
        //note.filterOut = note.filterOut + alpha* (raw - note.filterOut);
        //float value = note.filterOut * GetEnvelope(note);

        for (int i = 0; i < offNoteIDs.Count; i++)
        {
            if (time >= notes[offNoteIDs[i]].refTime + notes[offNoteIDs[i]].release)
            {
                RemoveNote(offNoteIDs[i]);
                i--;
            }
        }

        if (notes.Count > 0 && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        else if (notes.Count == 0 && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void AddNote(int key)
    {
        if (notes.ContainsKey(key))
        {
            notes.Remove(key);

            if (offNoteIDs.Contains(key))
                offNoteIDs.Remove(key);
        }

        notes.Add(key, new(2, key, edo, .05f, .5f, .3f, .5f, 1f, octaveRatio: 1.6f, octaveStartRatio: 2));
    }

    void AddNote(int key, int cents)
    {
        if (notes.ContainsKey(key))
        {
            notes.Remove(key);

            if (offNoteIDs.Contains(key))
                offNoteIDs.Remove(key);
        }

        notes.Add(key, new(0, cents, .05f, .5f, .3f, .5f, 1f));
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
    

    float Sine(float phase) => Mathf.Sin(2 * Mathf.PI * phase);
    float Saw(float phase) => phase * 2 - 1;
    float Triangle(float phase) => Mathf.Abs(phase * 4.0f - 2.0f) - 1.0f;
    float Square(float phase) => phase >= .5f ? 1 : 0;
}
