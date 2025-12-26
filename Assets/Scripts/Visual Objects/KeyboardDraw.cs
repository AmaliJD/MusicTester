using GLDebug;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class KeyboardDraw
{
    public static Vector2 xBounds;
    public static Vector2 yBounds;
    public float XInterval { get; private set; }
    public List<Vector2> Positions { get; private set; }
    public Vector2 KeySize { get; private set; }

    private int EDO = -1;

    [Min(1)]
    public int rows = 1;

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
            yBounds = new Vector2(-y, y);
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
            float keyColor = pressed ? .08f : .04f;
            float keyBorderColor = .18f;

            GLGizmos.SetLayer(-1);
            GLGizmos.SetColor(new Color(keyColor, keyColor, keyColor));
            GLGizmos.DrawSolidBox(position, KeySize);

            GLGizmos.SetLayer(0);
            GLGizmos.SetColor(new Color(keyBorderColor, keyBorderColor, keyBorderColor));
            GLGizmos.DrawWeightedBox(position, KeySize, .02f, GLGizmos.BorderType.Inside);

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
