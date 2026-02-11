using UnityEngine;
using UnityEngine.InputSystem;
using GLDebug;

[System.Serializable]
public class VisualNote
{
    public Note note;
    Vector2 position;
    float length = 2f;
    float height = .25f;
    bool hovering = false;

    public void Update()
    {
        Interact();
        Draw();
    }

    void Interact()
    {
        hovering = isHovering();
    }

    bool isHovering()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        return
            mousePos.x >= position.x &&
            mousePos.x <= position.x + length &&
            mousePos.y >= position.y - height / 2 &&
            mousePos.y <= position.y + height / 2;
    }

    void Draw()
    {
        

        float visualCenterX = position.x + length / 2;
        Vector2 center = new Vector2(visualCenterX, position.y);
        Vector2 size = new Vector2(length, height);

        Color color = new Color(.2f, 1f, .2f, 1f);
        Color colorBorder = new Color(.1f, .6f, .1f, 1f);

        GLGizmos.DrawSolidBox(center, size).SetColor(color);
        GLGizmos.DrawWeightedBox(center, size, .075f, BorderType.Inside).SetColor(colorBorder);
    }
}
