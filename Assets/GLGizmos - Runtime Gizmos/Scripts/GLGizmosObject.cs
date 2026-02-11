using System;
using TMPro;
using UnityEngine;

namespace GLDebug
{
    [System.Serializable]
    public class GLGizmosObject
    {
        public enum GizmoType
        {
            Box, Circle, Line, Capsule, Triangle, Collider, Text
        }
        public GizmoType gizmoType;

        [Serializable]
        public struct GLGObjectPosition
        {
            public LocalSpace space;
            public PositionType type;
            public Transform transform;
            public Vector2 offset;
        }
        public GLGObjectPosition ObjectPosition1;
        public GLGObjectPosition ObjectPosition2;

        [Flags]
        public enum LocalSpace
        {
            Position = 1, Rotation = 2, Scale = 4
        }

        public enum PositionType
        {
            This, Transform, Raw
        }

        public enum ScaleSizeType
        {
            Multiply, Add
        }
        public ScaleSizeType scaleSizeType;

        // generic
        public bool solid;

        [Min(0)]
        public float radius;
        public Vector2 size = Vector2.one;
        public float rotation;

        public float weight;
        public BorderType borderType;

        // box
        [Range(0f, 1f)]
        public float roundCorners01;
        public bool hideBox;
        public bool solidBorder;

        // circle
        [Range(-360, 360)]
        public float arcAngle;
        public ArcCloseType arcCloseType;
        public int numEdges = 0;
        public bool roundCenter;
        public bool dashed;

        // line
        public enum LineType
        {
            Solid, Dashed, Dotted, Bezier
        }
        public LineType lineType;
        public bool roundedTips;
        [Range(-1, 1)]
        public float bezierCurve = .75f;
        [Min(0)]
        public float dashLength;
        [Min(0)]
        public float gapSize;

        // triangle
        public Vector2 centerOffset;
        public float skew;

        // capsule
        public bool useToFromPositions;
        public CapsuleDirection2D capsuleDirection;

        // collider
        public Collider2D collider2D;

        // text
        [Multiline(5)]
        public string text;
        public TMP_FontAsset font;
        public float fontSize;
        public bool autoSize;
        public FontStyles fontStyle;

        public bool showTextBox;
        public Color textBoxColor;
        public TextAlignmentOptions textAlignment;
        public PositionPivot positionPivot;

        public float characterSpacing;
        public float wordSpacing;
        public float lineSpacing;
        public float paragraphSpacing;

        // settings
        public Color color = Color.white;
        public bool inheritColor = true;

        public int layer = 0;
        public bool inheritLayer = true;

        public bool disable;

        [SerializeField]
        private bool initialized;

        public void Uninitialize() => initialized = false;
    }
}
