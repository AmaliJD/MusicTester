using GLGizmosExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace GLDebug
{
    #region ### Structs
    public enum ArcCloseType { None, Flat, Center, Edge }
    public enum BorderType { Centered, Outside, Inside }
    public enum PositionPivot
    {
        Center, Left, Right,
        Top, TopLeft, TopRight,
        Bottom, BottomLeft, BottomRight,
    }
    public struct TextBoxParams
    {
        public float rotation;
        public Vector2? scale;
        public Vector2 textBoxSize;
        public FontStyles fontStyle;

        public float characterSpacing;
        public float wordSpacing;
        public float lineSpacing;
        public float paragraphSpacing;

        public TextAlignmentOptions? alignment;
        public PositionPivot positionPivot;
        public bool fitTextToBox;
    }

    public struct BoxParams
    {
        public bool solid;
        public float rotation;

        public float edgeRadius;
        public bool onlyRenderEdgeRadius;
        public bool solidEdgeRadius;

        public float borderWidth;
        public BorderType borderType;
    }

    public struct CircleParams
    {
        public int numEdges;

        public bool solid;

        public float arcAngle;
        public float rotation;
        public ArcCloseType arcCloseType;

        public float borderWidth;
        public BorderType borderType;
    }
    #endregion

    [ExecuteInEditMode]
    public class GLGizmos : MonoBehaviour
    {
        private static Material GLmat;
        private static TextMeshPro tmp;
        private static Color? color = Color.white;
        private static Color? lastColorSet;
        private static int drawLayer = 0;
        private static Dictionary<int, List<Action>> drawActions = new();
        public static bool manualClearDrawActions = false;

        private static List<GLGizmosComponent> GLGizmoComponents = new();

        private const float Min_Max_Bias = 1;

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
            CreateGLMaterial();
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
            DestroyGLMaterial();
        }

        private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            OnPostRender();
        }

        private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            OnPreRender();
        }

        private void OnPreRender()
        {
            GL.wireframe = false;
        }

        private void OnPostRender()
        {
            GLmat.SetPass(0);
            GL.PushMatrix();

            foreach (var gizmoComponent in GLGizmoComponents)
            {
                gizmoComponent.OnTransformChange();
                foreach (Action action in gizmoComponent.GetDrawActions())
                {
                    action.Invoke();
                }
            }

            foreach (int index in drawActions.Keys.OrderBy(x => x))
            {
                foreach (var draw in drawActions[index])
                    draw.Invoke();
            }

            GL.PopMatrix();
            if (!manualClearDrawActions)
                drawActions.Clear();

            drawLayer = 0;
            GL.wireframe = false;
        }


        #region ### System
        /// <summary>
        /// Adds an action to draw actions
        /// </summary>
        private static void AddAction(Action action, int? layer = null)
        {
            if (layer == null)
                layer = drawLayer;

            if (!drawActions.ContainsKey(layer.Value))
                drawActions.Add(layer.Value, new());

            drawActions[layer.Value].Add(action);
        }

        private static string DrawActionsToString()
        {
            string output = "";
            foreach (int index in drawActions.Keys.OrderBy(x => x))
            {
                output += $"[{index}, {drawActions[index].Count}] ";
                foreach (var draw in drawActions[index])
                {
                    draw.Invoke();
                }
            }

            return output;
        }

        /// <summary>
        /// Clears the list of draw actions
        /// </summary>
        public static void ClearDrawActions() => drawActions.Clear();

        public static void AddGLGizmoComponent(GLGizmosComponent glgc)
        {
            if (!GLGizmoComponents.Contains(glgc))
                GLGizmoComponents.Add(glgc);
        }

        public static void RemoveGLGizmoComponent(GLGizmosComponent glgc)
        {
            if (GLGizmoComponents.Contains(glgc))
                GLGizmoComponents.Remove(glgc);
        }

        /// <summary>
        /// Sets the global color parameter of GLGizmos
        /// </summary>
        public static void SetColor(Color colorSet)
        {
            AddAction(() => InternalSetColor(colorSet));
            lastColorSet = colorSet;
        }
        private static void InternalSetColor(Color colorSet) => color = colorSet;

        /// <summary>
        /// Sets the global drawLayer parameter of GLGizmos
        /// </summary>
        public static void SetLayer(int layer)
        {
            InternalSetLayer(layer);

            if (lastColorSet != null)
                SetColor(lastColorSet.Value);
        }
        private static void InternalSetLayer(int layer) => drawLayer = layer;
        #endregion

        #region ### Rectangles
        /// <summary>
        /// Draws a box with the specified parameters
        /// </summary>
        public static void DrawBox(Vector2 position, Vector2 size, BoxParams boxParams, Color? colorSetting = null)
        {
            if (boxParams.edgeRadius == 0 && boxParams.borderWidth == 0)
                AddAction(() => InternalDrawRect(position, size, boxParams.rotation, boxParams.solid && !boxParams.onlyRenderEdgeRadius, colorSetting));
            else if (boxParams.edgeRadius == 0 && boxParams.borderWidth != 0)
            {
                int fillBox = 0;
                (size, boxParams.borderWidth, boxParams.borderType, fillBox) = AdjustWeightedBoxParams(size, boxParams.borderWidth, boxParams.borderType);

                if (!boxParams.onlyRenderEdgeRadius)
                {
                    if (fillBox > 0)
                    {
                        AddAction(() => InternalDrawRect(position, size, boxParams.rotation, true, colorSetting));

                        if (fillBox == 2)
                            return;
                    }

                    AddAction(() => InternalDrawWeightedRect(position, size, boxParams.rotation, boxParams.borderWidth, colorSetting));

                    if (size.x > 0 && size.y > 0 && boxParams.solid && fillBox == 0)
                        AddAction(() => InternalDrawRect(position, size, boxParams.rotation, boxParams.solid, colorSetting));
                }
                else
                {
                    AddAction(() => InternalDrawRect(position, size + Vector2.one * boxParams.borderWidth * 2, boxParams.rotation, false, colorSetting));
                }
            }
            else if (boxParams.edgeRadius != 0 && boxParams.borderWidth == 0)
            {
                AddAction(() => InternalDrawRectEdgeRadius(position, size, boxParams.edgeRadius, boxParams.rotation, false, boxParams.solidEdgeRadius, colorSetting: colorSetting));
            
                if (!boxParams.onlyRenderEdgeRadius)
                    AddAction(() => InternalDrawRect(position, size, boxParams.rotation, boxParams.solid, colorSetting));
            }
            else if (boxParams.edgeRadius != 0 && boxParams.borderWidth != 0)
            {
                int fillBox = 0;
                (size, boxParams.borderWidth, boxParams.borderType, fillBox) = AdjustWeightedBoxParams(size, boxParams.borderWidth, boxParams.borderType);

                if (!boxParams.onlyRenderEdgeRadius)
                {
                    if (fillBox > 0)
                    {
                        AddAction(() => InternalDrawRect(position, size, boxParams.rotation, true, colorSetting));

                        if (fillBox == 2)
                        {
                            AddAction(() => InternalDrawRectEdgeRadius(position, size, boxParams.edgeRadius, boxParams.rotation, false, boxParams.solidEdgeRadius, colorSetting: colorSetting));
                            return;
                        }
                    }

                    AddAction(() => InternalDrawWeightedRect(position, size, boxParams.rotation, boxParams.borderWidth, colorSetting));

                    if (size.x > 0 && size.y > 0 && boxParams.solid && fillBox == 0)
                        AddAction(() => InternalDrawRect(position, size, boxParams.rotation, boxParams.solid, colorSetting));
                }

                if (fillBox != 2)
                    AddAction(() => InternalDrawRectEdgeRadius(position, size + Vector2.one * boxParams.borderWidth * 2, boxParams.edgeRadius, boxParams.rotation, false, boxParams.solidEdgeRadius, colorSetting: colorSetting));
                else
                    AddAction(() => InternalDrawRectEdgeRadius(position, size, boxParams.edgeRadius, boxParams.rotation, false, boxParams.solidEdgeRadius, colorSetting: colorSetting));
            }
        }

        /// <summary>
        /// Draws an open box at 'position' with 'size'
        /// </summary>
        public static void DrawOpenBox(Vector2 position, Vector2 size, Color? colorSetting = null)
            => AddAction(() => InternalDrawBox(position, size, false, colorSetting));

        /// <summary>
        /// Draws a solid box at 'position' with 'size'
        /// </summary>
        public static void DrawSolidBox(Vector2 position, Vector2 size, Color? colorSetting = null)
            => AddAction(() => InternalDrawBox(position, size, true, colorSetting));

        private static void InternalDrawBox(Vector2 position, Vector2 size, bool solid, Color? colorSetting = null)
        {
            GL.wireframe = !solid;
            GL.Begin(GL.wireframe ? GL.LINES : GL.QUADS);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));

            int signX = -1, signY = -1;
            bool flipY = true;
            for (int i = 0; i < 4; i++)
            {
                GL.Vertex(position + new Vector2(signX * size.x / 2, signY * size.y / 2));

                if (flipY)
                    signY *= -1;
                else
                    signX *= -1;

                if (GL.wireframe)
                    GL.Vertex(position + new Vector2(signX * size.x / 2, signY * size.y / 2));

                flipY = !flipY;
            }

            GL.End();
        }


        /// <summary>
        /// Draws multiple open boxes at 'positions' with 'size'
        /// </summary>
        /// <param name="colors">optional list of colors to cycle through</param>
        public static void DrawOpenBoxes(List<Vector2> positions, Vector2 size, List<Color> colors = null)
            => AddAction(() => InternalDrawBoxes(positions, size, false, colors));

        /// <summary>
        /// Draws multiple solid boxes at 'positions' with 'size'
        /// </summary>
        /// <param name="colors">optional list of colors to cycle through</param>
        public static void DrawSolidBoxes(List<Vector2> positions, Vector2 size, List<Color> colors = null)
            => AddAction(() => InternalDrawBoxes(positions, size, true, colors));

        private static void InternalDrawBoxes(List<Vector2> positions, Vector2 size, bool solid, List<Color> colors = null)
        {
            if (positions == null || positions.Count == 0)
                return;

            GL.wireframe = !solid;
            bool noColor = colors == null || colors.Count == 0;
            int i = 0;
            foreach (Vector2 position in positions)
            {
                InternalDrawBox(position, size, solid, noColor ? color.Value : colors[i % colors.Count]);

                i++;
            }
        }


        /// <summary>
        /// Draws a box at 'center' with 'size' and tiled with an open box grid of dimensions 'columns' x 'rows'
        /// </summary>
        /// <param name="colors">optional 2D array of colors to cycle through. Each element is a wrappable list of colors for each row</param>
        public static void DrawOpenBoxGrid(Vector2 center, Vector2 size, float columns, float rows, Color[,] colors = null)
            => AddAction(() => InternalDrawBoxGrid(center, size.x, size.y, new Vector2(columns, rows), false, colors));

        /// <summary>
        /// Draws a box at 'center' with 'size' and tiled with an solid box grid of dimensions 'columns' x 'rows'
        /// </summary>
        /// <param name="colors">optional 2D array of colors to cycle through. Each element is a wrappable list of colors for each row</param>
        public static void DrawSolidBoxGrid(Vector2 center, Vector2 size, float columns, float rows, Color[,] colors = null)
            => AddAction(() => InternalDrawBoxGrid(center, size.x, size.y, new Vector2(columns, rows), true, colors));

        private static void InternalDrawBoxGrid(Vector2 center, float width, float height, Vector2 arrayDimensions, bool solid, Color[,] colors = null)
        {
            if (arrayDimensions.x == 0 || arrayDimensions.y == 0)
                return;

            float boxWidth = width / arrayDimensions.x;
            float boxHeight = height / arrayDimensions.y;
            Vector2 origin = center - new Vector2(width / 2, height / 2) + new Vector2(boxWidth / 2, boxHeight / 2);

            for (int i = 0; i < arrayDimensions.x; i++)
            {
                for (int j = 0; j < arrayDimensions.y; j++)
                {
                    InternalDrawBox(origin + new Vector2(i * boxWidth, j * boxHeight), new Vector2(boxWidth, boxHeight), solid, colors == null ? null : colors[i % colors.GetLength(0), j % colors.GetLength(1)]);
                }
            }
        }


        /// <summary>
        /// Draws an edge radius outline around a box at 'position' with 'size'
        /// </summary>
        /// <param name="drawBox">draw an open box at 'position'</param>
        /// <param name="borderType">sets if the edge radius extends beyond the box size (outside), is constrained within the box size (inside), or half of either side (centered)</param>
        public static void DrawOpenBoxEdgeRadius(Vector2 position, Vector2 size, float edgeRadius, bool drawBox, BorderType borderType = BorderType.Outside, Color? colorSetting = null)
            => AddAction(() => InternalDrawBoxEdgeRadius(position, size, edgeRadius, drawBox, false, borderType, colorSetting));

        /// <summary>
        /// Fills in an edge radius area around a box at 'position' with 'size'
        /// </summary>
        /// <param name="drawBox">draw a solid box at 'position'</param>
        /// <param name="borderType">sets if the edge radius extends beyond the box size (outside), is constrained within the box size (inside), or half of either side (centered)</param>
        public static void DrawSolidBoxEdgeRadius(Vector2 position, Vector2 size, float edgeRadius, bool drawBox, BorderType borderType = BorderType.Outside, Color? colorSetting = null)
            => AddAction(() => InternalDrawBoxEdgeRadius(position, size, edgeRadius, drawBox, true, borderType, colorSetting));

        private static void InternalDrawBoxEdgeRadius(Vector2 position, Vector2 size, float edgeRadius, bool drawBox, bool solid, BorderType borderType = BorderType.Outside, Color? colorSetting = null)
        {
            size = size.Abs();

            if (edgeRadius < 0)
            {
                edgeRadius = -edgeRadius;
                borderType = borderType switch
                {
                    BorderType.Outside => BorderType.Inside,
                    BorderType.Inside => BorderType.Outside,
                    BorderType.Centered => BorderType.Centered,
                    _ => BorderType.Outside
                };
            }

            float minXY = Mathf.Min(size.x, size.y);
            switch (borderType)
            {
                case BorderType.Inside:
                    if (edgeRadius > minXY / 2)
                        edgeRadius = minXY / 2;

                    size -= Vector2.one * edgeRadius * 2;
                    break;
                case BorderType.Centered:
                    if (edgeRadius > minXY)
                    {
                        float difference = edgeRadius - minXY;
                        edgeRadius = minXY + difference / 2;
                    }

                    size -= Vector2.one * edgeRadius;
                    break;
            }

            if (size.x < 0)
                size.x = 0;

            if (size.y < 0)
                size.y = 0;

            if (!solid)
                InternalDrawOpenBoxEdgeRadius(position, size, edgeRadius, drawBox, colorSetting);
            else
                InternalDrawSolidBoxEdgeRadius(position, size, edgeRadius, drawBox, colorSetting);
        }

        private static void InternalDrawOpenBoxEdgeRadius(Vector2 position, Vector2 size, float edgeRadius, bool drawBox, Color? colorSetting = null)
        {
            GL.wireframe = false;
            GL.Begin(GL.LINE_STRIP);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));

            int num = 8;
            Vector2 halfSize = new Vector2(size.x / 2, size.y / 2);
            Vector2 topRight = position + halfSize.ScaleEach(1, 1);
            Vector2 topLeft = position + halfSize.ScaleEach(-1, 1);
            Vector2 bottomLeft = position + halfSize.ScaleEach(-1, -1);
            Vector2 bottomRight = position + halfSize.ScaleEach(1, -1);

            Vector2 vectorTopRight = topRight + Vector2.up * edgeRadius;
            Vector2 vectorTopLeft = topLeft + Vector2.up * edgeRadius;

            Vector2 vectorLeftUp = topLeft + Vector2.left * edgeRadius;
            Vector2 vectorLeftDown = bottomLeft + Vector2.left * edgeRadius;

            Vector2 vectorBottomLeft = bottomLeft + Vector2.down * edgeRadius;
            Vector2 vectorBottomRight = bottomRight + Vector2.down * edgeRadius;

            Vector2 vectorRightDown = bottomRight + Vector2.right * edgeRadius;
            Vector2 vectorRightUp = topRight + Vector2.right * edgeRadius;

            GL.Vertex(vectorTopRight);
            GL.Vertex(vectorTopLeft);
            for (int i = 0; i <= num; i++)
            {
                GL.Vertex((Vector2.up.Rotate(90f * ((float)i / (float)num)) * edgeRadius) + topLeft);
            }

            GL.Vertex(vectorLeftUp);
            GL.Vertex(vectorLeftDown);
            for (int i = 0; i <= num; i++)
            {
                GL.Vertex((Vector2.left.Rotate(90f * ((float)i / (float)num)) * edgeRadius) + bottomLeft);
            }

            GL.Vertex(vectorBottomLeft);
            GL.Vertex(vectorBottomRight);
            for (int i = 0; i <= num; i++)
            {
                GL.Vertex((Vector2.down.Rotate(90f * ((float)i / (float)num)) * edgeRadius) + bottomRight);
            }

            GL.Vertex(vectorRightDown);
            GL.Vertex(vectorRightUp);
            for (int i = 0; i <= num; i++)
            {
                GL.Vertex((Vector2.right.Rotate(90f * ((float)i / (float)num)) * edgeRadius) + topRight);
            }

            GL.End();

            if (drawBox)
                InternalDrawBox(position, size, false, colorSetting);
        }

        private static void InternalDrawSolidBoxEdgeRadius(Vector2 position, Vector2 size, float edgeRadius, bool drawBox, Color? colorSetting = null)
        {
            Vector2 halfSize = new Vector2(size.x / 2, size.y / 2);
            Vector2 topRight = position + halfSize.ScaleEach(1, 1);
            Vector2 topLeft = position + halfSize.ScaleEach(-1, 1);
            Vector2 bottomLeft = position + halfSize.ScaleEach(-1, -1);
            Vector2 bottomRight = position + halfSize.ScaleEach(1, -1);

            Vector2 vectorTopRight = topRight + Vector2.up * edgeRadius;
            Vector2 vectorTopLeft = topLeft + Vector2.up * edgeRadius;

            Vector2 vectorLeftUp = topLeft + Vector2.left * edgeRadius;
            Vector2 vectorLeftDown = bottomLeft + Vector2.left * edgeRadius;

            Vector2 vectorBottomLeft = bottomLeft + Vector2.down * edgeRadius;
            Vector2 vectorBottomRight = bottomRight + Vector2.down * edgeRadius;

            Vector2 vectorRightDown = bottomRight + Vector2.right * edgeRadius;
            Vector2 vectorRightUp = topRight + Vector2.right * edgeRadius;

            InternalDrawBox(new Vector2(position.x, position.y + halfSize.y + edgeRadius / 2), new Vector2(size.x, edgeRadius), true, colorSetting);
            InternalDrawBox(new Vector2(position.x, position.y - halfSize.y - edgeRadius / 2), new Vector2(size.x, edgeRadius), true, colorSetting);
            InternalDrawBox(new Vector2(position.x + halfSize.x + edgeRadius / 2, position.y), new Vector2(edgeRadius, size.y), true, colorSetting);
            InternalDrawBox(new Vector2(position.x - halfSize.x - edgeRadius / 2, position.y), new Vector2(edgeRadius, size.y), true, colorSetting);
            InternalDrawCircle(topRight, edgeRadius, 90, 0, -2, true, ArcCloseType.Center, colorSetting);
            InternalDrawCircle(topLeft, edgeRadius, 90, 90, -2, true, ArcCloseType.Center, colorSetting);
            InternalDrawCircle(bottomLeft, edgeRadius, 90, 180, -2, true, ArcCloseType.Center, colorSetting);
            InternalDrawCircle(bottomRight, edgeRadius, 90, 270, -2, true, ArcCloseType.Center, colorSetting);

            if (drawBox)
                InternalDrawBox(position, size, true, colorSetting);
        }


        /// <summary>
        /// Draws an edge radius outline around a rect rotated by 'angle' at 'position' with 'size'
        /// </summary>
        /// <param name="drawBox">draw an open rect at 'position'</param>
        /// <param name="borderType">sets if the edge radius extends beyond the rect size (outside), is constrained within the box size (inside), or half of either side (centered)</param>
        public static void DrawOpenRectEdgeRadius(Vector2 position, Vector2 size, float edgeRadius, float angle, bool drawBox, BorderType borderType = BorderType.Outside, Color? colorSetting = null)
            => AddAction(() => InternalDrawRectEdgeRadius(position, size, edgeRadius, angle, drawBox, false, borderType, colorSetting));

        /// <summary>
        /// Fills in an edge radius area around a rect rotated by 'angle' at 'position' with 'size'
        /// </summary>
        /// <param name="drawBox">draw a solid rect at 'position'</param>
        /// <param name="borderType">sets if the edge radius extends beyond the rect size (outside), is constrained within the box size (inside), or half of either side (centered)</param>
        public static void DrawSolidRectEdgeRadius(Vector2 position, Vector2 size, float edgeRadius, float angle, bool drawBox, BorderType borderType = BorderType.Outside, Color? colorSetting = null)
            => AddAction(() => InternalDrawRectEdgeRadius(position, size, edgeRadius, angle, drawBox, true, borderType, colorSetting));

        private static void InternalDrawRectEdgeRadius(Vector2 position, Vector2 size, float edgeRadius, float angle, bool drawBox, bool solid, BorderType borderType = BorderType.Outside, Color? colorSetting = null)
        {
            size = size.Abs();

            if (edgeRadius < 0)
            {
                edgeRadius = -edgeRadius;
                borderType = borderType switch
                {
                    BorderType.Outside => BorderType.Inside,
                    BorderType.Inside => BorderType.Outside,
                    BorderType.Centered => BorderType.Centered,
                    _ => BorderType.Outside
                };
            }

            float minXY = Mathf.Min(size.x, size.y);
            switch (borderType)
            {
                case BorderType.Inside:
                    if (edgeRadius > minXY / 2)
                        edgeRadius = minXY / 2;

                    size -= Vector2.one * edgeRadius * 2;
                    break;
                case BorderType.Centered:
                    if (edgeRadius > minXY)
                    {
                        float difference = edgeRadius - minXY;
                        edgeRadius = minXY + difference / 2;
                    }

                    size -= Vector2.one * edgeRadius;
                    break;
            }

            if (size.x < 0)
                size.x = 0;

            if (size.y < 0)
                size.y = 0;

            if (!solid)
                InternalDrawOpenRectEdgeRadius(position, size, edgeRadius, angle, drawBox, colorSetting);
            else
                InternalDrawSolidRectEdgeRadius(position, size, edgeRadius, angle, drawBox, colorSetting);
        }

        private static void InternalDrawOpenRectEdgeRadius(Vector2 position, Vector2 size, float edgeRadius, float angle, bool drawBox, Color? colorSetting = null)
        {
            GL.wireframe = false;
            GL.Begin(GL.LINE_STRIP);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));

            int num = 8;
            Vector2 halfSize = new Vector2(size.x / 2, size.y / 2);
            Vector2 topRight = position + halfSize.ScaleEach(1, 1).Rotate(angle);
            Vector2 topLeft = position + halfSize.ScaleEach(-1, 1).Rotate(angle);
            Vector2 bottomLeft = position + halfSize.ScaleEach(-1, -1).Rotate(angle);
            Vector2 bottomRight = position + halfSize.ScaleEach(1, -1).Rotate(angle);

            Vector2 vectorTopRight = topRight + Vector2.up.Rotate(angle) * edgeRadius;
            Vector2 vectorTopLeft = topLeft + Vector2.up.Rotate(angle) * edgeRadius;

            Vector2 vectorLeftUp = topLeft + Vector2.left.Rotate(angle) * edgeRadius;
            Vector2 vectorLeftDown = bottomLeft + Vector2.left.Rotate(angle) * edgeRadius;

            Vector2 vectorBottomLeft = bottomLeft + Vector2.down.Rotate(angle) * edgeRadius;
            Vector2 vectorBottomRight = bottomRight + Vector2.down.Rotate(angle) * edgeRadius;

            Vector2 vectorRightDown = bottomRight + Vector2.right.Rotate(angle) * edgeRadius;
            Vector2 vectorRightUp = topRight + Vector2.right.Rotate(angle) * edgeRadius;

            GL.Vertex(vectorTopRight);
            GL.Vertex(vectorTopLeft);
            for (int i = 0; i <= num; i++)
            {
                GL.Vertex((Vector2.up.Rotate(90f * ((float)i / (float)num) + angle) * edgeRadius) + topLeft);
            }

            GL.Vertex(vectorLeftUp);
            GL.Vertex(vectorLeftDown);
            for (int i = 0; i <= num; i++)
            {
                GL.Vertex((Vector2.left.Rotate(90f * ((float)i / (float)num) + angle) * edgeRadius) + bottomLeft);
            }

            GL.Vertex(vectorBottomLeft);
            GL.Vertex(vectorBottomRight);
            for (int i = 0; i <= num; i++)
            {
                GL.Vertex((Vector2.down.Rotate(90f * ((float)i / (float)num) + angle) * edgeRadius) + bottomRight);
            }

            GL.Vertex(vectorRightDown);
            GL.Vertex(vectorRightUp);
            for (int i = 0; i <= num; i++)
            {
                GL.Vertex((Vector2.right.Rotate(90f * ((float)i / (float)num) + angle) * edgeRadius) + topRight);
            }

            GL.End();

            if (drawBox)
                InternalDrawRect(position, size, angle, false, colorSetting);
        }

        private static void InternalDrawSolidRectEdgeRadius(Vector2 position, Vector2 size, float edgeRadius, float angle, bool drawBox, Color? colorSetting = null)
        {
            Vector2 halfSize = new Vector2(size.x / 2, size.y / 2);
            Vector2 topRight = position + halfSize.ScaleEach(1, 1).Rotate(angle);
            Vector2 topLeft = position + halfSize.ScaleEach(-1, 1).Rotate(angle);
            Vector2 bottomLeft = position + halfSize.ScaleEach(-1, -1).Rotate(angle);
            Vector2 bottomRight = position + halfSize.ScaleEach(1, -1).Rotate(angle);

            Vector2 vectorTopRight = topRight + Vector2.up.Rotate(angle) * edgeRadius;
            Vector2 vectorTopLeft = topLeft + Vector2.up.Rotate(angle) * edgeRadius;

            Vector2 vectorLeftUp = topLeft + Vector2.left.Rotate(angle) * edgeRadius;
            Vector2 vectorLeftDown = bottomLeft + Vector2.left.Rotate(angle) * edgeRadius;

            Vector2 vectorBottomLeft = bottomLeft + Vector2.down.Rotate(angle) * edgeRadius;
            Vector2 vectorBottomRight = bottomRight + Vector2.down.Rotate(angle) * edgeRadius;

            Vector2 vectorRightDown = bottomRight + Vector2.right.Rotate(angle) * edgeRadius;
            Vector2 vectorRightUp = topRight + Vector2.right.Rotate(angle) * edgeRadius;

            Vector2 upDirection = Vector2.up.Rotate(angle);
            Vector2 rightDirection = Vector2.right.Rotate(angle);

            InternalDrawRect(new Vector2(position.x, position.y) + upDirection * (halfSize.y + edgeRadius / 2), new Vector2(size.x, edgeRadius), angle, true, colorSetting);
            InternalDrawRect(new Vector2(position.x, position.y) - upDirection * (halfSize.y + edgeRadius / 2), new Vector2(size.x, edgeRadius), angle, true, colorSetting);
            InternalDrawRect(new Vector2(position.x, position.y) + rightDirection * (halfSize.x + edgeRadius / 2), new Vector2(edgeRadius, size.y), angle, true, colorSetting);
            InternalDrawRect(new Vector2(position.x, position.y) - rightDirection * (halfSize.x + edgeRadius / 2), new Vector2(edgeRadius, size.y), angle, true, colorSetting);
            InternalDrawCircle(topRight, edgeRadius, 90, 0 + angle, -2, true, ArcCloseType.Center, colorSetting);
            InternalDrawCircle(topLeft, edgeRadius, 90, 90 + angle, -2, true, ArcCloseType.Center, colorSetting);
            InternalDrawCircle(bottomLeft, edgeRadius, 90, 180 + angle, -2, true, ArcCloseType.Center, colorSetting);
            InternalDrawCircle(bottomRight, edgeRadius, 90, 270 + angle, -2, true, ArcCloseType.Center, colorSetting);

            if (drawBox)
                InternalDrawRect(position, size, angle, true, colorSetting);
        }


        /// <summary>
        /// Draws an open rectangle at 'position' with 'size' rotated by 'angle'
        /// </summary>
        public static void DrawOpenRect(Vector2 position, Vector2 size, float angle, Color? colorSetting = null)
            => AddAction(() => InternalDrawRect(position, size, angle, false, colorSetting));

        /// <summary>
        /// Draws a solid rectangle at 'position' with 'size' rotated by 'angle'
        /// </summary>
        public static void DrawSolidRect(Vector2 position, Vector2 size, float angle, Color? colorSetting = null)
                => AddAction(() => InternalDrawRect(position, size, angle, true, colorSetting));

        private static void InternalDrawRect(Vector2 position, Vector2 size, float angle, bool solid, Color? colorSetting = null)
        {
            List<Vector2> points = new List<Vector2>();
            points.Add(new Vector2(size.x / 2, size.y / 2).Rotate(angle) + position);
            points.Add(new Vector2(-size.x / 2, size.y / 2).Rotate(angle) + position);
            points.Add(new Vector2(-size.x / 2, -size.y / 2).Rotate(angle) + position);
            points.Add(new Vector2(size.x / 2, -size.y / 2).Rotate(angle) + position);

            if (!solid)
                InternalDrawPath(points, true, colorSetting);
            else
                InternalDrawFilledPath(points, colorSetting);
        }


        /// <summary>
        /// Draws a box with an edge thickness of 'borderWidth'
        /// </summary>
        public static void DrawWeightedBox(Vector2 position, Vector2 size, float borderWidth, BorderType borderType, Color? colorSetting = null)
        {
            if (borderWidth == 0)
            {
                AddAction(() => InternalDrawBox(position, size, false, colorSetting));
                return;
            }

            int fillBox = 0;
            (size, borderWidth, borderType, fillBox) = AdjustWeightedBoxParams(size, borderWidth, borderType);

            if (fillBox > 0)
            {
                AddAction(() => InternalDrawBox(position, size, true, colorSetting));

                if (fillBox == 2)
                    return;
            }

            AddAction(() => InternalDrawWeightedBox(position, size, borderWidth, colorSetting));
        }

        /// <summary>
        /// Draws a box bounded by corner1 and corner2 with an edge thickness of 'borderWidth'
        /// </summary>
        public static void DrawWeightedBox2(Vector2 corner1, Vector2 corner2, float borderWidth, BorderType borderType, Color? colorSetting = null)
        {
            Vector2 position = new Vector2((corner2.x + corner1.x) / 2, (corner2.y + corner1.y) / 2);
            Vector2 size = new Vector2(corner2.x - corner1.x, corner2.y - corner1.y).Abs();

            if (borderWidth == 0)
            {
                AddAction(() => InternalDrawBox(position, size, false, colorSetting));
                return;
            }
            int fillBox = 0;
            (size, borderWidth, borderType, fillBox) = AdjustWeightedBoxParams(size, borderWidth, borderType);

            if (fillBox > 0)
            {
                AddAction(() => InternalDrawBox(position, size, true, colorSetting));

                if (fillBox == 2)
                    return;
            }

            AddAction(() => InternalDrawWeightedBox(position, size, borderWidth, colorSetting));
        }

        private static void InternalDrawWeightedBox(Vector2 position, Vector2 innerSize, float borderWidth, Color? colorSetting = null)
        {
            InternalDrawBox(position + Vector2.up * ((innerSize.y / 2) + (borderWidth / 2)), new Vector2(innerSize.x + (2 * borderWidth), borderWidth), true, colorSetting);
            InternalDrawBox(position - Vector2.up * ((innerSize.y / 2) + (borderWidth / 2)), new Vector2(innerSize.x + (2 * borderWidth), borderWidth), true, colorSetting);
            InternalDrawBox(position + Vector2.right * ((innerSize.x / 2) + (borderWidth / 2)), new Vector2(borderWidth, innerSize.y), true, colorSetting);
            InternalDrawBox(position - Vector2.right * ((innerSize.x / 2) + (borderWidth / 2)), new Vector2(borderWidth, innerSize.y), true, colorSetting);
        }

        //public static Vector2 GetWeightedBoxAdjustedSize(Vector2 size, float borderWidth, BorderType borderType)
        //{
        //    int fillBox = 0;
        //    (size, borderWidth, borderType, fillBox) = AdjustWeightedBoxParams(size, borderWidth, borderType);
        //    return size;
        //}


        /// <summary>
        /// Draws a rect with an edge thickness of 'borderWidth' rotated by 'angle'
        /// </summary>
        public static void DrawWeightedRect(Vector2 position, Vector2 size, float angle, float borderWidth, BorderType borderType, Color? colorSetting = null)
        {
            if (borderWidth == 0)
            {
                AddAction(() => InternalDrawRect(position, size, angle, false, colorSetting));
                return;
            }

            int fillBox = 0;
            (size, borderWidth, borderType, fillBox) = AdjustWeightedBoxParams(size, borderWidth, borderType);

            if (fillBox > 0)
            {
                AddAction(() => InternalDrawRect(position, size, angle, true, colorSetting));

                if (fillBox == 2)
                    return;
            }

            AddAction(() => InternalDrawWeightedRect(position, size, angle, borderWidth, colorSetting));
        }
        private static void InternalDrawWeightedRect(Vector2 position, Vector2 innerSize, float angle, float borderWidth, Color? colorSetting = null)
        {
            innerSize /= 2;

            Vector2 innerTL = position + new Vector2(-innerSize.x, innerSize.y).Rotate(angle);
            Vector2 innerTR = position + new Vector2(innerSize.x, innerSize.y).Rotate(angle);
            Vector2 innerBL = position + new Vector2(-innerSize.x, -innerSize.y).Rotate(angle);
            Vector2 innerBR = position + new Vector2(innerSize.x, -innerSize.y).Rotate(angle);

            Vector2 outerTL = position + (new Vector2(-innerSize.x, innerSize.y) + new Vector2(-borderWidth, borderWidth)).Rotate(angle);
            Vector2 outerTR = position + (new Vector2(innerSize.x, innerSize.y) + new Vector2(borderWidth, borderWidth)).Rotate(angle);
            Vector2 outerBL = position + (new Vector2(-innerSize.x, -innerSize.y) + new Vector2(-borderWidth, -borderWidth)).Rotate(angle);
            Vector2 outerBR = position + (new Vector2(innerSize.x, -innerSize.y) + new Vector2(borderWidth, -borderWidth)).Rotate(angle);

            InternalDrawPolygon(new List<Vector2>() { innerTL, outerTL, outerTR, innerTR }, true, colorSetting);
            InternalDrawPolygon(new List<Vector2>() { innerTR, outerTR, outerBR, innerBR }, true, colorSetting);
            InternalDrawPolygon(new List<Vector2>() { innerBR, outerBR, outerBL, innerBL }, true, colorSetting);
            InternalDrawPolygon(new List<Vector2>() { innerBL, outerBL, outerTL, innerTL }, true, colorSetting);
        }


        private static (Vector2, float, BorderType, int) AdjustWeightedBoxParams(Vector2 size, float borderWidth, BorderType borderType)
        {
            int fillBox = 0; // 0 - no, 1 - yes, 2 - only
            size = size.Abs();

            // handle negative borderWidths
            if (borderWidth <= 0)
            {
                borderWidth = -borderWidth;
                borderType = borderType switch
                {
                    BorderType.Outside => BorderType.Inside,
                    BorderType.Inside => BorderType.Outside,
                    BorderType.Centered => BorderType.Centered,
                    _ => BorderType.Outside
                };
            }

            // handle too large borderWidths
            switch (borderType)
            {
                case BorderType.Inside:
                    if (borderWidth > size.x / 2 || borderWidth > size.y / 2)
                    {
                        fillBox = 2;
                        return (size, borderWidth, borderType, fillBox);
                    }
                    break;
                case BorderType.Centered:
                    if (borderWidth > size.x || borderWidth > size.y)
                    {
                        fillBox = 1;

                        borderWidth = borderWidth / 2;
                        borderType = BorderType.Outside;
                    }
                    break;
            }

            // adjust size to become innerSize
            size = borderType switch
            {
                BorderType.Centered => size - Vector2.one * borderWidth,
                BorderType.Inside => size - Vector2.one * borderWidth * 2,
                _ => size,
            };
            size = size.Abs();

            return (size, borderWidth, borderType, fillBox);
        }
        #endregion

        #region ### Circles
        /// <summary>
        /// Returns the positions of each vertex of a circle with 'numEdges'
        /// </summary>
        public static List<Vector2> GetCircleVertices(Vector2 position, float radius, int numEdges = 0)
            => InternalGetCircleVertices(position, radius, 360, 0, numEdges);

        /// <summary>
        /// Returns the positions of each vertex of an arc with 'numEdges'
        /// </summary>
        public static List<Vector2> GetArcVertices(Vector2 position, float radius, float arcAngle, float offsetAngle, int numEdges = 0)
            => InternalGetCircleVertices(position, radius, arcAngle, offsetAngle, numEdges);

        private static List<Vector2> InternalGetCircleVertices(Vector2 position, float radius, float arcAngle, float offsetAngle, int numEdges)
        {
            radius = Mathf.Abs(radius);
            int defaultMult = numEdges >= 0 ? 1 : Mathf.Abs(numEdges);

            if (numEdges <= 0)
                numEdges = (int)(12 * Mathf.Sqrt(radius * 2) / (360f / Mathf.Abs(arcAngle))) * defaultMult;

            List<Vector2> vertices = new();

            for (int i = 0; i <= numEdges; i++)
                vertices.Add(position + Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * radius);

            return vertices;
        }

        public static void DrawCircle(Vector2 position, float radius, CircleParams circleParams, Color? colorSetting = null)
        {
            if (circleParams.borderWidth < 0)
            {
                circleParams.borderWidth = -circleParams.borderWidth;
                circleParams.borderType = circleParams.borderType switch
                {
                    BorderType.Inside => BorderType.Outside,
                    BorderType.Outside => BorderType.Inside,
                    BorderType.Centered => BorderType.Centered,
                    _ => circleParams.borderType
                };
            }

            float innerRadius = 0;
            float outerRadius = radius;

            switch (circleParams.borderType)
            {
                case BorderType.Outside:
                    innerRadius = radius;
                    outerRadius = radius + circleParams.borderWidth;
                    break;
                case BorderType.Inside:
                    outerRadius = radius;
                    innerRadius = Mathf.Max(radius - circleParams.borderWidth, 0);
                    break;
                case BorderType.Centered:
                    outerRadius = radius + circleParams.borderWidth / 2;
                    innerRadius = Mathf.Max(radius - circleParams.borderWidth / 2, 0);
                    break;
            }

            AddAction(() => InternalDrawWeightedCircle(position, circleParams.solid ? 0 : innerRadius, outerRadius, circleParams.arcAngle, circleParams.rotation, circleParams.arcCloseType, circleParams.numEdges, colorSetting));
        }

        /// <summary>
        /// Draws an open circle at 'position' with 'radius' by drawing a polygon with 'numEdges' (automatically calculated if 0)
        /// </summary>
        public static void DrawOpenCircle(Vector2 position, float radius, int numEdges = 0, Color? colorSetting = null)
            => AddAction(() => InternalDrawEdgeCircle(position, radius, 360, 0, numEdges, false, ArcCloseType.None, colorSetting));

        /// <summary>
        /// Draws an open arc with 'angle' at 'position' with 'radius' by drawing a polygon with 'numEdges' (automatically calculated if 0)
        /// </summary>
        public static void DrawOpenArc(Vector2 position, float radius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType = ArcCloseType.None, int numEdges = 0, Color? colorSetting = null)
            => AddAction(() => InternalDrawEdgeCircle(position, radius, arcAngle, offsetAngle, numEdges, false, arcCloseType, colorSetting));

        /// <summary>
        /// Draws an open circle at 'position' with 'radius' by drawing a polygon with 'numEdges', drawing only half the edges for a dashed effect
        /// </summary>
        public static void DrawDashedCircle(Vector2 position, float radius, int numEdges = 0, Color? colorSetting = null)
        => AddAction(() => InternalDrawEdgeCircle(position, radius, 360, 0, numEdges, true, ArcCloseType.None, colorSetting));

        private static void InternalDrawCircle(Vector2 position, float radius, float arcAngle, float offsetAngle, int numEdges, bool solid, ArcCloseType arcCloseType = ArcCloseType.None, Color? colorSetting = null)
        {
            if (solid)
                InternalDrawFilledCircle(position, radius, arcAngle, offsetAngle, numEdges, ArcCloseType.None, colorSetting);
            else
                InternalDrawEdgeCircle(position, radius, arcAngle, offsetAngle, numEdges, false, ArcCloseType.None, colorSetting);
        }

        private static void InternalDrawEdgeCircle(Vector2 position, float radius, float arcAngle, float offsetAngle, int numEdges, bool dashed, ArcCloseType arcCloseType = ArcCloseType.None, Color? colorSetting = null)
        {
            GL.wireframe = true;
            GL.Begin(!dashed ? GL.LINE_STRIP : GL.LINES);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));

            radius = Mathf.Abs(radius);
            int defaultMult = numEdges >= 0 ? 1 : Mathf.Abs(numEdges);

            if (numEdges <= 0)
                numEdges = (int)(12 * Mathf.Sqrt(radius * 2) / (360f / Mathf.Abs(arcAngle))) * defaultMult;

            for (int i = 0; i <= numEdges; i++)
                GL.Vertex(position + Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * radius);

            switch (arcCloseType)
            {
                case ArcCloseType.Flat:
                    GL.Vertex(position + Vector2.right.Rotate(offsetAngle) * radius);
                    break;
                case ArcCloseType.Center:
                    GL.Vertex(position);
                    GL.Vertex(position + Vector2.right.Rotate(offsetAngle) * radius);
                    break;
                case ArcCloseType.Edge:
                    GL.Vertex(EdgeMinMaxPoint(position, -Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * Min_Max_Bias, position + Vector2.right.Rotate(offsetAngle) * radius, position + Vector2.right.Rotate(arcAngle + offsetAngle) * radius, arcAngle));
                    GL.Vertex(position + Vector2.right.Rotate(offsetAngle) * radius);
                    break;

            }

            GL.End();
        }

        /// <summary>
        /// Approximates a solid circle at 'position' with 'radius' by drawing a polygon with 'numEdges' (automatically calculated if 0)
        /// </summary>
        public static void DrawSolidCircle(Vector2 position, float radius, int numEdges = 0, Color? colorSetting = null)
        => AddAction(() => InternalDrawFilledCircle(position, radius, 360, 0, numEdges, ArcCloseType.None, colorSetting));

        /// <summary>
        /// Draws a solid arc with 'angle' at 'position' with 'radius' by drawing a polygon with 'numEdges' (automatically calculated if 0)
        /// </summary>
        public static void DrawSolidArc(Vector2 position, float radius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType = ArcCloseType.Center, int numEdges = 0, Color? colorSetting = null)
        => AddAction(() => InternalDrawFilledCircle(position, radius, arcAngle, offsetAngle, numEdges, arcCloseType, colorSetting));

        private static void InternalDrawFilledCircle(Vector2 position, float radius, float arcAngle, float offsetAngle, int numEdges, ArcCloseType arcCloseType = ArcCloseType.Center, Color? colorSetting = null)
        {
            GL.wireframe = false;
            GL.Begin(GL.TRIANGLES);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));

            radius = Mathf.Abs(radius);
            int defaultMult = numEdges >= 0 ? 1 : Mathf.Abs(numEdges);

            if (numEdges <= 0)
                numEdges = (int)(12 * Mathf.Sqrt(radius * 2) / (360f / arcAngle)) * defaultMult;

            Vector2 drawStartPosition = position;
            switch (arcCloseType)
            {
                case ArcCloseType.Flat:
                    drawStartPosition = Vector2.Lerp(position + Vector2.right.Rotate(offsetAngle) * radius, position + Vector2.right.Rotate(arcAngle + offsetAngle) * radius, .5f);
                    break;
                case ArcCloseType.Edge:
                    drawStartPosition = EdgeMinMaxPoint(position, -Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * Min_Max_Bias, position + Vector2.right.Rotate(offsetAngle) * radius, position + Vector2.right.Rotate(arcAngle + offsetAngle) * radius, arcAngle);
                    break;
            }

            for (int i = 0; i < numEdges; i++)
            {
                GL.Vertex(drawStartPosition);
                GL.Vertex(position + Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * radius);
                GL.Vertex(position + Vector2.right.Rotate(arcAngle * ((float)(i + 1) / (float)numEdges) + offsetAngle) * radius);
            }

            GL.End();
        }


        /// <summary>
        /// Draws a weighted circle by filling the area between 2 circles with 'innerRadius' and 'outerRadius'
        /// </summary>
        public static void DrawWeightedCircle(Vector2 position, float innerRadius, float outerRadius, int numEdges = 0, Color? colorSetting = null)
            => AddAction(() => InternalDrawWeightedCircle(position, innerRadius, outerRadius, 360, 0, ArcCloseType.None, numEdges, colorSetting));

        /// <summary>
        /// Draws a weighted arc by filling the area between 2 arcs with 'innerRadius' and 'outerRadius'. Arc is closed if ends are connected by arcCloseType
        /// </summary>
        public static void DrawWeightedArc(Vector2 position, float innerRadius, float outerRadius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType = ArcCloseType.None, int numEdges = 0, Color? colorSetting = null)
            => AddAction(() => InternalDrawWeightedCircle(position, innerRadius, outerRadius, arcAngle, offsetAngle, arcCloseType, numEdges, colorSetting));

        /// <summary>
        /// Draws a weighted circle with a edge thickness of 'borderWidth'
        /// </summary>
        public static void DrawWeightedCircle(Vector2 position, float radius, float borderWidth, BorderType borderType, int numEdges = 0, Color? colorSetting = null)
        {
            radius = Mathf.Abs(radius);

            if (borderWidth < 0)
            {
                borderWidth = -borderWidth;
                borderType = borderType switch
                {
                    BorderType.Inside => BorderType.Outside,
                    BorderType.Outside => BorderType.Inside,
                    BorderType.Centered => BorderType.Centered,
                    _ => borderType
                };
            }

            float innerRadius = 0;
            float outerRadius = radius;

            switch (borderType)
            {
                case BorderType.Outside:
                    innerRadius = radius;
                    outerRadius = radius + borderWidth;
                    break;
                case BorderType.Inside:
                    outerRadius = radius;
                    innerRadius = Mathf.Max(radius - borderWidth, 0);
                    break;
                case BorderType.Centered:
                    outerRadius = radius + borderWidth / 2;
                    innerRadius = Mathf.Max(radius - borderWidth / 2, 0);
                    break;
            }

            AddAction(() => InternalDrawWeightedCircle(position, innerRadius, outerRadius, 360, 0, ArcCloseType.None, numEdges, colorSetting));
        }

        /// <summary>
        /// Draws a weighted arc with a edge thickness of 'borderWidth'. Arc is closed if ends are connected by arcCloseType
        /// </summary>
        public static void DrawWeightedArc(Vector2 position, float radius, float arcAngle, float offsetAngle, float borderWidth, BorderType borderType, ArcCloseType arcCloseType = ArcCloseType.None, int numEdges = 0, Color? colorSetting = null)
        {
            radius = Mathf.Abs(radius);

            if (borderWidth < 0)
            {
                borderWidth = -borderWidth;
                borderType = borderType switch
                {
                    BorderType.Inside => BorderType.Outside,
                    BorderType.Outside => BorderType.Inside,
                    BorderType.Centered => BorderType.Centered,
                    _ => borderType
                };
            }

            float innerRadius = 0;
            float outerRadius = radius;

            switch (borderType)
            {
                case BorderType.Outside:
                    innerRadius = radius;
                    outerRadius = radius + borderWidth;
                    break;
                case BorderType.Inside:
                    outerRadius = radius;
                    innerRadius = Mathf.Max(radius - borderWidth, 0);
                    break;
                case BorderType.Centered:
                    outerRadius = radius + borderWidth / 2;
                    innerRadius = Mathf.Max(radius - borderWidth / 2, 0);
                    break;
            }

            AddAction(() => InternalDrawWeightedCircle(position, innerRadius, outerRadius, arcAngle, offsetAngle, arcCloseType, numEdges, colorSetting));
        }

        /// <summary>
        /// Draws a weighted circle bounded by point1 and point2 with an edge thickness of 'borderWidth'
        /// </summary>
        public static void DrawWeightedCircle(Vector2 point1, Vector2 point2, float borderWidth, BorderType borderType, int numEdges = 0, Color? colorSetting = null)
        {
            float radius = Vector2.Distance(point1, point2) / 2;
            Vector2 position = (point1 + point2) / 2;

            if (borderWidth < 0)
            {
                borderWidth = -borderWidth;
                borderType = borderType switch
                {
                    BorderType.Inside => BorderType.Outside,
                    BorderType.Outside => BorderType.Inside,
                    BorderType.Centered => BorderType.Centered,
                    _ => borderType
                };
            }

            float innerRadius = 0;
            float outerRadius = radius;

            switch (borderType)
            {
                case BorderType.Outside:
                    innerRadius = radius;
                    outerRadius = radius + borderWidth;
                    break;
                case BorderType.Inside:
                    outerRadius = radius;
                    innerRadius = Mathf.Max(radius - borderWidth, 0);
                    break;
                case BorderType.Centered:
                    outerRadius = radius + borderWidth / 2;
                    innerRadius = Mathf.Max(radius - borderWidth / 2, 0);
                    break;
            }

            AddAction(() => InternalDrawWeightedCircle(position, innerRadius, outerRadius, 360, 0, ArcCloseType.None, numEdges, colorSetting));
        }

        /// <summary>
        /// Draws a weighted arc bounded by point1 and point2 with an edge thickness of 'borderWidth'
        /// </summary>
        public static void DrawWeightedArc(Vector2 point1, Vector2 point2, float arcAngle, float offsetAngle, float borderWidth, BorderType borderType, ArcCloseType arcCloseType = ArcCloseType.None, int numEdges = 0, Color? colorSetting = null)
        {
            float radius = Vector2.Distance(point1, point2) / 2;
            Vector2 position = (point1 + point2) / 2;

            if (borderWidth < 0)
            {
                borderWidth = -borderWidth;
                borderType = borderType switch
                {
                    BorderType.Inside => BorderType.Outside,
                    BorderType.Outside => BorderType.Inside,
                    BorderType.Centered => BorderType.Centered,
                    _ => borderType
                };
            }

            float innerRadius = 0;
            float outerRadius = radius;

            switch (borderType)
            {
                case BorderType.Outside:
                    innerRadius = radius;
                    outerRadius = radius + borderWidth;
                    break;
                case BorderType.Inside:
                    outerRadius = radius;
                    innerRadius = Mathf.Max(radius - borderWidth, 0);
                    break;
                case BorderType.Centered:
                    outerRadius = radius + borderWidth / 2;
                    innerRadius = Mathf.Max(radius - borderWidth / 2, 0);
                    break;
            }

            AddAction(() => InternalDrawWeightedCircle(position, innerRadius, outerRadius, arcAngle, offsetAngle, arcCloseType, numEdges, colorSetting));
        }

        private static void InternalDrawWeightedCircle(Vector2 position, float innerRadius, float outerRadius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType = ArcCloseType.None, int numEdges = 0, Color ? colorSetting = null)
        {
            if (innerRadius == outerRadius)
            {
                InternalDrawEdgeCircle(position, innerRadius, arcAngle, offsetAngle, numEdges, false, arcCloseType, colorSetting);
                return;
            }

            innerRadius = Mathf.Abs(innerRadius);
            outerRadius = Mathf.Abs(outerRadius);
            float signAngle = Mathf.Sign(arcAngle);
            float arcAngleAbs = Mathf.Abs(arcAngle);

            if (innerRadius > outerRadius)
            {
                float temp = outerRadius;
                outerRadius = innerRadius;
                innerRadius = temp;
            }

            int defaultMult = numEdges >= 0 ? 1 : Mathf.Abs(numEdges);

            if (numEdges <= 0)
                numEdges = (int)(12 * Mathf.Sqrt(outerRadius * 2) / (360f / Mathf.Abs(arcAngle))) * defaultMult;

            float borderWidth = outerRadius - innerRadius;

            // fill arc if too thick
            if (arcAngleAbs < 360)
            {
                Vector2 innerStart = position + Vector2.right.Rotate(offsetAngle) * innerRadius;
                Vector2 innerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                Vector2 outerStart = position + Vector2.right.Rotate(offsetAngle) * outerRadius;
                Vector2 outerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;

                bool fillCompletely = false;
                switch (arcCloseType)
                {
                    case ArcCloseType.Flat:
                        float distance = outerRadius * 2;
                        if (arcAngleAbs >= 180)
                        {
                            distance = Vector2.Distance((outerStart + outerEnd) / 2, position + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * outerRadius);

                            fillCompletely = borderWidth > (distance / 2);
                            if (fillCompletely)
                            {
                                InternalDrawFilledCircle(position, outerRadius, arcAngle, offsetAngle, numEdges, arcCloseType, colorSetting);
                                return;
                            }
                        }
                        else
                        {
                            distance = Vector2.Distance((innerStart + innerEnd) / 2, position + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * outerRadius);
                            fillCompletely = borderWidth > (distance / 2);
                            if (fillCompletely)
                            {
                                InternalDrawFilledCircle(position, innerRadius, arcAngle, offsetAngle, numEdges, arcCloseType, colorSetting);
                                arcCloseType = ArcCloseType.None;
                            }
                        }
                        break;
                    case ArcCloseType.Center:
                        if (innerRadius <= outerRadius / 2)
                        {
                            InternalDrawFilledCircle(position, outerRadius, arcAngle, offsetAngle, numEdges, arcCloseType, colorSetting);
                            return;
                        }
                        break;
                    case ArcCloseType.Edge:
                        Vector2 _outerStart = position + Vector2.right.Rotate(offsetAngle) * outerRadius;
                        Vector2 _outerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;
                        Vector2 _outerCorner = EdgeMinMaxPoint(position, -Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * Min_Max_Bias, position + Vector2.right.Rotate(offsetAngle) * outerRadius, position + Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius, arcAngle);
                        if (_outerCorner == _outerEnd || _outerCorner == _outerStart)
                        {
                            distance = outerRadius * 2;
                            if (arcAngleAbs >= 180)
                            {
                                distance = Vector2.Distance((outerStart + outerEnd) / 2, position + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * outerRadius);

                                fillCompletely = borderWidth > (distance / 2);
                                if (fillCompletely)
                                {
                                    InternalDrawFilledCircle(position, outerRadius, arcAngle, offsetAngle, numEdges, arcCloseType, colorSetting);
                                    return;
                                }
                            }
                            else
                            {
                                distance = Vector2.Distance((innerStart + innerEnd) / 2, position + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * outerRadius);
                                fillCompletely = borderWidth > (distance / 2);
                                if (fillCompletely)
                                {
                                    InternalDrawFilledCircle(position, innerRadius, arcAngle, offsetAngle, numEdges, arcCloseType, colorSetting);
                                    arcCloseType = ArcCloseType.None;
                                }
                            }
                        }
                        break;
                }
            }

            // draw arc
            for (int i = 1; i <= numEdges; i++)
            {
                Vector2 outer0 = position + Vector2.right.Rotate(arcAngle * ((float)(i-1) / (float)numEdges) + offsetAngle) * outerRadius;
                Vector2 outer1 = position + Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * outerRadius;
                Vector2 inner0 = position + Vector2.right.Rotate(arcAngle * ((float)(i-1) / (float)numEdges) + offsetAngle) * innerRadius;
                Vector2 inner1 = position + Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * innerRadius;

                List<Vector2> vertices = new();
                vertices.Add(outer0);
                vertices.Add(outer1);
                vertices.Add(inner1);
                vertices.Add(inner0);
                InternalDrawFilledPath(vertices, colorSetting);
            }

            // if arc and needs connector - continue
            if (arcAngleAbs >= 360 || arcCloseType == ArcCloseType.None)
                return;

            
            List<Vector2> closingVerticesWest = new();
            List<Vector2> closingVerticesEast = new();

            bool GetNearestCircleEdge(Vector2 point, float circleRadius, out Vector2 p1, out Vector2 p2, out Vector2Int edgeIndex)
            {
                p1 = Vector2.zero;
                p2 = Vector2.zero;
                edgeIndex = new Vector2Int(-1, -1);

                int index1 = 0;
                int index2 = 1;
                Vector2 vertex0 = position + Vector2.right.Rotate(offsetAngle) * circleRadius;
                float anglePoint = Vector2.SignedAngle(vertex0 - position, point - position).PositiveAngle();
                for (int i = 1; i <= numEdges; i++)
                {
                    Vector2 vertex1 = position + Vector2.right.Rotate(arcAngle * ((float)index1 / (float)numEdges) + offsetAngle) * circleRadius;
                    Vector2 vertex2 = position + Vector2.right.Rotate(arcAngle * ((float)index2 / (float)numEdges) + offsetAngle) * circleRadius;
                    float angle1 = Vector2.SignedAngle(vertex0 - position, vertex1 - position).PositiveAngle();
                    float angle2 = Vector2.SignedAngle(vertex0 - position, vertex2 - position).PositiveAngle();
                    if (signAngle == 1 ? (anglePoint > angle1 && anglePoint <= angle2) : (anglePoint <= angle1 && anglePoint > angle2))
                    {
                        p1 = vertex1;
                        p2 = vertex2;
                        edgeIndex = new (index1, index2);
                        return true;
                    }

                    index1++;
                    index2++;
                }

                return false;
            }

            (List<Vector2>, List <Vector2>) GetWestEastInnerVectorLists(Vector2 outerCorner, Vector2 innerCorner, float innerRadiusLimit, bool ignoreDrawInnerCorner = false)
            {
                List<Vector2> westVertices = new();
                List<Vector2> eastVertices = new();

                westVertices.Add(outerCorner);
                Vector2 westEdgeDirection = position + Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius - outerCorner;
                for (int i = numEdges; i >= numEdges / 2; i--)
                {
                    Vector2 testPosition = position + Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * innerRadius;
                    Vector2 testDirection = testPosition - innerCorner;
                    if (Vector2.SignedAngle(westEdgeDirection, testDirection) * signAngle >= 0)
                    {
                        westVertices.Add(testPosition);
                    }
                    else
                    {
                        if (i < numEdges)
                        {
                            Vector2 pointA1 = testPosition;
                            Vector2 pointA2 = position + Vector2.right.Rotate(arcAngle * ((float)(i + 1) / (float)numEdges) + offsetAngle) * innerRadius;
                            Vector2 pointB1 = innerCorner;
                            Vector2 pointB2 = innerCorner + westEdgeDirection * (outerRadius + borderWidth);
                            if (Extensions.FindSegmentRayIntersection(pointA1, pointA2, pointB1, pointB2, out Vector2 intersectionPoint))
                            {
                                westVertices.Add(intersectionPoint);
                            }
                        }
                        break;
                    }
                }

                Vector2 innerRadiusPoint = position + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * innerRadiusLimit;
                bool innerCornerOutsideBounds = Vector2.Distance(position, innerCorner) > innerRadiusLimit;

                if (innerCornerOutsideBounds)
                {
                    Vector2 lastPosition = westVertices[westVertices.Count - 1];

                    Vector2 rayDirection = (lastPosition - innerCorner).normalized;

                    bool hitCircle = Extensions.FindFirstRayCircleIntersection(innerCorner, rayDirection, position, innerRadiusLimit, out Vector2 circleIntersectionPoint);
                    if (!hitCircle)
                        hitCircle = Extensions.FindSegmentRayIntersection(innerCorner, lastPosition, position, position + westEdgeDirection.Rotate(-90 * signAngle), out circleIntersectionPoint);

                    if (hitCircle)
                    {
                        if (GetNearestCircleEdge(circleIntersectionPoint, innerRadiusLimit, out Vector2 p1, out Vector2 p2, out Vector2Int index))
                        {
                            bool hitEdge = Extensions.FindSegmentIntersection(p1, p2, innerCorner, lastPosition, out Vector2 edgeIntersectionPoint);
                            if (!hitEdge)
                                edgeIntersectionPoint = circleIntersectionPoint;

                            westVertices.Add(edgeIntersectionPoint);
                            for (int i = index[0]; i >= 0; i--)
                            {
                                float angleToInnerRadiusMidpoint = Vector2.SignedAngle(position - innerCorner, Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle));
                                if (signAngle == 1 ? angleToInnerRadiusMidpoint < 0 : angleToInnerRadiusMidpoint > 0)
                                {
                                    Vector2 pos = position + Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * innerRadiusLimit;
                                    westVertices.Add(pos);
                                }
                                else
                                {
                                    westVertices.Add(innerRadiusPoint);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        westVertices.Add(innerCorner);
                    }
                }
                else
                {
                    westVertices.Add(innerCorner);
                }

                // east
                eastVertices.Add(outerCorner);
                Vector2 eastEdgeDirection = position + Vector2.right.Rotate(offsetAngle) * innerRadius - outerCorner;
                for (int i = 0; i <= numEdges / 2; i++)
                {
                    Vector2 testPosition = position + Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * innerRadius;
                    Vector2 testDirection = testPosition - innerCorner;
                    if (Vector2.SignedAngle(eastEdgeDirection, testDirection) * signAngle <= 0)
                    {
                        eastVertices.Add(testPosition);
                    }
                    else
                    {
                        if (i > 0)
                        {
                            Vector2 pointA1 = testPosition;
                            Vector2 pointA2 = position + Vector2.right.Rotate(arcAngle * ((float)(i - 1) / (float)numEdges) + offsetAngle) * innerRadius;
                            Vector2 pointB1 = innerCorner;
                            Vector2 pointB2 = innerCorner + eastEdgeDirection * (outerRadius + borderWidth);
                            if (Extensions.FindSegmentRayIntersection(pointA1, pointA2, pointB1, pointB2, out Vector2 intersectionPoint))
                            {
                                eastVertices.Add(intersectionPoint);
                            }
                        }
                        break;
                    }
                }

                if (innerCornerOutsideBounds)
                {
                    Vector2 lastPosition = eastVertices[eastVertices.Count - 1];

                    Vector2 rayDirection = (lastPosition - innerCorner).normalized;

                    bool hitCircle = Extensions.FindFirstRayCircleIntersection(innerCorner, rayDirection, position, innerRadiusLimit, out Vector2 circleIntersectionPoint);
                    if (!hitCircle)
                        hitCircle = Extensions.FindSegmentRayIntersection(innerCorner, lastPosition, position, position + eastEdgeDirection.Rotate(90 * signAngle), out circleIntersectionPoint);

                    if (hitCircle)
                    {
                        if (GetNearestCircleEdge(circleIntersectionPoint, innerRadiusLimit, out Vector2 p1, out Vector2 p2, out Vector2Int index))
                        {
                            bool hitEdge = Extensions.FindSegmentIntersection(p1, p2, innerCorner, lastPosition, out Vector2 edgeIntersectionPoint);
                            if (!hitEdge)
                                edgeIntersectionPoint = circleIntersectionPoint;

                            eastVertices.Add(edgeIntersectionPoint);
                            for (int i = index[1]; i <= numEdges; i++)
                            {
                                float angleToInnerRadiusMidpoint = Vector2.SignedAngle(position - innerCorner, Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle));
                                if (signAngle == 1 ? angleToInnerRadiusMidpoint > 0 : angleToInnerRadiusMidpoint < 0)
                                {
                                    Vector2 pos = position + Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * innerRadiusLimit;
                                    eastVertices.Add(pos);
                                }
                                else
                                {
                                    eastVertices.Add(innerRadiusPoint);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        eastVertices.Add(innerCorner);
                    }
                }
                else
                {
                    eastVertices.Add(innerCorner);
                }

                return (westVertices, eastVertices);
            }


            // draw connector
            switch (arcCloseType)
            {
                case ArcCloseType.Flat:
                    if (arcAngleAbs <= 180)
                    {
                        Vector2 innerStart = position + Vector2.right.Rotate(offsetAngle) * innerRadius;
                        Vector2 innerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                        Vector2 outerCorner = (innerStart + innerEnd) / 2;
                        Vector2 innerCorner = outerCorner + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * borderWidth;
                        (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(outerCorner, innerCorner, innerRadius);
                    }
                    else
                    {
                        Vector2 innerStart = position + Vector2.right.Rotate(offsetAngle) * innerRadius;
                        Vector2 innerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                        Vector2 outerStart = position + Vector2.right.Rotate(offsetAngle) * outerRadius;
                        Vector2 outerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;
                        Vector2 outerCorner = (outerStart + outerEnd) / 2;
                        Vector2 innerCorner = outerCorner + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * borderWidth;
                        Vector2 midCorner = (innerStart + innerEnd) / 2;
                        (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(midCorner, innerCorner, innerRadius);

                        List<Vector2> outerVertices = new();
                        outerVertices.Add(innerStart);
                        outerVertices.Add(innerEnd);
                        outerVertices.Add(outerEnd);
                        outerVertices.Add(outerStart);
                        InternalDrawFilledPath(outerVertices, colorSetting);

                    }
                    break;
                case ArcCloseType.Center:
                    Vector2 innerEdgeCenterPosition = position + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * borderWidth;
                    Vector2 innerCenerDirection = Vector2.right.Rotate(arcAngle / 2 + offsetAngle);
                    Vector2 innerRadiusPoint = position + innerCenerDirection * innerRadius;
                    Vector2 westEdgeDirection = Vector2.right.Rotate(arcAngle + offsetAngle);
                    Vector2 westEdgeDirectionNormal = westEdgeDirection.Rotate(-90 * signAngle);
                    Vector2 orthogonalPoint = (position + Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius) + westEdgeDirectionNormal * borderWidth;
                    if (Extensions.FindSegmentIntersection(position, position + innerCenerDirection * outerRadius, orthogonalPoint, orthogonalPoint + -westEdgeDirection * outerRadius, out Vector2 intersectionPoint, false))
                    {
                        (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(position, intersectionPoint, Mathf.Min(borderWidth, innerRadius), true);
                    }
                    else
                    {
                        (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(position, innerEdgeCenterPosition, Mathf.Min(borderWidth, innerRadius), true);
                    }
                    break;
                case ArcCloseType.Edge:

                    Vector2 _outerStart = position + Vector2.right.Rotate(offsetAngle) * outerRadius;
                    Vector2 _outerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;
                    Vector2 _outerCorner = EdgeMinMaxPoint(position, -Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * Min_Max_Bias, position + Vector2.right.Rotate(offsetAngle) * outerRadius, position + Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius, arcAngle);
                    
                    // if flat
                    if (_outerCorner == _outerEnd || _outerCorner == _outerStart)
                    {
                        if (arcAngleAbs <= 180)
                        {
                            Vector2 innerStart = position + Vector2.right.Rotate(offsetAngle) * innerRadius;
                            Vector2 innerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                            Vector2 outerCorner = (innerStart + innerEnd) / 2;
                            Vector2 innerCorner = outerCorner + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * borderWidth;
                            (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(outerCorner, innerCorner, innerRadius);
                        }
                        else
                        {
                            Vector2 innerStart = position + Vector2.right.Rotate(offsetAngle) * innerRadius;
                            Vector2 innerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                            Vector2 outerStart = position + Vector2.right.Rotate(offsetAngle) * outerRadius;
                            Vector2 outerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;
                            Vector2 outerCorner = (outerStart + outerEnd) / 2;
                            Vector2 innerCorner = outerCorner + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * borderWidth;
                            Vector2 midCorner = (innerStart + innerEnd) / 2;
                            (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(midCorner, innerCorner, innerRadius);

                            List<Vector2> outerVertices = new();
                            outerVertices.Add(innerStart);
                            outerVertices.Add(innerEnd);
                            outerVertices.Add(outerEnd);
                            outerVertices.Add(outerStart);
                            InternalDrawFilledPath(outerVertices, colorSetting);
                        }
                    }
                    else
                    {
                        Vector2 innerDirection = Vector2.right.Rotate(arcAngle / 2 + offsetAngle);
                        Vector2 innerStart = position + Vector2.right.Rotate(offsetAngle) * innerRadius;
                        Vector2 innerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                        Vector2 outerStart = position + Vector2.right.Rotate(offsetAngle) * outerRadius;
                        Vector2 outerEnd = position + Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;
                        Vector2 outerCorner = EdgeMinMaxPoint(position, -innerDirection * Min_Max_Bias, position + Vector2.right.Rotate(offsetAngle) * outerRadius, position + Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius, arcAngle);
                        Vector2 midCorner = EdgeMinMaxPoint(position, -innerDirection * Min_Max_Bias, position + Vector2.right.Rotate(offsetAngle) * innerRadius, position + Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius, arcAngle);
                        Vector2 innerCorner = _outerCorner + new Vector2(Mathf.Sign(innerDirection.x), Mathf.Sign(innerDirection.y)) * borderWidth;
                        
                        if (Vector2.Distance(position, innerCorner) > innerRadius)
                        {
                            Vector2 xDir = new Vector2(Mathf.Sign(innerDirection.x), 0);
                            Vector2 yDir = new Vector2(0, Mathf.Sign(innerDirection.y));
                            Vector2 pointX = position - xDir * innerRadius;
                            Vector2 pointY = position - yDir * innerRadius;
                            
                            if (GetNearestCircleEdge(innerCorner, innerRadius, out Vector2 p1, out Vector2 p2, out Vector2Int index))
                            {
                                if (Extensions.FindSegmentIntersection(innerCorner, innerCorner + xDir * innerRadius, p1, p2, out Vector2 intersectionPoint1))
                                {
                                    innerCorner = intersectionPoint1;
                                }
                                else if (Extensions.FindSegmentIntersection(innerCorner, innerCorner + yDir * innerRadius, p1, p2, out Vector2 intersectionPoint2))
                                {
                                    innerCorner = intersectionPoint2;
                                }
                            }
                            
                        }
                        
                        //(closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(midCorner, innerCorner);

                        List<Vector2> vertices = new();
                        vertices.Add(innerStart);
                        vertices.Add(outerStart);
                        vertices.Add(outerCorner);
                        vertices.Add(midCorner);
                        InternalDrawFilledPath(vertices, colorSetting);

                        vertices.Clear();
                        vertices.Add(innerEnd);
                        vertices.Add(outerEnd);
                        vertices.Add(outerCorner);
                        vertices.Add(midCorner);
                        InternalDrawFilledPath(vertices, colorSetting);
                    }
                    
                    break;

            }

            InternalDrawFilledPath(closingVerticesWest, colorSetting);
            InternalDrawFilledPath(closingVerticesEast, colorSetting);
        }

        private static Vector2 EdgeMinMaxPoint(Vector2 center, Vector2 offset, Vector2 point1, Vector2 point2, float angle)
        {
            Vector2 minXminY = new Vector2(Mathf.Min(point1.x, point2.x), Mathf.Min(point1.y, point2.y));
            Vector2 minXmaxY = new Vector2(Mathf.Min(point1.x, point2.x), Mathf.Max(point1.y, point2.y));
            Vector2 maxXminY = new Vector2(Mathf.Max(point1.x, point2.x), Mathf.Min(point1.y, point2.y));
            Vector2 maxXmaxY = new Vector2(Mathf.Max(point1.x, point2.x), Mathf.Max(point1.y, point2.y));
            Vector2[] vectors = new Vector2[4] { minXminY, minXmaxY, maxXminY, maxXmaxY };

            float rev = (Mathf.Abs(angle) % 360) <= 180 ? 1 : -1;

            float distanceMinMin = Vector2.Distance(minXminY, center + offset * rev);
            float distanceMinMax = Vector2.Distance(minXmaxY, center + offset * rev);
            float distanceMaxMin = Vector2.Distance(maxXminY, center + offset * rev);
            float distanceMaxMax = Vector2.Distance(maxXmaxY, center + offset * rev);
            float[] distances = new float[4] { distanceMinMin, distanceMinMax, distanceMaxMin, distanceMaxMax };

            float minDistance = Mathf.Min(Mathf.Min(Mathf.Min(distanceMinMin, distanceMinMax), distanceMaxMin), distanceMaxMax);
            float maxDistance = Mathf.Max(Mathf.Max(Mathf.Max(distanceMinMin, distanceMinMax), distanceMaxMin), distanceMaxMax);
            int index = Array.IndexOf(distances, (Mathf.Abs(angle) % 360) <= 180 ? minDistance : maxDistance);

            return vectors[index];
        }

        private static Vector2 GetPointOnUnitCircle(float radius, float angle) => Vector2.right.Rotate(angle) * radius;
        #endregion

        #region ### Lines and Polygons
        /// <summary>
        /// Draws a line starting at 'from' ending at 'to'
        /// </summary>
        public static void DrawLine(Vector2 from, Vector2 to, Color? colorSetting = null)
            => AddAction(() => InternalDrawLine(from, to, colorSetting));

        private static void InternalDrawLine(Vector2 from, Vector2 to, Color? colorSetting = null)
        {
            GL.wireframe = true;
            GL.Begin(GL.LINES);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));
            GL.Vertex(from);
            GL.Vertex(to);
            GL.End();
        }

        /// <summary>
        /// Draws a dashed line starting at 'from' ending at 'to'. Length of each dash is 'dashLength' with 'gapLength' space between them
        /// </summary>
        public static void DrawDashedLine(Vector2 from, Vector2 to, float dashLength, float gapLength, Color? colorSetting = null)
            => AddAction(() => InternalDrawDashedLine(from, to, dashLength, gapLength, colorSetting));

        private static void InternalDrawDashedLine(Vector2 from, Vector2 to, float dashLength, float gapLength, Color? colorSetting = null)
        {
            if (dashLength == 0)
                return;

            GL.wireframe = true;
            GL.Begin(GL.LINES);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));

            float accumulatedDistance = 0;
            float totalDistance = Vector2.Distance(from, to);
            Vector2 point = from;
            Vector2 direction = (to - from).normalized;

            while (accumulatedDistance < totalDistance)
            {
                GL.Vertex(point);
                point += direction * dashLength;
                GL.Vertex(point);
                point += direction * gapLength;

                accumulatedDistance += dashLength + gapLength;
            }

            GL.End();
        }



        /// <summary>
        /// Draws a path connecting the points in 'points'
        /// </summary>
        public static void DrawPath(List<Vector2> points, bool closed = false, Color? colorSetting = null)
            => AddAction(() => InternalDrawPath(points, closed, colorSetting));
        private static void InternalDrawPath(List<Vector2> points, bool closed, Color? colorSetting = null)
        {
            if (points == null || points.Count == 0)
                return;

            GL.wireframe = true;
            GL.Begin(GL.LINE_STRIP);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));

            foreach (Vector2 point in points)
                GL.Vertex(point);

            if (closed)
                GL.Vertex(points[0]);

            GL.End();
        }



        /// <summary>
        /// Draws an open polygon connecting the points in 'vertices'
        /// </summary>
        public static void DrawOpenPolygon(List<Vector2> vertices, Color? colorSetting = null)
        => AddAction(() => InternalDrawPath(vertices, true, colorSetting));

        /// <summary>
        /// Draws a solid polygon connecting the points in 'vertices'
        /// </summary>
        public static void DrawSolidPolygon(List<Vector2> vertices, Color? colorSetting = null)
        => AddAction(() => InternalDrawFilledPath(vertices, colorSetting));

        private static void InternalDrawPolygon(List<Vector2> vertices, bool solid, Color? colorSetting = null)
        {
            if (solid)
                InternalDrawFilledPath(vertices, colorSetting);
            else
                InternalDrawPath(vertices, true, colorSetting);
        }

        private static void InternalDrawFilledPath(List<Vector2> points, Color? colorSetting = null)
        {
            if (points == null || points.Count == 0)
                return;

            GL.wireframe = false;
            GL.Begin(GL.TRIANGLES);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));

            Vector2 center = new Vector2(points.Select(p => p.x).Sum() / points.Count, points.Select(p => p.y).Sum() / points.Count);

            for (int i = 0; i < points.Count; i++)
            {
                GL.Vertex(center);
                GL.Vertex(points[i]);
                GL.Vertex(points[(i + 1) % points.Count]);
            }

            GL.End();
        }



        /// <summary>
        /// Draws a solid polygon connecting the points in 'vertices'. Trianlges colored with 'colors'
        /// </summary>
        public static void DrawMultiColoredPolygon(List<Vector2> points, List<Color> colors)
        => AddAction(() => InternalDrawMultiColoredPolygon(points, colors));
        private static void InternalDrawMultiColoredPolygon(List<Vector2> points, List<Color> colors)
        {
            if (points == null || points.Count == 0)
                return;

            GL.wireframe = false;
            GL.Begin(GL.TRIANGLES);

            bool noColors = colors == null || colors.Count == 0;
            if (noColors)
                GL.Color(color.Value);

            Vector2 center = new Vector2(points.Select(p => p.x).Sum() / points.Count, points.Select(p => p.y).Sum() / points.Count);

            for (int i = 0; i < points.Count; i++)
            {
                if (!noColors)
                    GL.Color(colors[i % colors.Count]);

                GL.Vertex(center);
                GL.Vertex(points[i]);
                GL.Vertex(points[(i + 1) % points.Count]);
            }

            GL.End();
        }
        #endregion

        /// <summary>
        /// Draws open triangles using every 3 vertices in 'vertices'
        /// </summary>
        public static void DrawOpenTriangles(Vector2[] vertices, Color? colorSetting = null)
            => AddAction(() => InternalDrawTriangle(vertices, false, colorSetting));

        /// <summary>
        /// Draws solid triangles using every 3 vertices in 'vertices'
        /// </summary>
        public static void DrawSolidTriangles(Vector2[] vertices, Color? colorSetting = null)
            => AddAction(() => InternalDrawTriangle(vertices, true, colorSetting));
        private static void InternalDrawTriangle(Vector2[] points, bool solid, Color? colorSetting = null)
        {
            GL.wireframe = !solid;
            GL.Begin(GL.TRIANGLES);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));

            foreach (Vector2 point in points)
                GL.Vertex(point);

            GL.End();
        }



        /// <summary>
        /// Draws an open triangle based on a center position, height, and width. Angle rotates the triangle. Skew offsets the point opposite the base edge
        /// </summary>
        public static void DrawOpenTriangle(Vector2 position, Vector2 centerOffset, float height, float width, float skew, float angle, Color? colorSetting = null)
            => AddAction(() => InternalDrawTriangleAdv(position, centerOffset, height, width, skew, angle, false, colorSetting));

        /// <summary>
        /// Draws a solid triangle based on a center position, height, and width. Angle rotates the triangle. Skew offsets the point opposite the base edge
        /// </summary>
        public static void DrawSolidTriangle(Vector2 position, Vector2 centerOffset, float height, float width, float skew, float angle, Color? colorSetting = null)
            => AddAction(() => InternalDrawTriangleAdv(position, centerOffset, height, width, skew, angle, true, colorSetting));

        private static void InternalDrawTriangleAdv(Vector2 position, Vector2 centerOffset, float height, float width, float skew, float angle, bool solid, Color? colorSetting = null)
        {
            GL.wireframe = !solid;
            GL.Begin(GL.TRIANGLES);
            GL.Color((Color)(colorSetting == null ? color : colorSetting));

            float adjustedSkew = Extensions.Remap(-1, 1, 0, 1, skew);
            Vector2 adjustedOffset = centerOffset * height / 2;

            List<Vector2> points = new(); // with center at 0,0
            points.Add(new Vector2(-width / 2, -height / 2));
            points.Add(new Vector2(width / 2, -height / 2));
            points.Add(new Vector2(Mathf.LerpUnclamped(-width / 2, width / 2, adjustedSkew), height / 2));

            foreach (Vector2 point in points)
                GL.Vertex(point.Rotate(angle) + position + adjustedOffset.Rotate(angle));

            GL.End();
        }

        /// <summary>
        /// Draws an open capsule at 'position' based on box size and capsule direction
        /// </summary>
        public static void DrawOpenCapsule(Vector2 position, Vector2 size, CapsuleDirection2D direction, float angle = 0, Color? colorSetting = null)
            => AddAction(() => InternalDrawCapsule(position, size, direction, angle, false, colorSetting));

        /// <summary>
        /// Draws an open capsule starting at 'from' ending at 'to' with 'radius'
        /// </summary>
        public static void DrawOpenCapsule(Vector2 from, Vector2 to, float radius, Color? colorSetting = null)
        {
            Vector2 center = Vector2.Lerp(from, to, .5f);
            AddAction(() => InternalDrawCapsule(center, new Vector2(radius * 2, Vector2.Distance(from, to) + radius * 2), CapsuleDirection2D.Vertical, Vector2.SignedAngle(Vector2.up, from - center), false, colorSetting));
        }

        /// <summary>
        /// Draws a solid capsule at 'position' based on box size and capsule direction
        /// </summary>
        public static void DrawSolidCapsule(Vector2 position, Vector2 size, CapsuleDirection2D direction, float angle = 0, Color? colorSetting = null)
            => AddAction(() => InternalDrawCapsule(position, size, direction, angle, true, colorSetting));

        /// <summary>
        /// Draws a solid capsule starting at 'from' ending at 'to' with 'radius'
        /// </summary>
        public static void DrawSolidCapsule(Vector2 from, Vector2 to, float radius, Color? colorSetting = null)
        {
            Vector2 center = Vector2.Lerp(from, to, .5f);
            AddAction(() => InternalDrawCapsule(center, new Vector2(radius * 2, Vector2.Distance(from, to) + radius * 2), CapsuleDirection2D.Vertical, Vector2.SignedAngle(Vector2.up, from - center), true, colorSetting));
        }

        private static void InternalDrawCapsule(Vector2 position, Vector2 size, CapsuleDirection2D direction, float angle, bool solid, Color? colorSetting = null)
        {
            float radius = direction == CapsuleDirection2D.Vertical ? size.x / 2 : size.y / 2;
            float difference = direction == CapsuleDirection2D.Vertical ?
                (size.y > size.x ? (size.y - size.x) / 2 : 0) :
                (size.x > size.y ? (size.x - size.y) / 2 : 0);

            float offsetAngle = (direction == CapsuleDirection2D.Vertical ? 0 : 90) + angle;
            Vector2 curveOffsetDirection = (direction == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.left).Rotate(angle);

            if (!solid)
            {
                InternalDrawEdgeCircle(position + (curveOffsetDirection * difference), radius, 180, offsetAngle, 0, false, ArcCloseType.None, colorSetting);
                InternalDrawEdgeCircle(position + (-curveOffsetDirection * difference), radius, 180, 180 + offsetAngle, 0, false, ArcCloseType.None, colorSetting);

                Vector2 orientationSize = (direction == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.left).Rotate(angle);
                InternalDrawLine(position + (orientationSize * difference) + GetPointOnUnitCircle(radius, 180 + offsetAngle),
                                 position + (-orientationSize * difference) + GetPointOnUnitCircle(radius, 180 + offsetAngle), colorSetting);
                InternalDrawLine(position + (orientationSize * difference) + GetPointOnUnitCircle(radius, offsetAngle),
                                 position + (-orientationSize * difference) + GetPointOnUnitCircle(radius, offsetAngle), colorSetting);
            }
            else
            {
                InternalDrawFilledCircle(position + (curveOffsetDirection * difference), radius, 180, offsetAngle, 0, ArcCloseType.Center, colorSetting);
                InternalDrawFilledCircle(position + (-curveOffsetDirection * difference), radius, 180, 180 + offsetAngle, 0, ArcCloseType.Center, colorSetting);

                Vector2 orientationSize = direction == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.right;
                InternalDrawRect(position, (size - (radius * 2 * orientationSize)).ZeroNegatives(), angle, solid, colorSetting);
            }
        }

        public static void InternalDrawCapsule(Vector2 from, Vector2 to, float radius, Color? colorSetting = null)
        {
            Vector2 center = Vector2.Lerp(from, to, .5f);
            InternalDrawCapsule(center, new Vector2(radius * 2, Vector2.Distance(from, to) + radius * 2), CapsuleDirection2D.Vertical, Vector2.SignedAngle(Vector2.up, from - center), true, colorSetting);
        }




        /// <summary>
        /// Draws a solid path with 'thickness' connecting the points in 'points'
        /// </summary>
        public static void DrawCapsulePath(List<Vector2> points, float thickness, Color? colorSetting = null)
            => AddAction(() => InternalDrawCapsulePath(points, thickness, colorSetting));
        private static void InternalDrawCapsulePath(List<Vector2> points, float thickness, Color? colorSetting = null)
        {
            if (points == null || points.Count == 0 || thickness == 0)
                return;

            for (int i = 1; i < points.Count; i++)
                InternalDrawCapsule(points[i - 1], points[i], thickness / 2, colorSetting);
        }




        /// <summary>
        /// Draws a bezier curve starting at 'from' ending at 'to'. Curve [typically between -1 and 1]
        /// </summary>
        public static void DrawBezier(Vector2 from, Vector2 to, float curve = .75f, int numEdges = 0, Color? colorSetting = null)
            => AddAction(() => InternalDrawBezier(from, to, curve, numEdges, colorSetting));

        private static void InternalDrawBezier(Vector2 from, Vector2 to, float curve, int numEdges, Color? colorSetting = null)
        {
            List<Vector2> joints = new List<Vector2>();

            float lerpCenter = Extensions.Remap(-1, 1, 0, 1, curve);

            Vector2 p1c = new Vector2(from.x, to.y);
            Vector2 p4c = new Vector2(to.x, from.y);

            Vector2 p2 = Vector2.LerpUnclamped(p1c, p4c, lerpCenter);
            Vector2 p3 = Vector2.LerpUnclamped(p1c, p4c, 1 - lerpCenter);

            int defaultMult = numEdges >= 0 ? 1 : Mathf.Abs(numEdges);

            if (numEdges <= 0)
                numEdges = (int)Mathf.Clamp(Mathf.Pow(25, Mathf.Sqrt(Mathf.Sqrt(Mathf.Sqrt(Mathf.Abs(curve))))), 1, 75) * defaultMult;

            float t = 0;
            while (t < 1)
            {
                Vector2 point = Mathf.Pow(1 - t, 3) * from +
                                3 * Mathf.Pow(1 - t, 2) * t * p2 +
                                3 * (1 - t) * Mathf.Pow(t, 2) * p3 +
                                Mathf.Pow(t, 3) * to;
                joints.Add(point);
                t += (1f / (float)numEdges);
            }

            t = 1;
            Vector2 finalPoint = Mathf.Pow(1 - t, 3) * from +
                                3 * Mathf.Pow(1 - t, 2) * t * p2 +
                                3 * (1 - t) * Mathf.Pow(t, 2) * p3 +
                                Mathf.Pow(t, 3) * to;
            joints.Add(finalPoint);

            InternalDrawPath(joints, false, colorSetting);
        }





        /// <summary>
        /// Draws any 2D collider shape
        /// </summary>
        public static void DrawCollider2D(Collider2D collider, bool solid = false, Color? colorSetting = null) => AddAction(() => InternalDrawCollider2D(collider, solid, colorSetting));
        private static void InternalDrawCollider2D(Collider2D collider, bool solid, Color? colorSetting = null)
        {
            if (collider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = (BoxCollider2D)collider;

                if (boxCollider.transform.rotation.eulerAngles.z == 0)
                {
                    InternalDrawBoxEdgeRadius((Vector2)boxCollider.transform.position + ((Vector2)boxCollider.transform.right * boxCollider.offset.x) + ((Vector2)boxCollider.transform.up * boxCollider.offset.y), boxCollider.size.ScaleEach(boxCollider.transform.lossyScale.Abs()), boxCollider.edgeRadius, true, solid, BorderType.Outside, colorSetting);
                }
                else
                {
                    InternalDrawRectEdgeRadius((Vector2)boxCollider.transform.position + ((Vector2)boxCollider.transform.right * boxCollider.offset.x) + ((Vector2)boxCollider.transform.up * boxCollider.offset.y), boxCollider.size.ScaleEach(boxCollider.transform.lossyScale.Abs()), boxCollider.edgeRadius, boxCollider.transform.rotation.eulerAngles.z, true, solid, BorderType.Outside, colorSetting);
                }
            }
            else if (collider is CompositeCollider2D)
            {
                CompositeCollider2D compositeCollider = (CompositeCollider2D)collider;

                for (int i = 0; i < compositeCollider.pathCount; i++)
                {
                    Vector2[] array = new Vector2[compositeCollider.GetPathPointCount(i)];
                    compositeCollider.GetPath(i, array);

                    InternalDrawPolygon(array.Select(x => x.Rotate(compositeCollider.transform.rotation.eulerAngles.z) + (Vector2)compositeCollider.transform.position).ToList(), solid, colorSetting);
                }
            }
            else if (collider is CircleCollider2D)
            {
                CircleCollider2D circleCollider = (CircleCollider2D)collider;
                InternalDrawCircle((Vector2)circleCollider.transform.position + ((Vector2)circleCollider.transform.right * circleCollider.offset.x) + ((Vector2)circleCollider.transform.up * circleCollider.offset.y), circleCollider.radius * circleCollider.transform.lossyScale.Abs().Max(), 360f, 0f, 0, solid, ArcCloseType.None, colorSetting);
            }
            else if (collider is CapsuleCollider2D)
            {
                CapsuleCollider2D capsuleCollider2D = (CapsuleCollider2D)collider;
                float parentScale = capsuleCollider2D.transform.parent != null ? capsuleCollider2D.transform.parent.lossyScale.Abs().Max() : 1;
                InternalDrawCapsule((Vector2)capsuleCollider2D.transform.position + ((Vector2)capsuleCollider2D.transform.right * capsuleCollider2D.offset.x) + ((Vector2)capsuleCollider2D.transform.up * capsuleCollider2D.offset.y), capsuleCollider2D.size.ScaleEach(capsuleCollider2D.transform.lossyScale.Abs()), capsuleCollider2D.direction, capsuleCollider2D.transform.rotation.eulerAngles.z * parentScale, solid, colorSetting);
            }
            else if (collider is PolygonCollider2D)
            {
                PolygonCollider2D polygonCollider2D = (PolygonCollider2D)collider;
                Vector2 parentScale = polygonCollider2D.transform.parent != null ? polygonCollider2D.transform.parent.lossyScale : Vector2.one;
                InternalDrawPolygon(polygonCollider2D.points.Select(x => x.ScaleEach(polygonCollider2D.transform.localScale).Rotate(polygonCollider2D.transform.rotation.eulerAngles.z).ScaleEach(parentScale) + (Vector2)polygonCollider2D.transform.position).ToList().ToList(), solid, colorSetting);
            }
            else if (collider is EdgeCollider2D)
            {
                EdgeCollider2D edgeCollider2D = (EdgeCollider2D)collider;
                Vector2 parentScale = edgeCollider2D.transform.parent != null ? edgeCollider2D.transform.parent.lossyScale : Vector2.one;
                InternalDrawPath(edgeCollider2D.points.Select(x => x.ScaleEach(edgeCollider2D.transform.localScale).Rotate(edgeCollider2D.transform.rotation.eulerAngles.z).ScaleEach(parentScale) + (Vector2)edgeCollider2D.transform.position).ToList(), false, colorSetting);
            }
            else
            {
                Debug.LogError($"GLGizmos cannot draw type of {collider.GetType()}");
            }
        }

        #region ### Text
        /// <summary>
        /// Returns the center position of a box given an anchor position, size, rotation, and position pivot type
        /// </summary>
        public static Vector2 GetBoxPositionByPivot(Vector2 anchorPosition, Vector2 size, float rotation, PositionPivot positionPivot)
        {
            size = size.Abs();
            Vector2 RotatedTextBox(Vector2 textBox) => (Vector2.right * textBox.x).Rotate(rotation) + (Vector2.up * textBox.y).Rotate(rotation);
            Vector2 pos = positionPivot switch
            {
                PositionPivot.TopLeft => anchorPosition + RotatedTextBox(size.ScaleEach(.5f, -.5f)),
                PositionPivot.TopRight => anchorPosition + RotatedTextBox(size.ScaleEach(-.5f, -.5f)),
                PositionPivot.BottomLeft => anchorPosition + RotatedTextBox(size.ScaleEach(.5f, .5f)),
                PositionPivot.BottomRight => anchorPosition + RotatedTextBox(size.ScaleEach(-.5f, .5f)),

                PositionPivot.Top => anchorPosition + RotatedTextBox(size.ScaleEach(0, -.5f)),
                PositionPivot.Bottom => anchorPosition + RotatedTextBox(size.ScaleEach(0, .5f)),
                PositionPivot.Left => anchorPosition + RotatedTextBox(size.ScaleEach(.5f, 0)),
                PositionPivot.Right => anchorPosition + RotatedTextBox(size.ScaleEach(-.5f, 0)),

                _ => anchorPosition
            };

            return pos;
        }

        
        /// <summary>
        /// Draws text based on TextMeshPro font asset. If font is null, a default font will be used
        /// </summary>
        public static void DrawText(string text, Vector2 position, TMP_FontAsset font, float fontSize, TextBoxParams textBoxParams = new(), Color? colorSetting = null)
            => AddAction(() => InternalDrawText(text, position, font, fontSize, textBoxParams, colorSetting));
        public static void InternalDrawText(string text, Vector2 position, TMP_FontAsset font, float fontSize, TextBoxParams textBoxParams, Color? colorSetting = null)
        {
            textBoxParams.textBoxSize = textBoxParams.textBoxSize.Abs();
            Vector2 scale = textBoxParams.scale ?? Vector2.one;
            TextAlignmentOptions alignment = textBoxParams.alignment ?? TextAlignmentOptions.Center;

            tmp.enabled = true;
            tmp.text = text;
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontSizeMax = fontSize;
            tmp.fontStyle = textBoxParams.fontStyle;
            tmp.rectTransform.sizeDelta = textBoxParams.textBoxSize;
            tmp.textWrappingMode = textBoxParams.textBoxSize == Vector2.zero ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            tmp.alignment = alignment;
            tmp.enableAutoSizing = textBoxParams.fitTextToBox;
            tmp.color = (Color)(colorSetting == null ? color : colorSetting);
            
            tmp.characterSpacing = textBoxParams.characterSpacing;
            tmp.wordSpacing = textBoxParams.wordSpacing;
            tmp.lineSpacing = textBoxParams.lineSpacing;
            tmp.paragraphSpacing = textBoxParams.paragraphSpacing;

            tmp.ForceMeshUpdate();

            Mesh mesh = tmp.mesh;
            Material mat = tmp.fontSharedMaterial;

            Vector2 RotatedTextBox(Vector2 textBox) => (Vector2.right * textBox.x).Rotate(textBoxParams.rotation) + (Vector2.up * textBox.y).Rotate(textBoxParams.rotation);
            Vector2 pos = textBoxParams.positionPivot switch
            {
                PositionPivot.TopLeft => position + RotatedTextBox((textBoxParams.textBoxSize * scale).ScaleEach(.5f, -.5f)),
                PositionPivot.TopRight => position + RotatedTextBox((textBoxParams.textBoxSize * scale).ScaleEach(-.5f, -.5f)),
                PositionPivot.BottomLeft => position + RotatedTextBox((textBoxParams.textBoxSize * scale).ScaleEach(.5f, .5f)),
                PositionPivot.BottomRight => position + RotatedTextBox((textBoxParams.textBoxSize * scale).ScaleEach(-.5f, .5f)),

                PositionPivot.Top => position + RotatedTextBox((textBoxParams.textBoxSize * scale).ScaleEach(0, -.5f)),
                PositionPivot.Bottom => position + RotatedTextBox((textBoxParams.textBoxSize * scale).ScaleEach(0, .5f)),
                PositionPivot.Left => position + RotatedTextBox((textBoxParams.textBoxSize * scale).ScaleEach(.5f, 0)),
                PositionPivot.Right => position + RotatedTextBox((textBoxParams.textBoxSize * scale).ScaleEach(-.5f, 0)),

                _ => position
            };

            Quaternion rot = Quaternion.Euler(0, 0, textBoxParams.rotation);
            Vector2 scl = new Vector3(scale.x, scale.y, 1);

            mat.SetPass(0);
            Graphics.DrawMeshNow(
                mesh,
                Matrix4x4.TRS(pos, rot, scl)
            );

            GLmat.SetPass(0);
            tmp.text = "";
            tmp.enabled = false;
        }
        #endregion

        void CreateGLMaterial()
        {
            if (GLmat == null)
            {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                GLmat = new Material(shader);
                GLmat.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                GLmat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                GLmat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                GLmat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes
                GLmat.SetInt("_ZWrite", 0);
            }

            if (tmp == null)
            {
                GameObject tmpGO = new GameObject("GLGizmos_TMP_Reference");
                tmpGO.hideFlags = HideFlags.HideAndDontSave;
                tmp = tmpGO.AddComponent<TextMeshPro>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSizeMin = 0;
                tmp.enabled = false;
            }
        }

        void DestroyGLMaterial()
        {
            if (GLmat != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(GLmat);
                }
                else
                {
                    DestroyImmediate(GLmat);
                }
            }

            if (tmp != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(tmp.gameObject);
                    Destroy(tmp);
                }
                else
                {
                    DestroyImmediate(tmp.gameObject);
                    DestroyImmediate(tmp);
                }
            }
        }
    }
}

