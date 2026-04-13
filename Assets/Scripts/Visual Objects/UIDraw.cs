using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class UIDraw
{
    public static Vector2 xBounds;
    public static Vector2 yBounds;

    Main main;

    public UIDraw(Main m)
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
            yBounds = new Vector2(y * .45f, y);
        }
    }

    public void Draw()
    {

    }
}
