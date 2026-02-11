using System.Collections.Generic;
using UnityEngine;

namespace GLDebug
{
    using static GLGizmosObject;

    [ExecuteInEditMode]
    public class GLGizmosComponent : MonoBehaviour
    {
        int gizmoListCount = 0;
        public List<GLGizmosObject> gizmos;

        private void OnEnable()
        {
            GLGizmos.AddGLGizmoComponent(this);
        }

        private void OnDisable()
        {
            GLGizmos.RemoveGLGizmoComponent(this);
        }

        private void OnDestroy()
        {
            GLGizmos.RemoveGLGizmoComponent(this);
        }

        private void OnValidate()
        {
            if (gizmos == null)
                return;

            if (gizmoListCount < gizmos.Count && gizmoListCount > 1)
            {
                gizmos[gizmoListCount].Uninitialize();
            }

            gizmoListCount = gizmos.Count;
        }

        public void ReadGizmos()
        {
            if (gizmos == null)
                return;

            GLGizmos.ResetSettings();

            foreach (GLGizmosObject gizmo in gizmos)
            {
                if (gizmo.disable)
                    continue;

                if (!gizmo.inheritColor)
                    GLGizmos.SetColor(gizmo.color);
                if (!gizmo.inheritLayer)
                    GLGizmos.SetLayer(gizmo.layer);

                if (gizmo.gizmoType == GizmoType.Box)
                {
                    (Vector2? getPosition, Transform target) = GetPositionAndTargetTransform(gizmo.ObjectPosition1);
                    if (getPosition == null)
                        continue;

                    Vector2 position = getPosition.Value;
                    (Vector2 rightMult, Vector2 upMult, Vector2 scaleMult) = GetMults(gizmo.ObjectPosition1, target);
                    position += rightMult * gizmo.ObjectPosition1.offset.x * scaleMult.x + upMult * gizmo.ObjectPosition1.offset.y * scaleMult.y;

                    Vector2 size = gizmo.size;
                    if (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Scale))
                    {
                        if (gizmo.scaleSizeType == ScaleSizeType.Add)
                        {
                            size += (Vector2)target.localScale;
                        }
                        else
                        {
                            size *= target.localScale;
                        }
                    }

                    float angle = gizmo.rotation;
                    if (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Rotation))
                    {
                        angle += target.rotation.eulerAngles.z;
                    }

                    BoxParams boxParams = new BoxParams()
                    {
                        solid = gizmo.solid,
                        rotation = angle,

                        borderWidth = gizmo.weight,
                        borderType = gizmo.borderType,
                        solidBorder = gizmo.solidBorder,

                        roundCorners01 = gizmo.roundCorners01,
                        hideBox = gizmo.hideBox
                    };

                    GLGizmos.DrawCustomBox(position, size, boxParams);
                }
                else if (gizmo.gizmoType == GizmoType.Circle)
                {
                    (Vector2? getPosition, Transform target) = GetPositionAndTargetTransform(gizmo.ObjectPosition1);
                    if (getPosition == null)
                        continue;

                    Vector2 position = getPosition.Value;
                    (Vector2 rightMult, Vector2 upMult, Vector2 scaleMult) = GetMults(gizmo.ObjectPosition1, target);
                    position += rightMult * gizmo.ObjectPosition1.offset.x * scaleMult.x + upMult * gizmo.ObjectPosition1.offset.y * scaleMult.y;

                    float radius = gizmo.radius;
                    if (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Scale))
                    {
                        if (gizmo.scaleSizeType == ScaleSizeType.Add)
                        {
                            radius += Mathf.Max(Mathf.Abs(target.localScale.x), Mathf.Abs(target.localScale.y));
                        }
                        else
                        {
                            radius *= Mathf.Max(Mathf.Abs(target.localScale.x), Mathf.Abs(target.localScale.y));
                        }
                    }

                    float angle = gizmo.rotation;
                    if (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Rotation))
                    {
                        angle += target.rotation.eulerAngles.z;
                    }