namespace GLGizmosExtensions
{
    public static class Extensions
    {
        // MathEX
        public enum AngleUnits { Degrees, Radians };

        static float AngleUnitConversion(float value, AngleUnits unitsFrom, AngleUnits unitsTo)
        {
            string unitString = "" + (int)unitsFrom + "" + (int)unitsTo;
            switch (unitString)
            {
                case "01": /*Degrees -> Radians*/ return Mathf.Deg2Rad * value;
                case "10": /*Radians -> Degrees*/ return Mathf.Rad2Deg * value;
                default: return value;
            }
        }

        public static float Remap(float iMin, float iMax, float oMin, float oMax, float value)
        {
            float t = InverseLerp(iMin, iMax, value);
            return Lerp(oMin, oMax, t);
        }
        static float InverseLerp(float a, float b, float value) => (value - a) / (b - a);
        static float Lerp(float a, float b, float t) => (1f - t) * a + t * b;

        // Vector Extensions
        public static Vector2 Rotate90CW(this Vector2 v) => new Vector2(v.y, -v.x);
        public static Vector2 Rotate90CCW(this Vector2 v) => new Vector2(-v.y, v.x);

        public static Vector2 Rotate(this Vector2 v, float angle, AngleUnits units = AngleUnits.Degrees)
        {
            angle = AngleUnitConversion(angle, units, AngleUnits.Radians);

            float ca = Mathf.Cos(angle);
            float sa = Mathf.Sin(angle);
            return new Vector2(ca * v.x - sa * v.y, sa * v.x + ca * v.y);
        }

