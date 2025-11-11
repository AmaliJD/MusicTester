using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(AudioSource))]
public class SynthPlayer : MonoBehaviour
{
    private AudioSource audioSource;

    private static float sampleRate = 0;
    private static float baseFrequency = 110;
    private float time;
    List<KeyControl> keys = new();

    public struct Note
    {
        public float phase;
        public float frequency;

        public Note(int octave, int index, int edo)
        {
            if (edo < 1)
                edo = 1;

            if (octave < 0)
                octave = 0;

            phase = 0;
            frequency = (baseFrequency * ((float)octave + 1)) * Mathf.Pow(Mathf.Pow(2, 1 / (float)edo), (float)index);
        }

        public Note(int octave, float ratio)
        {
            if (octave < 0)
                octave = 0;

            phase = 0;
            frequency = (baseFrequency * (float)octave + 1) * ratio;
        }

        public Note(float freq)
        {
            phase = 0;
            frequency = freq;
        }

        public void UpdatePhase()
        {
            this.phase += frequency / sampleRate;
            if (phase >= 1) phase -= 1;
        }

    }
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
        for (int i = 0; i < data.Length; i += channels)
        {
            float value = CombineNotes();

            for (int c = 0; c < channels; c++)
                data[i + c] = value;

            foreach (int index in new List<int>(notes.Keys))
            {
                Note tempNote = notes[index];
                tempNote.UpdatePhase();
                notes[index] = tempNote;
            }
        }
    }

    private void Update()
    {
        time = Time.time;

        int edo = keys.Count - 1;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].wasPressedThisFrame)
            {
                notes.Add(i, new(1, i, edo));
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

    float CombineNotes()
    {
        float value = 0;
        foreach (var entry in notes)
        {
            Note note = entry.Value;
            value += Triangle(note.phase);
        }
        value /= notes.Count;

        return value;
    }

    float Sine(float phase)
    {
        return Mathf.Sin(2 * Mathf.PI * phase);
    }

    float Saw(float phase)
    {
        return phase * 2 - 1;
    }

    float Triangle(float phase)
    {
        return Mathf.Abs(phase * 4.0f - 2.0f) - 1.0f;
    }
}
