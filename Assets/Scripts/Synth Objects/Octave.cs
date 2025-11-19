using UnityEngine;

public class Octave
{
    public int value;
    public float scale;
    public float root;

    public Octave(int octave, float scale = 2, float? root = null)
    {
        if (scale < 1)
            scale = 1;

        this.value = octave;
        this.scale = scale;
        this.root = root ?? scale;
    }

    public static implicit operator Octave(int o) => new Octave(o);
    public static implicit operator Octave((int octave, float scale) os) => new Octave(os.octave, os.scale);
    public static implicit operator Octave((int octave, float scale, float root) osr) => new Octave(osr.octave, osr.scale, osr.root);
}
