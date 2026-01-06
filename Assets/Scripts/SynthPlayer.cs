using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SynthPlayer : MonoBehaviour
{
    private AudioSource audioSource;

    public static float sampleRate = 0;
    public static float baseFrequency = 220;
    private double time;
    private double timeIncrement = 1.0f;

    List<Note> notes = new();
    List<Note> audioBufferedNotes = new();
    Dictionary<int, Note> idNotes = new();
    public List<int> NoteIDList => new List<int>(idNotes.Keys);

    List<Synth> synths = new();
    int synthIndex;
    ADSR adsr = new(.05f, .5f, .3f, .5f, 1f);

    // test variables
    float maxValue = 0;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        sampleRate = AudioSettings.outputSampleRate;
        timeIncrement = 1 / sampleRate;
        synths.Add(new(Synth.Waveform.Triangle, adsr.Clone(attack: .12f, sustain: 1.5f, velocity: 2f, release: .5f), 0, 3, 8));
        synths.Add(new(Synth.Waveform.Saw, adsr));
        synths.Add(new(Synth.Waveform.Saw, adsr.Clone(attack: .08f, release: .8f), -1, 5, 8));
        synths.Add(new(Synth.Waveform.Square, adsr.Clone(attack: .2f)));
        synths.Add(new(Synth.Waveform.Sine, adsr.Clone(attack: 0.02f, velocity: 1.5f, sustain: 1f)));
        //synths.Add(new(Synth.Waveform.Square, adsr.Clone(decay: .2f, release: .75f), 0, 3, 700));
        //synths.Add(new(Synth.Waveform.Triangle, adsr.Clone(release: 0)));
        //synths.Add(new(Synth.Waveform.Sine, adsr.Clone(attack: .05f, decay: 1, release: 1.5f, sustain: .8f, velocity: .2f), 0, 2, .5f));
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        time = AudioSettings.dspTime;

        audioBufferedNotes.Clear();
        audioBufferedNotes.AddRange(notes);

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

            time += timeIncrement;
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

    private void LateUpdate()
    {
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

    public Synth GetSynth(int index) => synths[index % synths.Count];
    public Synth GetSynth() => synths[synthIndex];
    public void IncrementSynthIndex() => synthIndex = (synthIndex + 1) % synths.Count;

    public Note GetNote(int id) => idNotes.ContainsKey(id) ? idNotes[id] : null;

    public void SetNoteFrequency(int noteID, float frequency) => idNotes[noteID].frequency = frequency;

    public void AddNote(Note note, int? id = null)
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

    void ReleaseNote(Note note) => note.TurnOff();
    public void ReleaseNote(int id)
    {
        ReleaseNote(idNotes[id]);
        idNotes.Remove(id);
    }

    public void ReleaseAllNotes()
    {
        foreach (int id in new List<int>(idNotes.Keys))
            ReleaseNote(id);
    }

    void RemoveNote(Note note) => notes.Remove(note);
}
