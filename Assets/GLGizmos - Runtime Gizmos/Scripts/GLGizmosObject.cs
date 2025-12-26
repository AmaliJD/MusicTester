using System;
using UnityEngine;

namespace GLDebug
{
    [System.Serializable]
    public class GLGizmosObject
    {
        void OnEnable()
        {
            size = Vector2.one;
            color = Color.white;
        }

        public enum GizmoType
        {
            Box, Circle, Line, Triangle, Collider
        }
        public GizmoType gizmoType;

        [Flags]
        public enum LocalSpace
        {
            Position = 1, Rotation = 2, Scale = 4
        }
        public LocalSpace space;
        public LocalSpace space2;

        public enum PositionType
        {
            This, Transform, Raw
        }
        public PositionType positionType;
        public PositionType positionType2;

        public enum ScaleSizeType
        {
            Multiply, Add
        }
        public ScaleSizeType scaleSizeType;

        [SerializeField]
        private bool initialized;

        public Transform positionTransform;
        public Vector2 positionOffset;
        public Transform positionTransform2;
        public Vector2 positionOffset2;
        
        public Vector2 size = Vector2.one;
        public float angle;
        public float weight;
        public GLGizmos.BorderType borderType;
        public Color color = Color.white;
        public bool inheritColor;
        public int layer;
        public bool inheritLayer = true;

        [Min(0)]
        public float edgeRadius;
        public bool solidEdgeRadius;
        public bool cutOutBox;
        public bool solid;

        [Min(0)]
        public float radius;
        [Range(-360, 360)]
        public float arcAngle;
        public GLGizmos.ArcCloseType arcCloseType;
        public int numEdges = 0;

        public enum LineType
        {
            Solid, Dashed, Bezier
        }
        public LineType lineType;
        [Range(-1, 1)]
        public float bezierCurve = .75f;

        [Min(0)]
        public float dashLength;
        [Min(0)]
        public float gapSize;

        public Vector2 centerOffset;
        public float skew;

        public Collider2D collider2D;
    }
}
