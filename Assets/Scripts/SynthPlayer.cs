using GLDebug;
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
    int edo = 12;

    List<KeyControl> keys = new();
    Dictionary<int, Note> idNotes = new();
    List<Note> notes = new();
    List<Note> audioBufferedNotes = new();

    List<Synth> synths = new();
    int synthIndex;
    ADSR adsr = new(.05f, .5f, .3f, .5f, 1f);

    TouchHandler touchHandler = new();

    // test variables
    float maxValue = 0;
    Note freeNote;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        sampleRate = AudioSettings.outputSampleRate;
        //synths.Add(new(Synth.Waveform.Sine, adsr.Clone(attack: 0.02f), 0, 4, 8));
        synths.Add(new(Synth.Waveform.Saw, adsr));
        //synths.Add(new(Synth.Waveform.White, new ADSR(.02f, .2f, .3f, .7f, 1)));
        synths.Add(new(Synth.Waveform.Saw, adsr.Clone(attack: .08f, release: .8f), -1, 5, 8));
        synths.Add(new(Synth.Waveform.Square, adsr.Clone(decay: .2f, release: .75f), 0, 3, 700));
        synths.Add(new(Synth.Waveform.Square, adsr.Clone(attack: .2f)));
        synths.Add(new(Synth.Waveform.Triangle, adsr.Clone(release: 0)));
        synths.Add(new(Synth.Waveform.Sine, adsr.Clone(attack: .05f, decay: 1, release: 2f, sustain: 1, velocity: .2f)));
        freeNote = new(baseFrequency, 0, synths[synthIndex]);

        //keys = new()
        //{
        //    Keyboard.current.escapeKey,
        //    Keyboard.current.f1Key,
        //    Keyboard.current.f2Key,
        //    Keyboard.current.f3Key,
        //    Keyboard.current.f4Key,
        //    Keyboard.current.f5Key,
        //    Keyboard.current.f6Key,
        //    Keyboard.current.f7Key,
        //    Keyboard.current.f8Key,
        //    Keyboard.current.f9Key,
        //    Keyboard.current.f10Key,
        //    Keyboard.current.f11Key,
        //    Keyboard.current.f12Key,
        //    //Keyboard.current.digit1Key,
        //    //Keyboard.current.digit2Key,
        //    //Keyboard.current.digit3Key,
        //    //Keyboard.current.digit4Key,
        //    //Keyboard.current.digit5Key,
        //    //Keyboard.current.digit6Key,
        //    //Keyboard.current.digit7Key,
        //    //Keyboard.current.digit8Key,
        //    //Keyboard.current.digit9Key,
        //    //Keyboard.current.digit0Key,
        //    //Keyboard.current.minusKey,
        //    //Keyboard.current.equalsKey,
        //};
        //edo = keys.Count - 1;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        time = AudioSettings.dspTime;
        audioBufferedNotes = new List<Note>(notes);
        for (int i = 0; i < data.Length; i += channels)
        {
            float value = CombineNotes();

            for (int c = 0; c < channels; c++)
                data[i + c] = value;

            maxValue = Mathf.Max(maxValue, value);

            foreach (Note note in audioBufferedNotes)
            {
                note.UpdatePhase();
            }
        }
    }

    float CombineNotes()
    {
        float value = 0;
        float gain = .15f;
        double k = -3;

        for (int i = audioBufferedNotes.Count - 1; i >= 0; i--)
        {
            Note note = audioBufferedNotes[i];
            Synth synth = note.synth;
            ADSR adsr = note.GetADSR();
            float envelope = adsr.sustain;

            // if completed
            if (!note.on && time >= note.refTime + note.GetADSR().release)
            {
                RemoveNote(note);
                continue;
            }

            // note reached end
            if (note.on && note.duration > 0 && time >= note.refTime + note.duration)
                note.TurnOff();

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

    float GetFrequencyFromPosition(float yPos)
    {
        return Mathf.Lerp(220, 440, Mathf.InverseLerp(Camera.main.ScreenToWorldPoint(new Vector2(0, 0)).y, Camera.main.ScreenToWorldPoint(new Vector2(0, Screen.height)).y, yPos));
    }

    private void Update()
    {
        time = AudioSettings.dspTime;
        //Debug.Log($"Audio Value: {maxValue}");

        TouchHandler.TouchList touchList = touchHandler.GetTouchList();

        // change synth index
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            synthIndex = (synthIndex + 1) % synths.Count;
            freeNote = new(freeNote.frequency, 0, synths[synthIndex]);
        }

        // keyboard
        //for (int i = 0; i < keys.Count; i++)
        //{
        //    if (keys[i].wasPressedThisFrame)
        //    {
        //        AddNote(new Note(/*(0, 1.67f, 2)*/0, i, edo, 0, synths[synthIndex]), i);
        //        Debug.Log($"{idNotes[i].frequency} Hz");
        //    }
        //    else if (keys[i].wasReleasedThisFrame)
        //    {
        //        ReleaseNote(idNotes[i]);
        //    }
        //}

        for (int i = 0; i < touchList.Count; i++)
        {
            if (touchList.wasPressedThisFrame[i])
            {
                //AddNote(new Note(0, i, edo, 0, synths[synthIndex]), touchList.ids[i]);
                AddNote(new Note(GetFrequencyFromPosition(touchList.positions[i].y), 0, synths[synthIndex]), touchList.ids[i]);
            }
            else
            {
                idNotes[touchList.ids[i]].frequency = GetFrequencyFromPosition(touchList.positions[i].y);
            }
        }

        foreach (int id in new List<int>(idNotes.Keys))
        {
            if (!touchList.ids.Contains(id))
            {
                ReleaseNote(idNotes[id]);
                idNotes.Remove(id);
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

    void AddNote(Note note, int? id = null)
    {
        if (notes.Contains(note))
            note.TurnOn();
        else
            notes.Add(note.On());

        if (id != null)
            AddIdNote(id.Value, note);
    }

    void AddIdNote(int id, Note note)
    {
        if (idNotes.ContainsKey(id))
        {
            idNotes[id].TurnOff();
            idNotes.Remove(id);
        }

        idNotes.Add(id, note.On());
    }

    void ReleaseNote(Note note)
    {
        note.TurnOff();
    }

    void RemoveNote(Note note)
    {
        notes.Remove(note);
    }
}
