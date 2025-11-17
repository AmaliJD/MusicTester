using UnityEngine;

public class ADSR
{
    public float attack = 0;
    public float decay = 0;
    public float sustain = 1;
    public float release = 0;
    public float velocity = 1;

    public ADSR(float? a = null, float? d = null, float? s = null, float? r = null, float? v = null)
    {
        attack = a ?? attack;
        decay = d ?? decay;
        sustain = s ?? v ?? sustain;
        release = r ?? release;
        velocity = v ?? s ?? velocity;
    }
}
