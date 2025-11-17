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

    public static implicit operator Octave(uint o) => new Octave(o);
    public static implicit operator Octave((uint octave, float scale) os) => new Octave(os.octave, os.scale);
    public static implicit operator Octave((uint octave, float scale, float root) osr) => new Octave(osr.octave, osr.scale, osr.root);
}
