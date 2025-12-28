using GLDebug;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class KeyboardDraw
{
    public static Vector2 xBounds;
    public static Vector2 yBounds;
    public float XInterval { get; private set; }
    public List<Vector2> Positions { get; private set; }
    public Vector2 KeySize { get; private set; }

    private int EDO = -1;
    public static TMP_FontAsset font;
    public static float[] JI = new float[] { 1, 16f/15f, 9f/8f, 6f/5f, 5f/4f, 4f/3f, Mathf.Sqrt(2), 3f/2f, 8f/5f, 5f/3f, 9f/5f, 15f/8f, 2 };
    public static string[] JInames = new string[] { "U", "m2", "M2", "m3", "M3", "P4", "Tri", "P5", "m6", "M6", "m7", "M7", "O" };

    public KeyboardDraw(int edo = -1)
    {
        if (xBounds == Vector2.zero)
        {
            float x = Camera.main.orthographicSize * ((float)Screen.width / (float)Screen.height);
            xBounds = new Vector2(-x, x);
        }

        if (yBounds == Vector2.zero)
        {
            float y = Camera.main.orthographicSize;
            yBounds = new Vector2(-y, y * .6f);
        }
        
        (Positions, KeySize) = GetPositionsAndSize(edo);
        EDO = edo;
    }

    public (List<Vector2>, Vector2) GetPositionsAndSize(int edo)
    {
        if (edo != EDO)
        {
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

            EDO = edo;
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
            bool pressed = keysPressed.Contains(keyIndex);
            float keyColor = pressed ? .08f : .03f;
            float keyBorderColor = .18f;

            float frequencyMult = Mathf.Pow(Mathf.Pow(2, 1 / (float)edo), (float)keyIndex);
            float[] JIDiff = JI.Select(x => Main.GetCentDifference(x, frequencyMult)).ToArray();

            float minJIDiff = 1200;
            int minJIIndex = -1;

            for (int i = 0;  i < JIDiff.Length; i++)
            {
                float prevMinJIDiff = minJIDiff;
                minJIDiff = Mathf.Min(minJIDiff, Mathf.Abs(JIDiff[i]));
                if (minJIDiff < prevMinJIDiff)
                    minJIIndex = i;
            }

            GLGizmos.SetLayer(-1);
            GLGizmos.SetColor(new Color(keyColor, keyColor, keyColor));
            GLGizmos.DrawSolidBox(position, KeySize);

            GLGizmos.SetLayer(0);
            GLGizmos.SetColor(new Color(keyBorderColor, keyBorderColor, keyBorderColor));
            GLGizmos.DrawWeightedBox(position, KeySize, .02f, BorderType.Inside);

            if (Mathf.Abs(JIDiff[minJIIndex]) <= 33)
            {
                string topTxt = Mathf.Abs(JIDiff[minJIIndex]) > 0.01f ? (Mathf.Sign(JIDiff[minJIIndex]) == 1 ? "+" : "") + $"{Mathf.RoundToInt(JIDiff[minJIIndex])}" : "";
                string bottomTxt = JInames[minJIIndex];
                GLGizmos.SetColor(Color.white);
                GLGizmos.DrawText($"{topTxt}\n{bottomTxt}", position - (Vector2.up * ((yBounds.y - yBounds.x) / 2 - .2f)), font, 4, new TextBoxParams() { alignment = TextAlignmentOptions.Bottom, positionPivot = PositionPivot.Bottom, fitTextToBox = true, textBoxSize = Vector2.one * XInterval * .95f });
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

        float relativeX = Mathf.InverseLerp(xBounds.x, xBounds.y, xPos) * (EDO + 1);
        int key = Mathf.FloorToInt(relativeX);

        return key;
    }
}
