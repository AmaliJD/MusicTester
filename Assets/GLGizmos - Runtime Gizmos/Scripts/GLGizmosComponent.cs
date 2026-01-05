using System;
using System.Collections.Generic;
using UnityEngine;
using GLGizmosExtensions;

namespace GLDebug
{
    using static GLGizmosObject;

    [ExecuteInEditMode]
    public class GLGizmosComponent : MonoBehaviour
    {
        private List<Action> drawActions = new();
        public GLGizmosObject[] gizmos;
        private struct TransformCache
        {
            int id;
            Vector2 position;
            Quaternion rotation;
            Vector2 scale;

            public TransformCache(Transform transform)
            {
                id = transform.GetHashCode();
                position = transform.position;
                rotation = transform.rotation;
                scale = transform.localScale;
            }
        }
        private TransformCache transformCache;
        private List<TransformCache> refTransformCache = new();

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
            SetActions();
        }

        public void OnTransformChange()
        {
            if (!transformCache.Equals(new TransformCache(transform)))
            {
                transformCache = new TransformCache(transform);
                SetActions();
            }

            List<TransformCache> tempRefTransformCache = new();
            if (gizmos != null && gizmos.Length > 0)
            {
                foreach (GLGizmosObject gizmo in gizmos)
                {
                    if (gizmo.positionTransform != null)
                        tempRefTransformCache.Add(new TransformCache(gizmo.positionTransform));

                    if (gizmo.positionTransform2 != null)
                        tempRefTransformCache.Add(new TransformCache(gizmo.positionTransform2));
                }
            }

            if (tempRefTransformCache.Count != refTransformCache.Count)
            {
                SetActions();
                return;
            }

            int i = 0;
            foreach (TransformCache cache in tempRefTransformCache)
            {
                if (!tempRefTransformCache[i].Equals(refTransformCache[i]))
                {
                    transformCache = new TransformCache(transform);
                    SetActions();
                    return;
                }
                i++;
            }
        }

