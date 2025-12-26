using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchHandler
{
    public struct TouchList
    {
        public ReadOnlyArray<Touch> touches;
        public int[] ids => touches.Select(x => x.touchId).ToArray();
        public Vector2[] positions => touches.Select(x => (Vector2)Camera.main.ScreenToWorldPoint(x.screenPosition)).ToArray();
        public bool[] wasPressedThisFrame => touches.Select(x => x.began).ToArray();
        public int Count => touches.Count;

        public TouchList(ReadOnlyArray<Touch> t) => touches = t;
    }
    TouchList touchList;

    public TouchList GetTouchList() => new(Touch.activeTouches);
}
