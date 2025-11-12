
public static class MathF
{
    public static double InverseLerp(double a, double b, double value) => (value - a) / (b - a);
    public static double InverseLerpClamped(double a, double b, double value) => Clamp01((value - a) / (b - a));
    public static double Lerp(double a, double b, double t) => (1F - t) * a + t * b;

    public static double Clamp01(double value)
    {
        if (value < 0) value = 0;
        if (value > 1) value = 1;
        return value;
    }
}
