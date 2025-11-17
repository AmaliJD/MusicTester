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

public static class ADSRExtensions
{
    public static ADSR Clone(this ADSR adsr, float? attack = null, float? decay = null, float? sustain = null, float? release = null, float? velocity = null)
    {
        return new ADSR(
            attack ?? adsr.attack,
            decay ?? adsr.decay,
            sustain ?? adsr.sustain,
            release ?? adsr.release,
            velocity ?? adsr.velocity
        );
    }

    public static void Modify(this ADSR adsr, float? attack = null, float? decay = null, float? sustain = null, float? release = null, float? velocity = null)
    {
        adsr.attack = attack ?? adsr.attack;
        adsr.decay = decay ?? adsr.decay;
        adsr.sustain = sustain ?? adsr.sustain;
        adsr.release = release ?? adsr.release;
        adsr.velocity = velocity ?? adsr.velocity;
    }
}