        private void SetActions()
        {
            if (gizmos == null)
                return;

            drawActions.Clear();
            Color color = Color.white;
            int layer = 0;

            foreach (GLGizmosObject gizmo in gizmos)
            {
                if (gizmo.gizmoType == GizmoType.Box)
                {
                    Transform targetTransform = transform;

                    // position
                    Vector2 position;
                    switch (gizmo.positionType)
                    {
                        case PositionType.Transform:
                            if (gizmo.positionTransform != null)
                            {
                                position = gizmo.positionTransform.position;
                                targetTransform = gizmo.positionTransform;
                            }  
                            else
                                continue;
                            break;
                        case PositionType.Raw:
                            position = Vector2.zero;
                            break;
                        default:
                            position = targetTransform.position;
                            break;
                    }

                    Vector2 rightMult = gizmo.space.FlagEnumContains(LocalSpace.Position) ? targetTransform.right : Vector2.right;
                    Vector2 upMult = gizmo.space.FlagEnumContains(LocalSpace.Position) ? targetTransform.up : Vector2.up;
                    Vector2 scaleMult = gizmo.space.FlagEnumContains(LocalSpace.Scale) ? targetTransform.localScale : Vector2.one;
                    position += rightMult * gizmo.positionOffset.x * scaleMult.x + upMult * gizmo.positionOffset.y * scaleMult.y;

                    // size
                    Vector2 size = gizmo.size;
                    if (gizmo.space.FlagEnumContains(LocalSpace.Scale))
                    {
                        if (gizmo.scaleSizeType == ScaleSizeType.Add)
                            size += (Vector2)targetTransform.localScale;
                        else
                            size *= targetTransform.localScale;
                    }

                    float angle = gizmo.angle;
                    if (gizmo.space.FlagEnumContains(LocalSpace.Rotation))
                    {
                        angle += targetTransform.rotation.eulerAngles.z;
                    }

                    if (!gizmo.inheritColor)
                    {
                        color = gizmo.color;
                    }
                    if (gizmo.layer != layer && !gizmo.inheritLayer)
                    {
                        layer = gizmo.layer;
                        drawActions.Add(() => GLGizmos.SetLayer(gizmo.layer));
                    }

                    Color gizmoColor = color;
                    drawActions.Add(() => GLGizmos.DrawBox(position, size, new BoxParams() { solid = gizmo.solid, rotation = angle, edgeRadius = gizmo.edgeRadius, solidEdgeRadius = gizmo.solidEdgeRadius, onlyRenderEdgeRadius = gizmo.cutOutBox, borderWidth = gizmo.weight, borderType = gizmo.borderType }, gizmoColor));
                }
                else if (gizmo.gizmoType == GizmoType.Circle)
                {
                    Transform targetTransform = transform;

                    // position
                    Vector2 position;
                    switch (gizmo.positionType)
                    {
                        case PositionType.Transform:
                            if (gizmo.positionTransform != null)
                            {
                                position = gizmo.positionTransform.position;
                                targetTransform = gizmo.positionTransform;
                            }
                            else
                                continue;
                            break;
                        case PositionType.Raw:
                            position = Vector2.zero;
                            break;
                        default:
                            position = targetTransform.position;
                            break;
                    }

                    Vector2 rightMult = gizmo.space.FlagEnumContains(LocalSpace.Position) ? targetTransform.right : Vector2.right;
                    Vector2 upMult = gizmo.space.FlagEnumContains(LocalSpace.Position) ? targetTransform.up : Vector2.up;
                    Vector2 scaleMult = gizmo.space.FlagEnumContains(LocalSpace.Scale) ? targetTransform.localScale : Vector2.one;
                    position += rightMult * gizmo.positionOffset.x * scaleMult.x + upMult * gizmo.positionOffset.y * scaleMult.y;

                    // radius
                    float radius = gizmo.radius;
                    if (gizmo.space.FlagEnumContains(LocalSpace.Scale))
                    {
                        if (gizmo.scaleSizeType == ScaleSizeType.Add)
                            radius += Mathf.Max(Mathf.Abs(targetTransform.localScale.x), Mathf.Abs(targetTransform.localScale.y));
                        else
                            radius *= Mathf.Max(Mathf.Abs(targetTransform.localScale.x), Mathf.Abs(targetTransform.localScale.y));
                    }

                    float angle = gizmo.angle;
                    if (gizmo.space.FlagEnumContains(LocalSpace.Rotation))
                    {
                        angle += targetTransform.rotation.eulerAngles.z;
                    }

                    if (!gizmo.inheritColor)
                    {
                        color = gizmo.color;
                    }
                    if (gizmo.layer != layer && !gizmo.inheritLayer)
                    {
                        layer = gizmo.layer;
                        drawActions.Add(() => GLGizmos.SetLayer(gizmo.layer));
                    }

                    Color gizmoColor = color;
                    drawActions.Add(() => GLGizmos.DrawCircle(position, radius, new CircleParams() { solid = gizmo.solid, arcAngle = gizmo.arcAngle, rotation = angle, borderWidth = gizmo.weight, borderType = gizmo.borderType, arcCloseType = gizmo.arcCloseType, numEdges = gizmo.numEdges }, gizmoColor));
                }
                else if (gizmo.gizmoType == GizmoType.Line)
                {
                    Transform targetTransform = transform;
                    Transform targetTransform2 = transform;

                    // position
                    Vector2 position;
                    switch (gizmo.positionType)
                    {
                        case PositionType.Transform:
                            if (gizmo.positionTransform != null)
                            {
                                position = gizmo.positionTransform.position;
                                targetTransform = gizmo.positionTransform;
                            }
                            else
                                continue;
                            break;
                        case PositionType.Raw:
                            position = Vector2.zero;
                            break;
                        default:
                            position = targetTransform.position;
                            break;
                    }

                    Vector2 rightMult = gizmo.space.FlagEnumContains(LocalSpace.Position) ? targetTransform.right : Vector2.right;
                    Vector2 upMult = gizmo.space.FlagEnumContains(LocalSpace.Position) ? targetTransform.up : Vector2.up;
                    Vector2 scaleMult = gizmo.space.FlagEnumContains(LocalSpace.Scale) ? targetTransform.localScale : Vector2.one;
                    position += rightMult * gizmo.positionOffset.x * scaleMult.x + upMult * gizmo.positionOffset.y * scaleMult.y;

                    // position
                    Vector2 position2;
                    switch (gizmo.positionType2)
                    {
                        case PositionType.Transform:
                            if (gizmo.positionTransform2 != null)
                            {
                                position2 = gizmo.positionTransform2.position;
                                targetTransform2 = gizmo.positionTransform2;
                            }
                            else
                                continue;
                            break;
                        case PositionType.Raw:
                            position2 = Vector2.zero;
                            break;
                        default:
                            position2 = targetTransform2.position;
                            break;
                    }

                    Vector2 rightMult2 = gizmo.space2.FlagEnumContains(LocalSpace.Position) ? targetTransform2.right : Vector2.right;
                    Vector2 upMult2 = gizmo.space2.FlagEnumContains(LocalSpace.Position) ? targetTransform2.up : Vector2.up;
                    Vector2 scaleMult2 = gizmo.space2.FlagEnumContains(LocalSpace.Scale) ? targetTransform2.localScale : Vector2.one;
                    position2 += rightMult2 * gizmo.positionOffset2.x * scaleMult2.x + upMult2 * gizmo.positionOffset2.y * scaleMult2.y;

                    if (!gizmo.inheritColor)
                    {
                        color = gizmo.color;
                    }
                    if (gizmo.layer != layer && !gizmo.inheritLayer)
                    {
                        layer = gizmo.layer;
                        drawActions.Add(() => GLGizmos.SetLayer(gizmo.layer));
                    }

                    Color gizmoColor = color;
                    switch (gizmo.lineType)
                    {
                        case LineType.Solid:
                            if (gizmo.weight == 0)
                                drawActions.Add(() => GLGizmos.DrawLine(position, position2, gizmoColor));
                            else
                                drawActions.Add(() => GLGizmos.DrawCapsulePath(new List<Vector2>() { position, position2 }, Mathf.Abs(gizmo.weight), gizmoColor));
                            break;
                        case LineType.Bezier:
                            drawActions.Add(() => GLGizmos.DrawBezier(position, position2, gizmo.bezierCurve, gizmo.numEdges, gizmoColor));
                            break;
                        case LineType.Dashed:
                            drawActions.Add(() => GLGizmos.DrawDashedLine(position, position2, gizmo.dashLength, gizmo.gapSize, gizmoColor));
                            break;
                    }
                }
                else if (gizmo.gizmoType == GizmoType.Triangle)
                {
                    Transform targetTransform = transform;

                    // position
                    Vector2 position;
                    switch (gizmo.positionType)
                    {
                        case PositionType.Transform:
                            if (gizmo.positionTransform != null)
                            {
                                position = gizmo.positionTransform.position;
                                targetTransform = gizmo.positionTransform;
                            }
                            else
                                continue;
                            break;
                        case PositionType.Raw:
                            position = Vector2.zero;
                            break;
                        default:
                            position = targetTransform.position;
                            break;
                    }

                    Vector2 rightMult = gizmo.space.FlagEnumContains(LocalSpace.Position) ? targetTransform.right : Vector2.right;
                    Vector2 upMult = gizmo.space.FlagEnumContains(LocalSpace.Position) ? targetTransform.up : Vector2.up;
                    Vector2 scaleMult = gizmo.space.FlagEnumContains(LocalSpace.Scale) ? targetTransform.localScale : Vector2.one;
                    position += rightMult * gizmo.positionOffset.x * scaleMult.x + upMult * gizmo.positionOffset.y * scaleMult.y;

                    Vector2 centerOffset = rightMult * gizmo.centerOffset.x * scaleMult.x + upMult * gizmo.centerOffset.y * scaleMult.y;
                    float skew = gizmo.skew * (gizmo.space.FlagEnumContains(LocalSpace.Scale) ? targetTransform.localScale.x : 1);

                    // size
                    Vector2 size = gizmo.size;
                    if (gizmo.space.FlagEnumContains(LocalSpace.Scale))
                    {
                        if (gizmo.scaleSizeType == ScaleSizeType.Add)
                            size += (Vector2)targetTransform.localScale;
                        else
                            size *= targetTransform.localScale;
                    }

                    float angle = gizmo.angle;
                    if (gizmo.space.FlagEnumContains(LocalSpace.Rotation))
                    {
                        angle += targetTransform.rotation.eulerAngles.z;
                    }

                    if (!gizmo.inheritColor)
                    {
                        color = gizmo.color;
                    }
                    if (gizmo.layer != layer && !gizmo.inheritLayer)
                    {
                        layer = gizmo.layer;
                        drawActions.Add(() => GLGizmos.SetLayer(gizmo.layer));
                    }

                    Color gizmoColor = color;
                    if (gizmo.solid)
                    {
                        drawActions.Add(() => GLGizmos.DrawSolidTriangle(position, centerOffset, size.y, size.x, skew, angle, gizmoColor));
                    }
                    else
                    {
                        drawActions.Add(() => GLGizmos.DrawOpenTriangle(position, centerOffset, size.y, size.x, skew, angle, gizmoColor));
                    }
                }
                else if (gizmo.gizmoType == GizmoType.Collider)
                {
                    if (gizmo.collider2D == null)
                        return;

                    if (!gizmo.inheritColor)
                    {
                        color = gizmo.color;
                    }
                    if (gizmo.layer != layer && !gizmo.inheritLayer)
                    {
                        layer = gizmo.layer;
                        drawActions.Add(() => GLGizmos.SetLayer(gizmo.layer));
                    }

                    Color gizmoColor = color;
                    drawActions.Add(() => GLGizmos.DrawCollider2D(gizmo.collider2D, gizmo.solid, gizmoColor));
                }
            }
            drawActions.Add(() => GLGizmos.SetLayer(0));
        }

        public List<Action> GetDrawActions() => drawActions;
    }
}
