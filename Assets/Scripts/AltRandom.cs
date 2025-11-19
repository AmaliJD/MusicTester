using UnityEngine;

public class AltRandom
{
    uint s = 1;

    public AltRandom()
    {
        s = (uint)this.GetHashCode();
    }

    public void Seed(uint seed) => s = seed;

    // call this wherever you need a -1..+1 float
    public float Roll()
    {
        s ^= s << 13;
        s ^= s >> 17;
        s ^= s << 5;
        return (s * 1f / uint.MaxValue) * 2f - 1f;   // -1..+1
    }
}