        public static Vector2 ScaleEach(this Vector2 v, float scaleX, float scaleY) => new Vector2(v.x * scaleX, v.y * scaleY);
        public static Vector2 ScaleEach(this Vector2 v, Vector2 scaleXY) => new Vector2(v.x * scaleXY.x, v.y * scaleXY.y);
        public static Vector2 ZeroNegatives(this Vector2 v) => new Vector2(v.x > 0 ? v.x : 0, v.y > 0 ? v.y : 0);
        public static Vector2 Abs(this Vector3 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        public static Vector2 Abs(this Vector2 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        public static float Max(this Vector2 v) => Mathf.Max(v.x, v.y);
        public static float PositiveAngle(this float f) => f < 0 ? f + 360 : f;
        public static bool FlagEnumContains<T>(this T thisEnum, T testEnum) where T : Enum
        {
            return (Convert.ToInt64(thisEnum) & Convert.ToInt64(testEnum)) != 0;
        }

        /// <summary>
        /// Finds the intersection point of two 2D line segments.
        /// </summary>
        /// <param name="p1">Start point of Segment 1.</param>
        /// <param name="p2">End point of Segment 1.</param>
        /// <param name="p3">Start point of Segment 2.</param>
        /// <param name="p4">End point of Segment 2.</param>
        /// <param name="intersectionPoint">The computed intersection point if one exists.</param>
        /// <returns>True if the segments intersect within their bounds [0, 1], False otherwise.</returns>
        public static bool FindSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersectionPoint, bool withinSegment = true)
        {
            intersectionPoint = Vector2.zero;

            // Directions of the segments
            Vector2 v1 = p2 - p1;
            Vector2 v2 = p4 - p3;

            // Difference between start points
            Vector2 p3_minus_p1 = p3 - p1;

            // Denominator (Determinant)
            // This is the 2D cross product of the direction vectors: det(v1, -v2) or det(v1, v2)
            float denominator = (v1.x * v2.y) - (v1.y * v2.x);

            // --- EDGE CASE 1: PARALLEL or COLINEAR LINES ---
            // If the denominator is close to zero, the lines are parallel or colinear.
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                // For segment intersection, we often treat this as no intersection 
                // unless a specific colinearity overlap check is needed.
                // For simplicity and common use, we return false for parallel/colinear.
                return false;
            }

            // --- SOLVE FOR t and u ---

            // Solve for t (parameter for Segment 1)
            // t = det(p3_minus_p1, -v2) / denominator
            // t = ((p3_minus_p1.x * -v2.y) - (p3_minus_p1.y * -v2.x)) / denominator
            float t = ((p3_minus_p1.x * v2.y) - (p3_minus_p1.y * v2.x)) / denominator;

            // Solve for u (parameter for Segment 2)
            // u = det(v1, p3_minus_p1) / denominator
            float u = ((p3_minus_p1.x * v1.y) - (p3_minus_p1.y * v1.x)) / denominator;

            // --- EDGE CASE 2: INTERSECTION OUTSIDE SEGMENT BOUNDS ---
            // The intersection point is only valid if 0 <= t <= 1 and 0 <= u <= 1
            if ((t >= 0 && t <= 1 && u >= 0 && u <= 1) || !withinSegment)
            {
                // Intersection is within both segments.
                // Calculate the actual point P(t) = p1 + t*v1
                intersectionPoint = p1 + t * v1;
                return true;
            }

            // Intersection exists on the full lines, but not on the segments.
            return false;
        }