                    CircleParams circleParams = new CircleParams()
                    {
                        solid = gizmo.solid,
                        dashed = gizmo.dashed,
                        
                        arcAngle = gizmo.arcAngle,
                        rotation = angle,

                        borderWidth = gizmo.weight,
                        borderType = gizmo.borderType,

                        arcCloseType = gizmo.arcCloseType,
                        numEdges = gizmo.numEdges,
                        roundCenter = gizmo.roundCenter
                    };

                    GLGizmos.DrawCustomCircle(position, radius, circleParams);
                }
                else if (gizmo.gizmoType == GizmoType.Line)
                {
                    (Vector2? getPosition1, Transform target1) = GetPositionAndTargetTransform(gizmo.ObjectPosition1);
                    (Vector2? getPosition2, Transform target2) = GetPositionAndTargetTransform(gizmo.ObjectPosition2);
                    if (getPosition1 == null || getPosition2 == null)
                        continue;

                    Vector2 position1 = getPosition1.Value;
                    Vector2 position2 = getPosition2.Value;

                    (Vector2 rightMult1, Vector2 upMult1, Vector2 scaleMult1) = GetMults(gizmo.ObjectPosition1, target1);
                    position1 += rightMult1 * gizmo.ObjectPosition1.offset.x * scaleMult1.x + upMult1 * gizmo.ObjectPosition1.offset.y * scaleMult1.y;

                    (Vector2 rightMult2, Vector2 upMult2, Vector2 scaleMult2) = GetMults(gizmo.ObjectPosition2, target2);
                    position2 += rightMult2 * gizmo.ObjectPosition2.offset.x * scaleMult2.x + upMult2 * gizmo.ObjectPosition2.offset.y * scaleMult2.y;

                    switch (gizmo.lineType)
                    {
                        case LineType.Solid:
                            GLGizmos.DrawWeightedLine(position1, position2, gizmo.weight, gizmo.roundedTips);
                            break;
                        case LineType.Bezier:
                            GLGizmos.DrawBezier(position1, position2, gizmo.bezierCurve, gizmo.numEdges);
                            break;
                        case LineType.Dashed:
                            GLGizmos.DrawWeightedDashedLine(position1, position2, gizmo.dashLength, gizmo.gapSize, gizmo.weight);
                            break;
                        case LineType.Dotted:
                            GLGizmos.DrawDottedLine(position1, position2, gizmo.weight / 2, gizmo.gapSize);
                            break;
                    }
                }
                else if (gizmo.gizmoType == GizmoType.Capsule)
                {
                    if (gizmo.useToFromPositions)
                    {
                        (Vector2? getPosition1, Transform target1) = GetPositionAndTargetTransform(gizmo.ObjectPosition1);
                        (Vector2? getPosition2, Transform target2) = GetPositionAndTargetTransform(gizmo.ObjectPosition2);
                        if (getPosition1 == null || getPosition2 == null)
                            continue;

                        Vector2 position1 = getPosition1.Value;
                        Vector2 position2 = getPosition2.Value;

                        (Vector2 rightMult1, Vector2 upMult1, Vector2 scaleMult1) = GetMults(gizmo.ObjectPosition1, target1);
                        position1 += rightMult1 * gizmo.ObjectPosition1.offset.x * scaleMult1.x + upMult1 * gizmo.ObjectPosition1.offset.y * scaleMult1.y;

                        (Vector2 rightMult2, Vector2 upMult2, Vector2 scaleMult2) = GetMults(gizmo.ObjectPosition2, target2);
                        position2 += rightMult2 * gizmo.ObjectPosition2.offset.x * scaleMult2.x + upMult2 * gizmo.ObjectPosition2.offset.y * scaleMult2.y;

                        if (gizmo.weight == 0 && gizmo.solid)
                        {
                            GLGizmos.DrawSolidCapsule(position1, position2, gizmo.radius);
                            continue;
                        }

                        if (gizmo.solid)
                        {
                            float newRadius = gizmo.radius;
                            if (gizmo.weight != 0)
                            {
                                (float newBorderWidth, BorderType newBorderType) = GLGizmos.AdjustForNegativeBorderWidth(gizmo.weight, gizmo.borderType);

                                switch (newBorderType)
                                {
                                    case BorderType.Outside:
                                        newRadius += newBorderWidth * 2;
                                        break;
                                    case BorderType.Centered:
                                        newRadius += newBorderWidth;
                                        break;
                                }
                            }

                            GLGizmos.DrawSolidCapsule(position1, position2, newRadius);
                        }
                        else
                        {
                            GLGizmos.DrawWeightedCapsule(position1, position2, gizmo.radius, gizmo.weight, gizmo.borderType);
                        }
                    }
                    else
                    {
                        (Vector2? getPosition, Transform target) = GetPositionAndTargetTransform(gizmo.ObjectPosition1);
                        if (getPosition == null)
                            continue;

                        Vector2 position = getPosition.Value;
                        (Vector2 rightMult, Vector2 upMult, Vector2 scaleMult) = GetMults(gizmo.ObjectPosition1, target);
                        position += rightMult * gizmo.ObjectPosition1.offset.x * scaleMult.x + upMult * gizmo.ObjectPosition1.offset.y * scaleMult.y;

                        Vector2 size = gizmo.size;
                        if (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Scale))
                        {
                            if (gizmo.scaleSizeType == ScaleSizeType.Add)
                            {
                                size += (Vector2)target.localScale;
                            }
                            else
                            {
                                size *= target.localScale;
                            }
                        }

                        float angle = gizmo.rotation;
                        if (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Rotation))
                        {
                            angle += target.rotation.eulerAngles.z;
                        }

                        if (gizmo.solid)
                        {
                            Vector2 newSize = size;
                            if (gizmo.weight != 0)
                            {
                                (float newBorderWidth, BorderType newBorderType) = GLGizmos.AdjustForNegativeBorderWidth(gizmo.weight, gizmo.borderType);

                                switch (newBorderType)
                                {
                                    case BorderType.Outside:
                                        newSize += Vector2.one * newBorderWidth * 2;
                                        break;
                                    case BorderType.Centered:
                                        newSize += Vector2.one * newBorderWidth;
                                        break;
                                }
                            }

                            GLGizmos.DrawSolidCapsule(position, newSize, gizmo.capsuleDirection, angle);
                        }
                        else
                        {
                            GLGizmos.DrawWeightedCapsule(position, size, gizmo.capsuleDirection, angle, gizmo.weight, gizmo.borderType);
                        }
                    }
                }
                else if (gizmo.gizmoType == GizmoType.Triangle)
                {
                    (Vector2? getPosition, Transform target) = GetPositionAndTargetTransform(gizmo.ObjectPosition1);
                    if (getPosition == null)
                        continue;

                    Vector2 position = getPosition.Value;
                    (Vector2 rightMult, Vector2 upMult, Vector2 scaleMult) = GetMults(gizmo.ObjectPosition1, target);
                    position += rightMult * gizmo.ObjectPosition1.offset.x * scaleMult.x + upMult * gizmo.ObjectPosition1.offset.y * scaleMult.y;

                    Vector2 centerOffset = rightMult * gizmo.centerOffset.x * scaleMult.x + upMult * gizmo.centerOffset.y * scaleMult.y;
                    float skew = gizmo.skew * (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Scale) ? target.localScale.x : 1);

                    Vector2 size = gizmo.size;
                    if (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Scale))
                    {
                        if (gizmo.scaleSizeType == ScaleSizeType.Add)
                        {
                            size += (Vector2)target.localScale;
                        }
                        else
                        {
                            size *= target.localScale;
                        }
                    }

                    float angle = gizmo.rotation;
                    if (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Rotation))
                    {
                        angle += target.rotation.eulerAngles.z;
                    }

                    if (gizmo.solid)
                    {
                        if (gizmo.weight == 0)
                        {
                            GLGizmos.DrawSolidTriangle(position, centerOffset, size.y, size.x, skew, angle);
                        }
                        else
                        {
                            (float newBorderWidth, BorderType newBorderType) = GLGizmos.AdjustForNegativeBorderWidth(gizmo.weight, gizmo.borderType);

                            switch (newBorderType)
                            {
                                case BorderType.Outside:
                                    GLGizmos.DrawWeightedTriangle(position, centerOffset, size.y, size.x, skew, angle, newBorderWidth, newBorderType);
                                    GLGizmos.DrawSolidTriangle(position, centerOffset, size.y, size.x, skew, angle);
                                    break;
                                case BorderType.Inside:
                                    GLGizmos.DrawSolidTriangle(position, centerOffset, size.y, size.x, skew, angle);
                                    break;
                                case BorderType.Centered:
                                    GLGizmos.DrawWeightedTriangle(position, centerOffset, size.y, size.x, skew, angle, newBorderWidth / 2, BorderType.Outside);
                                    GLGizmos.DrawSolidTriangle(position, centerOffset, size.y, size.x, skew, angle);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        GLGizmos.DrawWeightedTriangle(position, centerOffset, size.y, size.x, skew, angle, gizmo.weight, gizmo.borderType);
                    }
                }
                else if (gizmo.gizmoType == GizmoType.Collider)
                {
                    if (gizmo.collider2D == null)
                        return;

                    GLGizmos.DrawCollider2D(gizmo.collider2D, gizmo.solid);
                }
                else if (gizmo.gizmoType == GizmoType.Text)
                {
                    (Vector2? getPosition, Transform target) = GetPositionAndTargetTransform(gizmo.ObjectPosition1);
                    if (getPosition == null)
                        continue;

                    Vector2 position = getPosition.Value;
                    (Vector2 rightMult, Vector2 upMult, Vector2 scaleMult) = GetMults(gizmo.ObjectPosition1, target);
                    position += rightMult * gizmo.ObjectPosition1.offset.x * scaleMult.x + upMult * gizmo.ObjectPosition1.offset.y * scaleMult.y;

                    Vector2 size = gizmo.size;
                    if (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Scale))
                    {
                        if (gizmo.scaleSizeType == ScaleSizeType.Add)
                        {
                            size += (Vector2)target.localScale;
                        }
                        else
                        {
                            size *= target.localScale;
                        }
                    }

                    float angle = gizmo.rotation;
                    if (gizmo.ObjectPosition1.space.HasFlag(LocalSpace.Rotation))
                    {
                        angle += target.rotation.eulerAngles.z;
                    }

                    TextBoxParams textBoxParams = new TextBoxParams()
                    {
                        fontStyle = gizmo.fontStyle,
                        fitTextToBox = gizmo.autoSize,
                        rotation = angle,
                        textBoxSize = size,
                        alignment = gizmo.textAlignment,
                        positionPivot = gizmo.positionPivot,

                        characterSpacing = gizmo.characterSpacing,
                        wordSpacing = gizmo.wordSpacing,
                        lineSpacing = gizmo.lineSpacing,
                        paragraphSpacing = gizmo.paragraphSpacing
                    };

                    GLGizmos.DrawText(gizmo.text, position, gizmo.font, gizmo.fontSize, textBoxParams);

                    if (gizmo.showTextBox)
                        GLGizmos.DrawOpenBox(GLGizmos.GetBoxPositionByPivot(position, size, angle, gizmo.positionPivot), size, angle).SetColor(gizmo.textBoxColor);
                }
            }
        }

        (Vector2?, Transform) GetPositionAndTargetTransform(GLGObjectPosition Position)
        {
            Vector2? position = null;
            Transform target = transform;

            switch (Position.type)
            {
                case PositionType.Transform:
                    if (Position.transform != null)
                    {
                        position = Position.transform.position;
                        target = Position.transform;
                    }
                    else
                    {
                        return (null, null);
                    }
                        break;
                case PositionType.Raw:
                    position = Vector2.zero;
                    break;
                default:
                case PositionType.This:
                    position = transform.position;
                    break;
            }

            return (position, target);
        }

        (Vector2, Vector2, Vector2) GetMults(GLGObjectPosition Position, Transform target)
        {
            Vector2 rightMult = Position.space.HasFlag(LocalSpace.Position) ? target.right : Vector2.right;
            Vector2 upMult = Position.space.HasFlag(LocalSpace.Position) ? target.up : Vector2.up;
            Vector2 scaleMult = Position.space.HasFlag(LocalSpace.Scale) ? target.localScale : Vector2.one;

            return (rightMult, upMult, scaleMult);
        }
    }
}
