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
    public static float baseFrequency = 110;
    private double time;
    float maxValue = 0;
    List<KeyControl> keys = new();

    public Dictionary<int, Note> notes = new();

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
            Keyboard.current.f12Key,
            Keyboard.current.digit1Key,
            Keyboard.current.digit2Key,
            Keyboard.current.digit3Key,
            Keyboard.current.digit4Key,
            Keyboard.current.digit5Key,
            //Keyboard.current.digit6Key,
            //Keyboard.current.digit7Key,
            //Keyboard.current.digit8Key,
            //Keyboard.current.digit9Key,
            //Keyboard.current.digit0Key,
            //Keyboard.current.minusKey,
            //Keyboard.current.equalsKey,
        };
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
                    float t = (float)MathF.InverseLerpClamped(note.refTime + note.attack, note.refTime + note.attack + note.decay, time);
                    envelope = (float)MathF.Lerp(note.velocity, note.sustain, t);
                }
            }
            else
            {
                if (time - note.refTime < note.release)
                {
                    envelope = (1 - (float)MathF.InverseLerpClamped(note.refTime, note.refTime + note.release, time)) * note.sustain;
                }
            }

            value += Saw(note.phase) * envelope * gain;
        }

        return value;
    }

    private void Update()
    {
        int edo = keys.Count - 1;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].wasPressedThisFrame)
            {
                notes.Add(i, new(1, i, edo, a: 1, d: 1, s: .2f, v: 1f));
                Debug.Log($"{notes[i].frequency} Hz");
            }
            else if (keys[i].wasReleasedThisFrame)
            {
                notes.Remove(i);
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
    

    float Sine(float phase) => Mathf.Sin(2 * Mathf.PI * phase);
    float Saw(float phase) => phase * 2 - 1;
    float Triangle(float phase) => Mathf.Abs(phase * 4.0f - 2.0f) - 1.0f;
    float Square(float phase) => phase >= .5f ? 1 : 0;
}
