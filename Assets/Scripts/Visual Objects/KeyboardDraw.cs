using GLDebug;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEditor.PackageManager;
using UnityEngine;

public class KeyboardDraw
{
    public static Vector2 xBounds;
    public static Vector2 yBounds;
    public float XInterval { get; private set; }
    public List<Vector2> Positions { get; private set; }
    public Vector2 KeySize { get; private set; }

    Main main;

    private int EDO = -1;
    private int EXTEND = 0;
    public static TMP_FontAsset font;
    public static float[] JI = new float[] { 1, 16f/15f, 9f/8f, 6f/5f, 5f/4f, 4f/3f, Mathf.Sqrt(2), 3f/2f, 8f/5f, 5f/3f, 7f/4f, 9f/5f, 15f/8f, 2 };
    public static string[] JInames = new string[] { "U", "m2", "M2", "m3", "M3", "P4", "Tri", "P5", "m6", "M6", "H7", "m7", "M7", "O" };
    public static float limitFrequencyRatio = 2f.AddCents(50);

    public KeyboardDraw(Main m, int edo = -1)
    {
        main = m;

        if (xBounds == Vector2.zero)
        {
            float x = Camera.main.orthographicSize * ((float)Screen.width / (float)Screen.height);
            xBounds = new Vector2(-x, x);
        }

        if (yBounds == Vector2.zero)
        {
            float y = Camera.main.orthographicSize;
            yBounds = new Vector2(-y, y * .45f);
        }
        
        (Positions, KeySize) = GetPositionsAndSize(edo);
        EDO = edo;
        EXTEND = main.extend;
    }

    public (List<Vector2>, Vector2) GetPositionsAndSize(int edo)
    {
        if (edo != EDO || main.extend != EXTEND)
        {
            EDO = edo;
            EXTEND = main.extend;
            edo = edo + EXTEND;

            float width = xBounds.y - xBounds.x;
            XInterval = width / ((float)edo + 1);

            Positions = new List<Vector2>();
            KeySize = new Vector2(XInterval, yBounds.y - yBounds.x);

            for (int i = 0; i < ((float)edo + 1); i++)
            {
                float xPosition = xBounds.x + (XInterval / 2) + (XInterval * i);
                float yPosition = (yBounds.y + yBounds.x) / 2;
                Vector2 position = new Vector2(xPosition, yPosition);
                Positions.Add(position);
            }
        }

        return (Positions, KeySize);
    }

    public void Draw(int edo, List<Vector2> positions)
    {
        (Positions, KeySize) = GetPositionsAndSize(edo);
        List<int> keysPressed = positions.Select(x => GetKey(x)).ToList();

        int keyIndex = 0;
        foreach (Vector2 position in Positions)
        {
            float frequencyRatio = main.partitionMode switch
            {
                Main.PartitionMode.Edo => Mathf.Pow(Mathf.Pow(main.Period, 1 / (float)edo), (float)keyIndex + main.shift),
                Main.PartitionMode.Cent => SynthPlayer.baseFrequency.AddCents(main.cents * (keyIndex + main.shift)) / SynthPlayer.baseFrequency,
                _ => 1
            };

            bool pressed = keysPressed.Contains(keyIndex);
            bool isPeriod = main.partitionMode switch
            {
                Main.PartitionMode.Edo => (keyIndex + main.shift) % edo == 0,
                Main.PartitionMode.Cent => GetGeometricClosenessToPower(frequencyRatio, 2) < .025f,
                _ => false
            };
            float keyColor = pressed ? .08f : (isPeriod ? .04f : .0275f);
            float keyBorderColor = .18f;

            GLGizmos.SetLayer(-1);
            GLGizmos.SetColor(new Color(keyColor, keyColor, keyColor));
            GLGizmos.DrawSolidBox(position, KeySize);

            GLGizmos.SetLayer(0);
            GLGizmos.SetColor(new Color(keyBorderColor, keyBorderColor, keyBorderColor));
            GLGizmos.DrawWeightedBox(position, KeySize, .02f, BorderType.Inside);

            TextBoxParams tbp = new TextBoxParams() { alignment = TextAlignmentOptions.Bottom, positionPivot = PositionPivot.Bottom, fitTextToBox = true, textBoxSize = Vector2.one * XInterval * .9f, lineSpacing = 20 };
            void DrawNullKey()
            {
                GLGizmos.SetColor(new Color(1, 1, 1, .2f));
                GLGizmos.DrawText($"--", position - (Vector2.up * ((yBounds.y - yBounds.x) / 2 - .2f)), font, 3.25f, tbp);
            }

            if (frequencyRatio == 0)
            {
                DrawNullKey();
                keyIndex++;
                continue;
            }

            float octaveAdjAmt = Mathf.Abs(Mathf.Floor(Mathf.Log(frequencyRatio, 2)));
            if (frequencyRatio > limitFrequencyRatio)
                frequencyRatio /= Mathf.Pow(2, octaveAdjAmt);
            else if (frequencyRatio < 1)
                frequencyRatio *= Mathf.Pow(2, octaveAdjAmt);

            if (octaveAdjAmt != 0 && frequencyRatio * 2 < limitFrequencyRatio)
                frequencyRatio *= 2;

            float[] JIDiff = JI.Select(x => main.GetCentDifference(x, frequencyRatio)).ToArray();

            float minJIDiff = 1200;
            int minJIIndex = -1;

            for (int i = 0;  i < JIDiff.Length; i++)
            {
                float prevMinJIDiff = minJIDiff;
                minJIDiff = Mathf.Min(minJIDiff, Mathf.Abs(JIDiff[i]));
                if (minJIDiff < prevMinJIDiff)
                    minJIIndex = i;
            }

            if (minJIIndex == -1)
            {
                DrawNullKey();
                keyIndex++;
                continue;
            }

            if (Mathf.Abs(JIDiff[minJIIndex]) <= 50)
            {
                string topTxt = Mathf.Abs(JIDiff[minJIIndex]) > 0.01f ? (Mathf.Sign(JIDiff[minJIIndex]) == 1 ? "+" : "") + $"{Mathf.RoundToInt(JIDiff[minJIIndex])}" : "";
                string bottomTxt = JInames[minJIIndex];
                GLGizmos.SetColor(Mathf.Abs(JIDiff[minJIIndex]) <= 33 ? Color.white : new Color(1, 1, 1, .2f));
                GLGizmos.DrawText($"{topTxt}\n{bottomTxt}", position - (Vector2.up * ((yBounds.y - yBounds.x) / 2 - .2f)), font, 3.25f, tbp);
            }

            keyIndex++;
        }
    }

    public int GetKey(Vector2 position)
    {
        float xPos = position.x;
        float yPos = position.y;

        if (xPos < xBounds.x || xPos > xBounds.y || yPos < yBounds.x || yPos > yBounds.y)
            return -1;

        float relativeX = Mathf.InverseLerp(xBounds.x, xBounds.y, xPos) * (EDO + 1 + EXTEND);
        int key = Mathf.FloorToInt(relativeX);

        return key;
    }

    private float GetGeometricClosenessToPower(float number, float power)
    {
        if (number <= 0) return float.PositiveInfinity;

        // The exponent value (e.g., for 6, log2 is ~2.58)
        float logValue = Mathf.Log(number) / Mathf.Log(power);

        // Distance to the nearest integer exponent
        // Result of 0.0 means it is exactly a power of 2.
        // Result of 0.5 means it is geometrically right in the middle (the geometric mean).
        float distance = Mathf.Abs(logValue - Mathf.Round(logValue));

        return distance;
    }
}