        /// <summary>
        /// Finds the intersection point of a 2D line segment (P1-P2) and a ray (starting at P3, direction P4-P3).
        /// </summary>
        /// <param name="p1">Start point of the Line Segment.</param>
        /// <param name="p2">End point of the Line Segment.</param>
        /// <param name="p3">Start point of the Ray (Ray Origin).</param>
        /// <param name="p4">A point defining the direction of the Ray.</param>
        /// <param name="intersectionPoint">The computed intersection point if one exists.</param>
        /// <returns>True if the segment and ray intersect within their bounds, False otherwise.</returns>
        public static bool FindSegmentRayIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersectionPoint)
        {
            intersectionPoint = Vector2.zero;

            // Directions vectors
            Vector2 v1 = p2 - p1; // Direction vector of the Segment
            Vector2 v2 = p4 - p3; // Direction vector of the Ray

            // Vector between start points
            Vector2 p3_minus_p1 = p3 - p1;

            // Denominator (Determinant)
            // If the denominator is zero, the segment and ray are parallel or colinear.
            float denominator = (v1.x * v2.y) - (v1.y * v2.x);

            // --- EDGE CASE 1: PARALLEL or COLINEAR LINES ---
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                // For general use, we assume no unique intersection point in this case.
                return false;
            }

