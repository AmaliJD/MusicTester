using UnityEngine;

public class Octave
{
    public uint value;
    public float scale;
    public float root;

    public Octave(uint octave, float scale = 2, float? root = null)
    {
        if (scale < 1)
            scale = 1;

        this.value = octave;
        this.scale = scale;
        this.root = root ?? scale;
    }

    public static implicit operator Octave(uint octave) => new Octave(octave);
    public static implicit operator Octave((uint octave, float scale) octave_scale) => new Octave(octave_scale.octave, octave_scale.scale);
    public static implicit operator Octave((uint octave, float scale, float root) octave_scale_root) => new Octave(octave_scale_root.octave, octave_scale_root.scale, octave_scale_root.root);
}