            // --- SOLVE FOR t and u ---

            // Solve for t (parameter for the Segment P1-P2)
            // t = det(p3_minus_p1, v2) / denominator
            float t = ((p3_minus_p1.x * v2.y) - (p3_minus_p1.y * v2.x)) / denominator;

            // Solve for u (parameter for the Ray P3-P4)
            // u = det(v1, p3_minus_p1) / denominator
            float u = ((p3_minus_p1.x * v1.y) - (p3_minus_p1.y * v1.x)) / denominator;

            // --- EDGE CASE 2: CONSTRAINTS CHECK ---

            // 1. Segment Constraint: t must be within [0, 1]
            bool t_valid = (t >= 0 && t <= 1);

            // 2. Ray Constraint: u must be non-negative [0, infinity)
            bool u_valid = (u >= 0);

            if (t_valid && u_valid)
            {
                // Intersection is valid for both the segment and the ray.
                // Calculate the actual point P(t) = p1 + t*v1
                intersectionPoint = p1 + t * v1;
                return true;
            }

            // Intersection exists on the full lines, but not on the segment or the ray.
            return false;
        }

        /// <summary>
        /// Finds the first (closest) intersection point between a ray and a circle.
        /// </summary>
        /// <param name="rayOrigin">Starting point of the ray (P0).</param>
        /// <param name="rayDirection">Direction vector of the ray (must be normalized).</param>
        /// <param name="center">Center of the circle (C).</param>
        /// <param name="radius">Radius of the circle (r).</param>
        /// <param name="intersectionPoint">The computed first intersection point.</param>
        /// <returns>True if an intersection exists, False otherwise.</returns>
        public static bool FindFirstRayCircleIntersection(
            Vector2 rayOrigin, Vector2 rayDirection, Vector2 center, float radius, out Vector2 intersectionPoint)
        {
            intersectionPoint = Vector2.zero;

            // 1. Define vector v (from circle center to ray origin: P0 - C)
            Vector2 v = rayOrigin - center;

            // 2. Define A, B, C for the quadratic equation At^2 + Bt + C = 0
            // A = d . d (Since rayDirection should be normalized, A = 1)
            float A = 1f;

            // B = 2 * (d . v)
            float B = 2f * Vector2.Dot(rayDirection, v);

            // C = (v . v) - r^2
            float C = Vector2.Dot(v, v) - (radius * radius);

            // 3. Calculate the Discriminant (D = B^2 - 4AC)
            float discriminant = (B * B) - (4f * A * C);

            // --- EDGE CASE 1: NO REAL INTERSECTION ---
            // If D < 0, the line does not intersect the circle (misses entirely).
            if (discriminant < 0)
            {
                return false;
            }

            // 4. Calculate the two potential t values
            float sqrtDiscriminant = Mathf.Sqrt(discriminant);

            // t1 is the closest point (using the minus sign)
            float t1 = (-B - sqrtDiscriminant) / (2f * A);

            // t2 is the farthest point (using the plus sign)
            float t2 = (-B + sqrtDiscriminant) / (2f * A);

            // 5. Select the smallest non-negative t value

            // --- EDGE CASE 2: INTERSECTION BEHIND THE RAY START ---

            // Case 1: t1 >= 0
            if (t1 >= 0)
            {
                // t1 is non-negative and is the smallest positive t, so it's the first hit.
                float t = t1;
                intersectionPoint = rayOrigin + (rayDirection * t);
                return true;
            }
            // Case 2: t1 < 0, but t2 >= 0
            else if (t2 >= 0)
            {
                // t1 is behind the ray, but t2 is in front. This happens if the ray 
                // starts *inside* the circle. t2 is the exit point.
                float t = t2;
                intersectionPoint = rayOrigin + (rayDirection * t);
                return true;
            }

            // Case 3: Both t1 and t2 are negative (Circle is entirely behind the ray origin)
            return false;
        }
    }
}

