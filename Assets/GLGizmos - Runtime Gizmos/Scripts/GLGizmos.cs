using GLGizmosExtensions;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace GLDebug
{
    #region ### Public Structs
    public enum ArcCloseType { None, Flat, Center, Edge }
    public enum BorderType { Centered, Outside, Inside }
    public enum PositionPivot
    {
        Center, Left, Right,
        Top, TopLeft, TopRight,
        Bottom, BottomLeft, BottomRight,
    }
    //public enum Polygon { Circle, Triangle, Square, Hexagon }

    [Serializable]
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

    [Serializable]
    public struct BoxParams
    {
        public bool solid;
        public float rotation;

        public float borderWidth;
        public BorderType borderType;
        public bool solidBorder;

        [Range(0f, 1f)]
        public float roundCorners01;
        public bool hideBox;
    }

    [Serializable]
    public struct CircleParams
    {
        public int numEdges;
        public bool solid;
        public bool dashed;

        [Range(-180, 180)]
        public float arcAngle;
        public float rotation;
        public ArcCloseType arcCloseType;
        public bool roundCenter;

        public float borderWidth;
        public BorderType borderType;
    }
    #endregion

    [ExecuteInEditMode]
    public class GLGizmos : MonoBehaviour
    {
        #region ### Shape Structs
        public struct ShapeModifier
        {
            int index;
            int count;
            bool valid;
            public ShapeModifier(int index, int count = 1, bool valid = true)
            {
                this.index = index;
                this.count = count;
                this.valid = valid;
            }

            public ShapeModifier SetColor(Color color)
            {
                if (!valid) { return this; }
                for (int i = 0; i < count; i++)
                {
                    ShapeSettings settings = shapeSettingsList[index + i];
                    settings.SetColor(color);
                    shapeSettingsList[index + i] = settings;

                    if (settings.isText)
                        continue;
                    for (int j = 0; j < settings.numberOfDefinitions; j++)
                    {
                        ShapeDefinition definition = shapeDefinitionList[settings.definitionListIndex + j];
                        definition.NullOverrideColor();
                        shapeDefinitionList[settings.definitionListIndex + j] = definition;
                    }
                }
                return this;
            }
            public ShapeModifier SetLayer(int layer)
            {
                if (!valid) { return this; }
                for (int i = 0; i < count; i++)
                {
                    ShapeSettings settings = shapeSettingsList[index + i];
                    settings.SetLayer(layer);
                    shapeSettingsList[index + i] = settings;
                }
                return this;
            }
            public ShapeModifier SetOrigin(Vector3 origin)
            {
                if (!valid) { return this; }
                for (int i = 0; i < count; i++)
                {
                    ShapeSettings settings = shapeSettingsList[index + i];
                    settings.SetOrigin(origin);
                    shapeSettingsList[index + i] = settings;
                }
                return this;
            }
            public ShapeModifier SetLookRotation(Vector3 forward, Vector3 up)
            {
                if (!valid) { return this; }
                for (int i = 0; i < count; i++)
                {
                    ShapeSettings settings = shapeSettingsList[index + i];
                    settings.SetLookRotation(forward, up);
                    shapeSettingsList[index + i] = settings;
                }
                return this;
            }
            public ShapeModifier SetLookRotation(Vector3 forward)
            {
                if (!valid) { return this; }
                for (int i = 0; i < count; i++)
                {
                    ShapeSettings settings = shapeSettingsList[index + i];
                    settings.SetLookRotation(forward, Vector3.up);
                    shapeSettingsList[index + i] = settings;
                }
                return this;
            }
            public ShapeModifier SetLookRotation(Transform t)
            {
                if (!valid) { return this; }
                for (int i = 0; i < count; i++)
                {
                    ShapeSettings settings = shapeSettingsList[index + i];
                    settings.SetLookRotation(t);
                    shapeSettingsList[index + i] = settings;
                }
                return this;
            }
            //public ShapeModifier SetLookRotationToCamera()
            //{
            //    if (!valid) { return this; }
            //    for (int i = 0; i < count; i++)
            //    {
            //        ShapeSettings settings = shapeSettingsList[index + i];
            //        settings.SetLookRotationToCamera();
            //        shapeSettingsList[index + i] = settings;
            //    }
            //    return this;
            //}
        }
        private struct ShapeSettings : IComparable<ShapeSettings>
        {
            public int layer { get; private set; }
            public Color color { get; private set; }
            public Vector3 origin { get; private set; }
            public Vector3 up { get; private set; }
            public Vector3 forward { get; private set; }
            public bool faceCamera { get; private set; }

            public int index { get; private set; }
            public int definitionListIndex;
            public int numberOfDefinitions;
            public bool isText { get; private set; }

            public ShapeSettings(int index, int layer, Color color, Vector3 origin, Vector3 up, Vector3 forward, int definitionListIndex, int numberOfDefinitions, bool isText = false)
            {
                this.index = index;
                this.layer = layer;
                this.color = color;
                this.origin = origin;
                this.up = up;
                this.forward = forward;
                this.faceCamera = false;
                this.definitionListIndex = definitionListIndex;
                this.numberOfDefinitions = numberOfDefinitions;
                this.isText = isText;
            }

            public void SetColor(Color color) => this.color = color;
            public void SetLayer(int layer) => this.layer = layer;
            public void SetOrigin(Vector3 origin) => this.origin = origin;
            public void SetLookRotation(Vector3 forward, Vector3 up)
            {
                this.up = up;
                this.forward = forward;
                faceCamera = false;
            }
            public void SetLookRotation(Transform t)
            {
                this.up = t.up;
                this.forward = t.forward;
                faceCamera = false;
            }
            public void SetLookRotationToCamera() => faceCamera = true;

            public int CompareTo(ShapeSettings x)
            {
                bool equalLayers = this.layer == x.layer;
                return equalLayers switch
                {
                    false => this.layer.CompareTo(x.layer),
                    true => this.index.CompareTo(x.index),
                };
            }
        }
        private struct ShapeDefinition
        {
            public int shapeType { get; private set; }
            public bool wireframe { get; private set; }
            public int vertexListIndex;
            public int numberOfVertices { get; private set; }
            public Color? overrideColor { get; private set; }

            public ShapeDefinition(int shapeType, bool wireframe, int vertexListIndex, int numberOfVertices, Color? color = null)
            {
                this.shapeType = shapeType;
                this.wireframe = wireframe;
                this.vertexListIndex = vertexListIndex;
                this.numberOfVertices = numberOfVertices;
                this.overrideColor = color;
            }

            public void NullOverrideColor() => overrideColor = null;
        }

        private struct TextDefinition
        {
            public string text { get; private set; }
            public TMP_FontAsset font { get; private set; }
            public float fontSize;
            public TextBoxParams textBoxParams;

            public TextDefinition(string text, TMP_FontAsset font, float fontSize, TextBoxParams textBoxParams)
            {
                this.text = text;
                this.font = font;
                this.fontSize = fontSize;
                this.textBoxParams = textBoxParams;
            }
        }
        #endregion

        private static Material GLmat;
        private static TextMeshPro tmp;

        private static Color color = Color.white;
        private static int drawLayer = 0;
        private static Vector3 coordinateUp = Vector3.up;
        private static Vector3 coordinateForward = Vector3.forward;
        private static Vector3 coordinateOrigin = Vector3.zero;

        private static List<ShapeSettings> shapeSettingsList = new();
        private static List<ShapeDefinition> shapeDefinitionList = new();
        private static List<Vector3> verticesList = new();
        private static List<TextDefinition> textList = new();

        private static List<GLGizmosComponent> GLGizmoComponents = new();

        private const float Min_Max_Bias = 1;

        // horrible experiement gone wrong, do not use batching!
        private static bool batchDrawCalls = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            shapeSettingsList.Clear();
            shapeDefinitionList.Clear();
            verticesList.Clear();
            textList.Clear();
            GLGizmoComponents.Clear();

            color = Color.white;
            drawLayer = 0;
            coordinateUp = Vector3.up;
            coordinateForward = Vector3.forward;
            coordinateOrigin = Vector3.zero;

            GLmat = null;
            tmp = null;
            batchDrawCalls = false;
        }

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
            CreateGLMaterial();
            ResetSettings();
            ClearLists();
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
            DestroyGLMaterial();
            ClearLists();
            batchDrawCalls = false;
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
            if (GLmat == null)
                CreateGLMaterial();

            GL.wireframe = false;
        }

        private void OnPostRender()
        {
            foreach (var gizmoComponent in GLGizmoComponents)
            {
                gizmoComponent.ReadGizmos();
            }

            if (shapeSettingsList.Count != 0)
            {
                shapeSettingsList.HybridSort();

                // horrible experiement gone wrong, do not use batching!
                if (batchDrawCalls)
                {
                    ReorderLists();
                    BatchDrawCalls();
                }

                GLmat.SetPass(0);

                Matrix4x4 matrix = Matrix4x4.TRS(Vector2.zero, Quaternion.LookRotation(coordinateForward, coordinateUp), Vector3.one);
                GL.MultMatrix(matrix);
                GL.PushMatrix();

                foreach (ShapeSettings settings in shapeSettingsList)
                {
                    int startIndex = settings.definitionListIndex;
                    if (!settings.isText)
                    {
                        for (int i = 0; i < settings.numberOfDefinitions; i++)
                        {
                            DrawGL(settings, shapeDefinitionList[startIndex + i]);
                        }
                    }
                    else
                    {
                        DrawGLText(settings, textList[settings.definitionListIndex]);
                    }
                }

                //Debug.Log($"Definitions: {shapeDefinitionList.Count + textList.Count}, Vertices: {verticesList.Count}");

                GL.PopMatrix();

                GL.wireframe = false;
            }

            ClearLists();
            ResetSettings();
        }

        #region ### System
        /// <summary>
        /// Resets layer, color, and matrix orientation
        /// </summary>
        public static void ResetSettings()
        {
            color = Color.white;
            drawLayer = 0;
            coordinateUp = Vector3.up;
            coordinateForward = Vector3.forward;
            coordinateOrigin = Vector3.zero;
        }
        private static void ClearLists()
        {
            shapeSettingsList.Clear();
            shapeDefinitionList.Clear();
            verticesList.Clear();
            textList.Clear();
        }
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
        public static void SetColor(Color newColor) => color = newColor;

        /// <summary>
        /// Sets the global drawLayer parameter of GLGizmos
        /// </summary>
        public static void SetLayer(int layer) => drawLayer = layer;

        /// <summary>
        /// Sets the global origin parameter of GLGizmos (for shapes that do not specify a center position)
        /// </summary>
        public static void SetOrigin(Vector3 origin) => coordinateOrigin = origin;

        /// <summary>
        /// Sets the global look rotation parameter of GLGizmos
        /// </summary>
        public static void SetLookRotation(Vector3 forward, Vector3 up)
        {
            coordinateForward = forward;
            coordinateUp = up;
        }

        /// <summary>
        /// Sets the global look rotation parameter of GLGizmos
        /// </summary>
        public static void SetLookRotation(Transform t)
        {
            coordinateForward = t.forward;
            coordinateUp = t.up;
        }

        private static bool SamePlane(Vector3 forward1, Vector3 forward2, Vector3 up1, Vector3 up2, Vector3 pos1, Vector3 pos2)
        {
            return forward1 == forward2 && up1 == up2 && Vector3.Dot(pos1, forward1) == Vector3.Dot(pos2, forward1);
        }

        private static void BatchDrawCalls()
        {
            bool MergeSettings(int i)
            {
                ShapeSettings settings1 = shapeSettingsList[i];
                ShapeSettings settings2 = shapeSettingsList[i + 1];

                if (settings1.layer != settings2.layer ||
                    settings1.color != settings2.color ||
                    settings1.isText || settings2.isText ||
                    !SamePlane(settings1.forward, settings2.forward, settings1.up, settings2.up, settings1.origin, settings2.origin))
                {
                    return false;
                }

                ShapeSettings newSettings = new ShapeSettings(
                    index: settings1.index,
                    layer: settings1.layer,
                    color: settings1.color,
                    origin: settings1.origin,
                    up: settings1.up,
                    forward: settings1.forward,
                    definitionListIndex: settings1.definitionListIndex,
                    numberOfDefinitions: settings1.numberOfDefinitions + settings2.numberOfDefinitions
                );

                shapeSettingsList[i] = newSettings;
                shapeSettingsList.RemoveAt(i + 1);

                // update vertices
                int startDefinitionIndex = settings2.definitionListIndex;
                for (int j = 0; j < settings2.numberOfDefinitions; j++)
                {
                    int startVertexIndex = shapeDefinitionList[startDefinitionIndex + j].vertexListIndex;
                    for (int k = 0; k < shapeDefinitionList[startDefinitionIndex + j].numberOfVertices; k++)
                    {
                        verticesList[startVertexIndex + k] += (settings2.origin - settings1.origin).Rotate(Vector3.SignedAngle(settings1.up, Vector3.up, settings1.forward), settings1.forward);
                    }
                }

                return true;
            }

            void MergeDefinitions(int i)
            {
                int numMerged = 0;
                ShapeSettings settings = shapeSettingsList[i];

                for (int j = 0; j < settings.numberOfDefinitions - 1; j++)
                {
                    int definitionIndex = settings.definitionListIndex + j;
                    ShapeDefinition definition1 = shapeDefinitionList[definitionIndex];
                    ShapeDefinition definition2 = shapeDefinitionList[definitionIndex + 1];

                    if (definition1.shapeType != definition2.shapeType ||
                        definition1.wireframe != definition2.wireframe ||
                        definition1.overrideColor != definition2.overrideColor ||
                        definition1.shapeType == GL.LINE_STRIP || definition2.shapeType == GL.LINE_STRIP)
                        continue;

                    ShapeDefinition newDefinition = new ShapeDefinition(
                        shapeType: definition1.shapeType,
                        wireframe: definition1.wireframe,
                        vertexListIndex: definition1.vertexListIndex,
                        numberOfVertices: definition1.numberOfVertices + definition2.numberOfVertices,
                        color: definition1.overrideColor
                    );

                    shapeDefinitionList[definitionIndex] = newDefinition;
                    shapeDefinitionList.RemoveAt(definitionIndex + 1);
                    settings.numberOfDefinitions--;
                    shapeSettingsList[i] = settings;
                    j--;
                    numMerged++;
                }

                if (numMerged > 0)
                {
                    for (int j = i + 1; j < shapeSettingsList.Count; j++)
                    {
                        ShapeSettings tempShape = shapeSettingsList[j];

                        if (tempShape.isText)
                            continue;

                        tempShape.definitionListIndex -= numMerged;
                        shapeSettingsList[j] = tempShape;
                    }
                }
            }

            for (int i = 0; i < shapeSettingsList.Count - 1; i++)
            {
                if (MergeSettings(i))
                {
                    i--;
                }
            }

            for (int i = 0; i < shapeSettingsList.Count; i++)
            {
                if (!shapeSettingsList[i].isText)
                    MergeDefinitions(i);
            }
        }

        private static void ReorderLists()
        {
            int definitionCount = shapeDefinitionList.Count;
            int vertexCount = verticesList.Count;

            int definitionIndex = 0;
            int vertexIndex = 0;
            for(int i = 0; i < shapeSettingsList.Count; i++)
            {
                ShapeSettings settings = shapeSettingsList[i];

                if (settings.isText)
                    continue;

                for (int j = 0; j < settings.numberOfDefinitions; j++)
                {
                    ShapeDefinition definition = shapeDefinitionList[settings.definitionListIndex + j];
                    
                    for (int k = 0; k < definition.numberOfVertices; k++)
                    {
                        verticesList.Add(verticesList[definition.vertexListIndex + k]);
                    }

                    definition.vertexListIndex = vertexIndex;
                    shapeDefinitionList.Add(definition);
                    vertexIndex += definition.numberOfVertices;
                }

                settings.definitionListIndex = definitionIndex;
                shapeSettingsList[i] = settings;
                definitionIndex += settings.numberOfDefinitions;
            }

            shapeDefinitionList.RemoveRange(0, definitionCount);
            verticesList.RemoveRange(0, vertexCount);
        }
        #endregion

        #region ### Draw Overhead
        private static void DrawGL(ShapeSettings settings, ShapeDefinition definition)
        {
            GL.wireframe = definition.wireframe;
            int startIndex = definition.vertexListIndex;

            Matrix4x4 matrix = Matrix4x4.TRS(settings.origin, Quaternion.LookRotation(settings.forward, settings.up), Vector3.one);
            GL.MultMatrix(matrix);

            GL.Begin(definition.shapeType);

            GL.Color(definition.overrideColor ?? settings.color);
            for (int i = 0; i < definition.numberOfVertices; i++)
            {
                GL.Vertex(verticesList[startIndex + i]);
            }
            GL.End();
        }
        private static void DrawGLText(ShapeSettings settings, TextDefinition definition)
        {
            GL.wireframe = false;
            Vector2 scale = definition.textBoxParams.scale ?? Vector2.one;
            TextAlignmentOptions alignment = definition.textBoxParams.alignment ?? TextAlignmentOptions.Center;

            //tmp.enabled = true;
            tmp.SetText(definition.text);
            tmp.font = definition.font;
            tmp.fontSize = definition.fontSize;
            tmp.fontSizeMax = definition.fontSize;
            tmp.fontStyle = definition.textBoxParams.fontStyle;
            tmp.rectTransform.sizeDelta = definition.textBoxParams.textBoxSize;
            tmp.textWrappingMode = definition.textBoxParams.textBoxSize == Vector2.zero ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            tmp.alignment = alignment;
            tmp.enableAutoSizing = definition.textBoxParams.fitTextToBox;
            tmp.color = settings.color;

            tmp.characterSpacing = definition.textBoxParams.characterSpacing;
            tmp.wordSpacing = definition.textBoxParams.wordSpacing;
            tmp.lineSpacing = definition.textBoxParams.lineSpacing;
            tmp.paragraphSpacing = definition.textBoxParams.paragraphSpacing;

            tmp.ForceMeshUpdate();

            Vector2 RotatedTextBox(Vector2 textBox) => (Vector2.right * textBox.x).Rotate(definition.textBoxParams.rotation) + (Vector2.up * textBox.y).Rotate(definition.textBoxParams.rotation);
            Vector3 pos = definition.textBoxParams.positionPivot switch
            {
                PositionPivot.TopLeft => settings.origin + (Vector3)RotatedTextBox((definition.textBoxParams.textBoxSize * scale).ScaleEach(.5f, -.5f)),
                PositionPivot.TopRight => settings.origin + (Vector3)RotatedTextBox((definition.textBoxParams.textBoxSize * scale).ScaleEach(-.5f, -.5f)),
                PositionPivot.BottomLeft => settings.origin + (Vector3)RotatedTextBox((definition.textBoxParams.textBoxSize * scale).ScaleEach(.5f, .5f)),
                PositionPivot.BottomRight => settings.origin + (Vector3)RotatedTextBox((definition.textBoxParams.textBoxSize * scale).ScaleEach(-.5f, .5f)),

                PositionPivot.Top => settings.origin + (Vector3)RotatedTextBox((definition.textBoxParams.textBoxSize * scale).ScaleEach(0, -.5f)),
                PositionPivot.Bottom => settings.origin + (Vector3)RotatedTextBox((definition.textBoxParams.textBoxSize * scale).ScaleEach(0, .5f)),
                PositionPivot.Left => settings.origin + (Vector3)RotatedTextBox((definition.textBoxParams.textBoxSize * scale).ScaleEach(.5f, 0)),
                PositionPivot.Right => settings.origin + (Vector3)RotatedTextBox((definition.textBoxParams.textBoxSize * scale).ScaleEach(-.5f, 0)),

                _ => settings.origin
            };

            Quaternion rot = Quaternion.Euler(0, 0, definition.textBoxParams.rotation);
            Vector2 scl = new Vector3(scale.x, scale.y, 1);

            Mesh mesh = tmp.mesh;
            Material mat = tmp.fontSharedMaterial;
            mat.SetPass(0);
            Graphics.DrawMeshNow(
                mesh,
                Matrix4x4.TRS(pos, rot, scl)
            );

            GLmat.SetPass(0);
        }

        private static void _NewShape(Vector3 origin, int definitionCount)
        {
            int settingsListIndex = shapeSettingsList.Count;
            int definitionListIndex = shapeDefinitionList.Count;
            ShapeSettings settings = new ShapeSettings(
                index: settingsListIndex,
                layer: drawLayer,
                color: color,
                origin: origin,
                up: coordinateUp,
                forward: coordinateForward,
                definitionListIndex: definitionListIndex,
                numberOfDefinitions: definitionCount
            );

            shapeSettingsList.Add(settings);
        }

        private static void _NewShapeText(Vector3 origin, int definitionCount)
        {
            int settingsListIndex = shapeSettingsList.Count;
            int textListIndex = textList.Count;
            ShapeSettings settings = new ShapeSettings(
                index: settingsListIndex,
                layer: drawLayer,
                color: color,
                origin: origin,
                up: coordinateUp,
                forward: coordinateForward,
                definitionListIndex: textListIndex,
                numberOfDefinitions: 1,
                isText: true
            );

            shapeSettingsList.Add(settings);
        }

        private static ShapeModifier _NewShapeModifierCount(int count = 1)
        {
            if (count > 0)
                return _NewShapeModifierBacktracked(count);
            else
                return _NullShapeModifier();
        }
        private static ShapeModifier _NewShapeModifierCurrent(int count = 1) => new ShapeModifier(shapeSettingsList.Count, count);
        private static ShapeModifier _NewShapeModifierBacktracked(int count = 1) => new ShapeModifier(shapeSettingsList.Count - count, count);
        private static ShapeModifier _NullShapeModifier() => new ShapeModifier(0, 0, false);
        #endregion

        #region ### Basic Shape Definitions
        private static void _OpenBox(Vector3 offset, Vector2 size, float rotation, Color? overrideColor = null)
            => _OpenBox(offset, Quaternion.identity, size, rotation, overrideColor);
        private static void _OpenBox(Vector3 offset, Quaternion lookRotation, Vector2 size, float rotation, Color ? overrideColor = null)
        {
            // definition
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.LINE_STRIP,
                wireframe: true,
                vertexListIndex: vertexListIndex,
                numberOfVertices: 5,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            int signX = -1;
            int signY = -1;

            float halfSizeX = size.x / 2;
            float halfSizeY = size.y / 2;

            bool flipY = true;
            for (int i = 0; i < 5; i++)
            {
                verticesList.Add(offset + lookRotation * ((Vector3)new Vector2(signX * halfSizeX, signY * halfSizeY).Rotate(rotation)));

                if (flipY)
                    signY *= -1;
                else
                    signX *= -1;

                flipY = !flipY;
            }
        }
        public static void _SolidBox(Vector3 offset, Vector2 size, float rotation, Color? overrideColor = null)
            => _SolidBox(offset, Quaternion.identity, size, rotation, overrideColor);
        public static void _SolidBox(Vector3 offset, Quaternion lookRotation, Vector2 size, float rotation, Color ? overrideColor = null)
        {
            // definition
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.QUADS,
                wireframe: false,
                vertexListIndex: vertexListIndex,
                numberOfVertices: 4,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            int signX = -1;
            int signY = -1;

            float halfSizeX = size.x / 2;
            float halfSizeY = size.y / 2;

            bool flipY = true;
            for (int i = 0; i < 4; i++)
            {
                verticesList.Add(offset + lookRotation * ((Vector3)new Vector2(signX * halfSizeX, signY * halfSizeY).Rotate(rotation)));

                if (flipY)
                    signY *= -1;
                else
                    signX *= -1;

                flipY = !flipY;
            }
        }
        private static void _PartialOpenBox(Vector3 offset, Vector2 size, float rotation, bool top, bool bottom, bool left, bool right, Color? overrideColor = null)
            => _PartialOpenBox(offset, Quaternion.identity, size, rotation, top, bottom, left, right, overrideColor);
        private static void _PartialOpenBox(Vector3 offset, Quaternion lookRotation, Vector2 size, float rotation, bool top, bool bottom, bool left, bool right, Color ? overrideColor = null)
        {
            int count = 0;
            if (top) count += 2;
            if (bottom) count += 2;
            if (left) count += 2;
            if (right) count += 2;

            // definition
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.LINES,
                wireframe: true,
                vertexListIndex: vertexListIndex,
                numberOfVertices: count,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            Vector3 topRight = lookRotation * ((Vector3)new Vector2(size.x / 2, size.y / 2).Rotate(rotation)) + offset;
            Vector3 topLeft = lookRotation * ((Vector3)new Vector2(-size.x / 2, size.y / 2).Rotate(rotation)) + offset;
            Vector3 bottomLeft = lookRotation * ((Vector3)new Vector2(-size.x / 2, -size.y / 2).Rotate(rotation)) + offset;
            Vector3 bottomRight = lookRotation * ((Vector3)new Vector2(size.x / 2, -size.y / 2).Rotate(rotation)) + offset;

            if (top)
            {
                verticesList.Add(topLeft);
                verticesList.Add(topRight);
            }

            if (bottom)
            {
                verticesList.Add(bottomLeft);
                verticesList.Add(bottomRight);
            }

            if (left)
            {
                verticesList.Add(bottomLeft);
                verticesList.Add(topLeft);
            }

            if (right)
            {
                verticesList.Add(bottomRight);
                verticesList.Add(topRight);
            }
        }
        private static void _OpenQuad(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4, Color? overrideColor = null)
            => _OpenQuad(v1, v2, v3, v4, Quaternion.identity, overrideColor);
        private static void _OpenQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Quaternion lookRoation, Color? overrideColor = null)
        {
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.LINE_STRIP,
                wireframe: true,
                vertexListIndex: vertexListIndex,
                numberOfVertices: 5,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            verticesList.Add(lookRoation * v1);
            verticesList.Add(lookRoation * v2);
            verticesList.Add(lookRoation * v3);
            verticesList.Add(lookRoation * v4);
            verticesList.Add(lookRoation * v1);
        }
        private static void _SolidQuad(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4, Color? overrideColor = null)
            => _SolidQuad(v1, v2, v3, v4, Quaternion.identity, overrideColor);
        private static void _SolidQuad(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4, Quaternion lookRoation, Color? overrideColor = null)
        {
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.QUADS,
                wireframe: false,
                vertexListIndex: vertexListIndex,
                numberOfVertices: 4,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            verticesList.Add(lookRoation * v1);
            verticesList.Add(lookRoation * v2);
            verticesList.Add(lookRoation * v3);
            verticesList.Add(lookRoation * v4);
        }
        private static void _SolidQuads(NativeList<Vector3> vertices)
            => _SolidQuads(vertices, Vector3.zero, Quaternion.identity);
        private static void _SolidQuads(NativeList<Vector3> vertices, Vector3 offset)
            => _SolidQuads(vertices, offset, Quaternion.identity);
        private static void _SolidQuads(NativeList<Vector3> vertices, Vector3 offset, Quaternion lookRotation)
        {
            int numberOfVertices = vertices.Length - (vertices.Length % 4);

            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.QUADS,
                wireframe: false,
                vertexListIndex: vertexListIndex,
                numberOfVertices: numberOfVertices
            );

            shapeDefinitionList.Add(definition);

            // vertices
            for (int i = 0; i < numberOfVertices; i++)
            {
                verticesList.Add((lookRotation * vertices[i]) + offset);
            }

            vertices.Dispose();
        }
        private static void _OpenCircle(Vector3 offset, float radius, float arcAngle, float offsetAngle, int numEdges, bool dashed = false, Color? overrideColor = null)
            => _OpenCircle(offset, Quaternion.identity, radius, arcAngle, offsetAngle, numEdges, dashed = false, overrideColor);
        private static void _OpenCircle(Vector3 offset, Quaternion lookRotation, float radius, float arcAngle, float offsetAngle, int numEdges, bool dashed = false, Color? overrideColor = null)
        {
            radius = Mathf.Abs(radius);

            numEdges = GetNumEdges(radius, arcAngle, numEdges);

            if (dashed)
            {
                if (numEdges % 2 != 0 && Mathf.Abs(arcAngle) >= 360)
                    numEdges++;
                else if (numEdges % 2 == 0 && Mathf.Abs(arcAngle) < 360)
                    numEdges--;
            }

            // definition
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: dashed ? GL.LINES : GL.LINE_STRIP,
                wireframe: true,
                vertexListIndex: vertexListIndex,
                numberOfVertices: numEdges + 1,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            for (int i = 0; i <= numEdges; i++)
            {
                verticesList.Add(offset + lookRotation * ((Vector3)Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * radius));
            }
        }
        private static void _SolidCircle(Vector3 offset, float radius, float arcAngle, float offsetAngle, int numEdges, Color? overrideColor = null)
        {
            radius = Mathf.Abs(radius);
            numEdges = GetNumEdges(radius, arcAngle, numEdges);

            // definition
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.TRIANGLES,
                wireframe: false,
                vertexListIndex: vertexListIndex,
                numberOfVertices: numEdges * 3,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            Span<Vector3> vertices = stackalloc Vector3[numEdges + 1];

            for (int i = 0; i <= numEdges; i++)
            {
                vertices[i] = offset + (Vector3)Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * radius;
            }

            for (int i = 0; i < numEdges; i++)
            {
                verticesList.Add(offset);
                verticesList.Add(vertices[i]);
                verticesList.Add(vertices[i + 1]);
            }
        }
        private static void _SolidFanOut(Vector3 fanOutPosition, Span<Vector3> vertices, bool connect = false, Color? overrideColor = null)
        {
            int numEdges = vertices.Length - (connect ? 0 : 1);

            // definition
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.TRIANGLES,
                wireframe: false,
                vertexListIndex: vertexListIndex,
                numberOfVertices: numEdges * 3,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            for (int i = 0; i < numEdges; i++)
            {
                verticesList.Add(fanOutPosition);
                verticesList.Add(vertices[i]);
                verticesList.Add(vertices[connect switch
                {
                    false => i + 1,
                    true => (i + 1) % numEdges
                }]);
            }
        }
        private static void _SolidPolygon(Span<Vector3> vertices, Color? overrideColor = null)
        {
            Vector3 averageVertex = Vector3.zero;
            for (int i = 0; i < vertices.Length; i++)
            {
                averageVertex += vertices[i];
            }
            averageVertex /= vertices.Length;
            _SolidFanOut(averageVertex, vertices, true, overrideColor);
        }
        private static void _SolidArc(Vector3 offset, float radius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType, int numEdges, Color? overrideColor = null)
        {
            if (arcCloseType == ArcCloseType.Flat || arcCloseType == ArcCloseType.Edge)
            {
                Vector3 fanOutPosition = Vector3.zero;
                switch (arcCloseType)
                {
                    case ArcCloseType.Flat:
                        fanOutPosition = Vector2.Lerp(Vector2.right.Rotate(offsetAngle) * radius, Vector2.right.Rotate(arcAngle + offsetAngle) * radius, .5f);
                        break;
                    case ArcCloseType.Edge:
                        fanOutPosition = EdgeMinMaxPoint(Vector2.zero, -Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * Min_Max_Bias, Vector2.right.Rotate(offsetAngle) * radius, Vector2.right.Rotate(arcAngle + offsetAngle) * radius, arcAngle);
                        break;
                }

                fanOutPosition += offset;

                radius = Mathf.Abs(radius);
                numEdges = GetNumEdges(radius, arcAngle, numEdges);

                // vertices
                Span<Vector3> vertices = stackalloc Vector3[numEdges + 1];

                for (int i = 0; i <= numEdges; i++)
                {
                    vertices[i] = offset + (Vector3)Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * radius;
                }

                _SolidFanOut(fanOutPosition, vertices, overrideColor: overrideColor);
            }
            else
            {
                _SolidCircle(offset, radius, arcAngle, offsetAngle, numEdges, overrideColor);
            }
        }
        private static void _Lines(ReadOnlySpan<Vector3> vertices, Color? overrideColor = null)
        {
            int count = vertices.Length;
            if (vertices.Length % 2 != 0)
                count -= 1;

            // definition
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.LINES,
                wireframe: true,
                vertexListIndex: vertexListIndex,
                numberOfVertices: count,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            for (int i = 0; i < count; i++)
            {
                verticesList.Add(vertices[i]);
            }
        }
        private static void _LineStrip(ReadOnlySpan<Vector3> vertices, bool connect, Color? overrideColor = null)
        {
            int count = vertices.Length;
            if (connect)
                count += 1;

            // definition
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.LINE_STRIP,
                wireframe: true,
                vertexListIndex: vertexListIndex,
                numberOfVertices: count,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                verticesList.Add(vertices[i]);
            }

            if (connect)
            {
                verticesList.Add(vertices[0]);
            }
        }
        private static void _Triangle(Vector3 v1, Vector3 v2, Vector3 v3, bool solid, Color? overrideColor = null)
        {
            // definition
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.TRIANGLES,
                wireframe: !solid,
                vertexListIndex: vertexListIndex,
                numberOfVertices: 3,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            verticesList.Add(v1);
            verticesList.Add(v2);
            verticesList.Add(v3);
        }
        private static void _Triangles(NativeList<Vector3> vertices, bool solid, Color? overrideColor = null)
        {
            // definition
            int numberOfVertices = vertices.Length - (vertices.Length % 3);
            int vertexListIndex = verticesList.Count;
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: GL.TRIANGLES,
                wireframe: !solid,
                vertexListIndex: vertexListIndex,
                numberOfVertices: numberOfVertices,
                color: overrideColor
            );

            shapeDefinitionList.Add(definition);

            // vertices
            for (int i = 0; i < numberOfVertices; i++)
            {
                verticesList.Add(vertices[i]);
            }

            vertices.Dispose();
        }

        private static void _Text(string text, TMP_FontAsset font, float fontSize, TextBoxParams textBoxParams)
        {
            // definition
            ShapeDefinition definition = new ShapeDefinition
            (
                shapeType: -1,
                wireframe: false,
                vertexListIndex: -1,
                numberOfVertices: 0
            );

            textList.Add(new TextDefinition(text, font, fontSize, textBoxParams));
        }
        #endregion

        #region ### Rectangles
        /// <summary>
        /// Draws an open box at 'position' with 'size' and 'rotation'
        /// </summary>
        public static ShapeModifier DrawOpenBox(Vector3 position, Vector2 size, float rotation = 0)
            => _NewShapeModifierCount(BuildOpenBox(position, size, rotation));
        private static int BuildOpenBox(Vector3 position, Vector2 size, float rotation = 0)
        {
            _NewShape(position, 1);
            _OpenBox(Vector3.zero, size, rotation);
            return 1;
        }

        /// <summary>
        /// Draws a solid box at 'position' with 'size' and 'rotation'
        /// </summary>
        public static ShapeModifier DrawSolidBox(Vector3 position, Vector2 size, float rotation = 0)
            => _NewShapeModifierCount(BuildSolidBox(position, size, rotation));
        private static int BuildSolidBox(Vector3 position, Vector2 size, float rotation = 0)
        {
            _NewShape(position, 1);
            _SolidBox(Vector3.zero, size, rotation);
            return 1;
        }

        /// <summary>
        /// Draws a box with an edge thickness of 'borderWidth'
        /// </summary>
        public static ShapeModifier DrawWeightedBox(Vector3 position, Vector2 size, float borderWidth, BorderType borderType, float rotation = 0)
            => _NewShapeModifierCount(BuildWeightedBox(position, size, borderWidth, borderType, rotation));
        /// <summary>
        /// Draws a box bounded by corner1 and corner2 with an edge thickness of 'borderWidth'
        /// </summary>
        public static ShapeModifier DrawWeightedBoxFromCorners(Vector2 corner1, Vector2 corner2, float borderWidth, BorderType borderType, float rotation = 0)
        {
            Vector2 position = new Vector2((corner2.x + corner1.x) / 2, (corner2.y + corner1.y) / 2);
            Vector2 corner1A = corner1 - position;
            Vector2 corner2A = corner2 - position;
            float hypotenuse = Vector2.Distance(corner1, corner2);
            float angle = Vector2.Angle(Vector2.right.Rotate(rotation), corner2A - corner1A);
            float x = hypotenuse * Mathf.Cos(angle * Mathf.PI / 180f);
            float y = hypotenuse * Mathf.Sin(angle * Mathf.PI / 180f);
            Vector2 size = new Vector2(x, y);
            return _NewShapeModifierCount(BuildWeightedBox(position, size, borderWidth, borderType, rotation));
        }
        private static int BuildWeightedBox(Vector3 position, Vector2 size, float borderWidth, BorderType borderType, float rotation = 0)
        {
            if (borderWidth == 0)
                return BuildOpenBox(position, size, rotation);

            bool outsideFill;
            bool insideFill;
            (size, borderWidth, borderType, insideFill, outsideFill) = AdjustWeightedBoxParams(size, borderWidth, borderType);

            //int count = (insideFill ? 1 : 0) + (outsideFill ? 4 : 0);
            int count = (insideFill ? 1 : 0) + (outsideFill ? 1 : 0);
            _NewShape(position, count);

            if (insideFill)
                _SolidBox(Vector3.zero, size, rotation);

            if (outsideFill)
            {
                Vector2 halfSize = size / 2;

                Vector2 innerTL = new Vector2(-halfSize.x, halfSize.y).Rotate(rotation);
                Vector2 innerTR = new Vector2(halfSize.x, halfSize.y).Rotate(rotation);
                Vector2 innerBL = new Vector2(-halfSize.x, -halfSize.y).Rotate(rotation);
                Vector2 innerBR = new Vector2(halfSize.x, -halfSize.y).Rotate(rotation);

                Vector2 outerTL = (new Vector2(-halfSize.x, halfSize.y) + new Vector2(-borderWidth, borderWidth)).Rotate(rotation);
                Vector2 outerTR = (new Vector2(halfSize.x, halfSize.y) + new Vector2(borderWidth, borderWidth)).Rotate(rotation);
                Vector2 outerBL = (new Vector2(-halfSize.x, -halfSize.y) + new Vector2(-borderWidth, -borderWidth)).Rotate(rotation);
                Vector2 outerBR = (new Vector2(halfSize.x, -halfSize.y) + new Vector2(borderWidth, -borderWidth)).Rotate(rotation);

                _SolidQuads(new NativeList<Vector3>(Allocator.Temp)
                {
                    innerTL, outerTL, outerTR, innerTR,
                    innerTR, outerTR, outerBR, innerBR,
                    innerBR, outerBR, outerBL, innerBL,
                    innerBL, outerBL, outerTL, innerTL
                });
            }

            return 1;
        }

        /// <summary>
        /// Draws an open quad with 'vertices'
        /// </summary>
        private static ShapeModifier DrawOpenQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
            => _NewShapeModifierCount(BuildOpenQuad(v1, v2, v3, v4));
        private static int BuildOpenQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            _NewShape(coordinateOrigin, 1);
            _OpenQuad(v1, v2, v3, v4);
            return 1;
        }

        /// <summary>
        /// Draws a solid quad with 'vertices'
        /// </summary>
        private static ShapeModifier DrawSolidQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
            => _NewShapeModifierCount(BuildSolidQuad(v1, v2, v3, v4));
        private static int BuildSolidQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            _NewShape(coordinateOrigin, 1);
            _SolidQuad(v1, v2, v3, v4);
            return 1;
        }

        /// <summary>
        /// Draws an edge radius outline around a box rotated by 'angle' at 'position' with 'size'
        /// </summary>
        /// <param name="drawOpenBox">draw an open box at 'position'</param>
        /// <param name="borderType">sets if the edge radius extends beyond the box size (outside), is constrained within the box size (inside), or half of either side (centered)</param>
        public static ShapeModifier DrawOpenBoxEdgeRadius(Vector3 position, Vector2 size, float edgeRadius, float rotation, bool drawOpenBox, BorderType borderType = BorderType.Outside)
            => DrawBoxEdgeRadius(position, size.Abs(), edgeRadius, rotation, drawOpenBox, borderType, false);
        /// <summary>
        /// Fills in an edge radius area around a box rotated by 'angle' at 'position' with 'size'
        /// </summary>
        /// <param name="drawSolidBox">draw a solid box at 'position'</param>
        /// <param name="borderType">sets if the edge radius extends beyond the rboxect size (outside), is constrained within the box size (inside), or half of either side (centered)</param>
        public static ShapeModifier DrawSolidBoxEdgeRadius(Vector3 position, Vector2 size, float edgeRadius, float rotation, bool drawSolidBox, BorderType borderType = BorderType.Outside)
            => DrawBoxEdgeRadius(position, size.Abs(), edgeRadius, rotation, drawSolidBox, borderType, true);
        private static ShapeModifier DrawBoxEdgeRadius(Vector3 position, Vector2 size, float edgeRadius, float rotation, bool drawBox, BorderType borderType, bool solid)
        {
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

            if (edgeRadius == 0)
            {
                if (drawBox)
                {
                    if (solid)
                        return _NewShapeModifierCount(BuildSolidBox(position, size, rotation));
                    else
                        return _NewShapeModifierCount(BuildOpenBox(position, size, rotation));
                }
                else
                    return _NullShapeModifier();
            }

            if (size.x < 0)
                size.x = 0;

            if (size.y < 0)
                size.y = 0;

            if (!solid)
                return _NewShapeModifierCount(BuildOpenBoxEdgeRadius(position, size, edgeRadius, rotation, drawBox));
            else
                return _NewShapeModifierCount(BuildSolidBoxEdgeRadius(position, size, edgeRadius, rotation, drawBox));
        }
        private static int BuildOpenBoxEdgeRadius(Vector3 position, Vector2 size, float edgeRadius, float rotation, bool drawBox)
        {
            int count = drawBox ? 2 : 1;
            _NewShape(position, count);

            int numCornerVertices = 8;
            Vector2 halfSize = new Vector2(size.x / 2, size.y / 2);
            Vector2 topRight = halfSize.ScaleEach(1, 1).Rotate(rotation);
            Vector2 topLeft = halfSize.ScaleEach(-1, 1).Rotate(rotation);
            Vector2 bottomLeft = halfSize.ScaleEach(-1, -1).Rotate(rotation);
            Vector2 bottomRight = halfSize.ScaleEach(1, -1).Rotate(rotation);

            Vector2 vectorTopRight = topRight + Vector2.up.Rotate(rotation) * edgeRadius;
            Vector2 vectorTopLeft = topLeft + Vector2.up.Rotate(rotation) * edgeRadius;

            Vector2 vectorLeftUp = topLeft + Vector2.left.Rotate(rotation) * edgeRadius;
            Vector2 vectorLeftDown = bottomLeft + Vector2.left.Rotate(rotation) * edgeRadius;

            Vector2 vectorBottomLeft = bottomLeft + Vector2.down.Rotate(rotation) * edgeRadius;
            Vector2 vectorBottomRight = bottomRight + Vector2.down.Rotate(rotation) * edgeRadius;

            Vector2 vectorRightDown = bottomRight + Vector2.right.Rotate(rotation) * edgeRadius;
            Vector2 vectorRightUp = topRight + Vector2.right.Rotate(rotation) * edgeRadius;

            Span<Vector2> topLeftCornerVertices = stackalloc Vector2[numCornerVertices];
            for (int i = 1; i <= numCornerVertices; i++)
            {
                topLeftCornerVertices[i - 1] = (Vector2.up.Rotate(90f * ((float)i / (float)numCornerVertices) + rotation) * edgeRadius) + topLeft;
            }

            Span<Vector2> bottomLeftCornerVertices = stackalloc Vector2[numCornerVertices];
            for (int i = 1; i <= numCornerVertices; i++)
            {
                bottomLeftCornerVertices[i - 1] = (Vector2.left.Rotate(90f * ((float)i / (float)numCornerVertices) + rotation) * edgeRadius) + bottomLeft;
            }

            Span<Vector2> bottomRightCornerVertices = stackalloc Vector2[numCornerVertices];
            for (int i = 1; i <= numCornerVertices; i++)
            {
                bottomRightCornerVertices[i - 1] = (Vector2.down.Rotate(90f * ((float)i / (float)numCornerVertices) + rotation) * edgeRadius) + bottomRight;
            }

            Span<Vector2> topRightCornerVertices = stackalloc Vector2[numCornerVertices];
            for (int i = 1; i <= numCornerVertices; i++)
            {
                topRightCornerVertices[i - 1] = (Vector2.right.Rotate(90f * ((float)i / (float)numCornerVertices) + rotation) * edgeRadius) + topRight;
            }

            Span<Vector2> eightCorners = stackalloc Vector2[]
            {
                vectorTopRight,
                vectorTopLeft,
                vectorLeftUp,
                vectorLeftDown,
                vectorBottomLeft,
                vectorBottomRight,
                vectorRightDown,
                vectorRightUp
            };
            Span<Vector3> vertices = stackalloc Vector3[numCornerVertices * 4 + 8];

            int index = 0;
            int eightCornersIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    vertices[index++] = eightCorners[eightCornersIndex++];
                }
                for (int k = 0; k < numCornerVertices; k++)
                {
                    vertices[index++] = i switch
                    {
                        0 => topLeftCornerVertices[k],
                        1 => bottomLeftCornerVertices[k],
                        2 => bottomRightCornerVertices[k],
                        3 => topRightCornerVertices[k],
                    };
                }
            }

            _LineStrip(vertices, true);

            if (drawBox)
                _OpenBox(Vector2.zero, size, rotation);

            return 1;
        }
        private static int BuildSolidBoxEdgeRadius(Vector3 position, Vector2 size, float edgeRadius, float rotation, bool drawBox)
        {
            drawBox = drawBox && size.x != 0 && size.y != 0;
            int count = drawBox ? 6 : 5;
            _NewShape(position, count);

            int numCornerVertices = 8;
            Vector2 halfSize = new Vector2(size.x / 2, size.y / 2);
            Vector2 topRight = halfSize.ScaleEach(1, 1).Rotate(rotation);
            Vector2 topLeft = halfSize.ScaleEach(-1, 1).Rotate(rotation);
            Vector2 bottomLeft = halfSize.ScaleEach(-1, -1).Rotate(rotation);
            Vector2 bottomRight = halfSize.ScaleEach(1, -1).Rotate(rotation);

            Vector2 vectorTopRight = topRight + Vector2.up.Rotate(rotation) * edgeRadius;
            Vector2 vectorTopLeft = topLeft + Vector2.up.Rotate(rotation) * edgeRadius;

            Vector2 vectorLeftUp = topLeft + Vector2.left.Rotate(rotation) * edgeRadius;
            Vector2 vectorLeftDown = bottomLeft + Vector2.left.Rotate(rotation) * edgeRadius;

            Vector2 vectorBottomLeft = bottomLeft + Vector2.down.Rotate(rotation) * edgeRadius;
            Vector2 vectorBottomRight = bottomRight + Vector2.down.Rotate(rotation) * edgeRadius;

            Vector2 vectorRightDown = bottomRight + Vector2.right.Rotate(rotation) * edgeRadius;
            Vector2 vectorRightUp = topRight + Vector2.right.Rotate(rotation) * edgeRadius;

            _SolidQuads(new NativeList<Vector3>(Allocator.Temp)
            {
                topLeft, topRight, vectorTopRight, vectorTopLeft,
                topLeft, bottomLeft, vectorLeftDown, vectorLeftUp,
                bottomLeft, bottomRight, vectorBottomRight, vectorBottomLeft,
                topRight, bottomRight, vectorRightDown, vectorRightUp
            });
            _SolidCircle(topRight, edgeRadius, 90, 0 + rotation, numCornerVertices);
            _SolidCircle(topLeft, edgeRadius, 90, 90 + rotation, numCornerVertices);
            _SolidCircle(bottomLeft, edgeRadius, 90, 180 + rotation, numCornerVertices);
            _SolidCircle(bottomRight, edgeRadius, 90, 270 + rotation, numCornerVertices);

            if (drawBox)
                _SolidBox(Vector2.zero, size, rotation);

            return 1;
        }

        /// <summary>
        /// Draws an open plus shape at 'position' with 'size' rotated by 'rotation' and with bar width of 'barWidth'
        /// </summary>
        public static ShapeModifier DrawOpenPlus(Vector3 position, Vector2 size, float barWidth, float rotation, bool useLineWidthAsMultiplier = false)
            => _NewShapeModifierCount(BuildOpenPlus(position, size.Abs(), Mathf.Abs(barWidth), rotation, useLineWidthAsMultiplier));
        public static int BuildPlusZeroWidth(Vector3 position, Vector2 size, float rotation)
        {
            _NewShape(position, 1);

            Vector2 top = (Vector2.up * size / 2).Rotate(rotation);
            Vector2 right = (Vector2.right * size / 2).Rotate(rotation);
            _Lines(stackalloc Vector3[]
            {
                    top,
                    -top,
                    right,
                    -right
            });

            return 1;
        }
        public static int BuildOpenPlus(Vector3 position, Vector2 size, float barWidth, float rotation, bool useLineWidthAsMultiplier = false)
        {
            float lineX = useLineWidthAsMultiplier ? size.x * barWidth : barWidth;
            float lineY = useLineWidthAsMultiplier ? size.y * barWidth : barWidth;

            if (barWidth == 0)
                return BuildPlusZeroWidth(position, size, rotation);

            _NewShape(position, 4);

            bool overSizeX = lineX > size.x;
            bool overSizeY = lineY > size.y;
            Vector2 upOffset = (Vector2.up * ((lineY / 2) + (size.y / 4 - lineY / 4))).Rotate(rotation);
            Vector2 rightOffset = (Vector2.right * ((lineX / 2) + (size.x / 4 - lineX / 4))).Rotate(rotation);
            Vector2 verticalSize = new Vector2(Mathf.Min(lineX, size.x), size.y / 2 - lineY / 2);
            Vector2 horizontalSize = new Vector2(size.x / 2 - lineX / 2, Mathf.Min(lineY, size.y));
            _PartialOpenBox(upOffset, verticalSize, rotation, !overSizeY, overSizeY, true, true);
            _PartialOpenBox(-upOffset, verticalSize, rotation, overSizeY, !overSizeY, true, true);
            _PartialOpenBox(rightOffset, horizontalSize, rotation, true, true, overSizeX, !overSizeX);
            _PartialOpenBox(-rightOffset, horizontalSize, rotation, true, true, !overSizeX, overSizeX);

            return 1;
        }

        /// <summary>
        /// Draws a solid plus shape at 'position' with 'size' rotated by 'rotation' and with bar width of 'barWidth'
        /// </summary>
        public static ShapeModifier DrawSolidPlus(Vector3 position, Vector2 size, float barWidth, float rotation, bool useLineWidthAsMultiplier = false)
            => _NewShapeModifierCount(BuildSolidPlus(position, size.Abs(), Mathf.Abs(barWidth), rotation, useLineWidthAsMultiplier));
        public static int BuildSolidPlus(Vector3 position, Vector2 size, float lineWidth, float rotation, bool useLineWidthAsMultiplier = false)
        {
            float lineX = useLineWidthAsMultiplier ? size.x * lineWidth : lineWidth;
            float lineY = useLineWidthAsMultiplier ? size.y * lineWidth : lineWidth;

            if (lineWidth == 0)
                return BuildPlusZeroWidth(position, size, rotation);

            _NewShape(position, 5);

            Vector2 upOffset = (Vector2.up * ((lineY / 2) + (size.y / 4 - lineY / 4))).Rotate(rotation);
            Vector2 rightOffset = (Vector2.right * ((lineX / 2) + (size.x / 4 - lineX / 4))).Rotate(rotation);
            Vector2 verticalSize = new Vector2(Mathf.Min(lineX, size.x), size.y / 2 - lineY / 2);
            Vector2 horizontalSize = new Vector2(size.x / 2 - lineX / 2, Mathf.Min(lineY, size.y));
            Vector2 centerSize = new Vector2(Mathf.Min(lineX, size.x), Mathf.Min(lineY, size.y));

            _SolidBox(Vector2.zero, centerSize, rotation);
            _SolidBox(upOffset, verticalSize, rotation);
            _SolidBox(-upOffset, verticalSize, rotation);
            _SolidBox(rightOffset, horizontalSize, rotation);
            _SolidBox(-rightOffset, horizontalSize, rotation);

            return 1;
        }

        /// <summary>
        /// Draws multiple open boxes at 'positions' with 'size' and 'rotation'
        /// </summary>
        /// <param name="colors">optional list of colors to cycle through</param>
        [Obsolete("This method may soon be deprecated and unavailable for use.", false)]
        public static ShapeModifier DrawOpenBoxes(List<Vector2> positions, Vector2 size, List<Color> colors = null) => DrawBoxes2DPlane(positions, size, false, colors);
        /// <summary>
        /// Draws multiple solid boxes at 'positions' with 'size' and 'rotation'
        /// </summary>
        /// <param name="colors">optional list of colors to cycle through</param>
        [Obsolete("This method may soon be deprecated and unavailable for use.", false)]
        public static ShapeModifier DrawSolidBoxes(List<Vector2> positions, Vector2 size, List<Color> colors = null) => DrawBoxes2DPlane(positions, size, true, colors);
        private static ShapeModifier DrawBoxes2DPlane(List<Vector2> positions, Vector2 size, bool solid, List<Color> colors = null)
            => _NewShapeModifierCount(BuildBoxes2DPlane(positions, size, solid, colors));
        private static int BuildBoxes2DPlane(List<Vector2> positions, Vector2 size, bool solid, List<Color> colors = null)
        {
            bool noOverrideColor = colors == null || colors.Count == 0;
            _NewShape(Vector2.zero, positions.Count);
            for (int i = 0; i < positions.Count; i++)
            {
                if (solid)
                    _SolidBox(positions[i], size, 0, noOverrideColor ? null : colors[i % colors.Count]);
                else
                    _OpenBox(positions[i], size, 0, noOverrideColor ? null : colors[i % colors.Count]);
            }

            return 1;
        }

        /// <summary>
        /// Draws multiple open boxes at 'positions' with 'size' and 'rotation'
        /// </summary>
        /// <param name="colors">optional list of colors to cycle through</param>
        [Obsolete("This method may soon be deprecated and unavailable for use.", false)]
        public static ShapeModifier DrawOpenBoxes(List<Vector3> positions, Vector2 size, List<Color> colors = null) => DrawBoxes3DSpace(positions, size, false, colors);
        /// <summary>
        /// Draws multiple solid boxes at 'positions' with 'size' and 'rotation'
        /// </summary>
        /// <param name="colors">optional list of colors to cycle through</param>
        [Obsolete("This method may soon be deprecated and unavailable for use.", false)]
        public static ShapeModifier DrawSolidBoxes(List<Vector3> positions, Vector2 size, List<Color> colors = null) => DrawBoxes3DSpace(positions, size, true, colors);
        private static ShapeModifier DrawBoxes3DSpace(List<Vector3> positions, Vector2 size, bool solid, List<Color> colors = null)
            => _NewShapeModifierCount(BuildBoxes3DSpace(positions, size, solid, colors));
        private static int BuildBoxes3DSpace(List<Vector3> positions, Vector2 size, bool solid, List<Color> colors = null)
        {
            bool noOverrideColor = colors == null || colors.Count == 0;
            for (int i = 0; i < positions.Count; i++)
            {
                _NewShape(positions[i], 1);

                if (solid)
                    _SolidBox(Vector3.zero, size, 0, noOverrideColor ? null : colors[i % colors.Count]);
                else
                    _OpenBox(Vector3.zero, size, 0, noOverrideColor ? null : colors[i % colors.Count]);
            }

            return positions.Count;
        }

        /// <summary>
        /// Draws a grid of open boxes with dimentions 'columns' x 'rows', size 'size', and centered at 'center'
        /// </summary>
        /// <param name="colors">optional grid of colors to cycle through</param>
        public static ShapeModifier DrawOpenBoxGrid(Vector3 center, Vector2 size, int columns, int rows, Color[,] colors = null)
            => _NewShapeModifierCount(BuildBoxGrid(center, size, columns, rows, false, colors));
        /// <summary>
        /// Draws a grid of solid boxes with dimentions 'columns' x 'rows', size 'size', and centered at 'center'
        /// </summary>
        /// <param name="colors">optional grid of colors to cycle through</param>
        public static ShapeModifier DrawSolidBoxGrid(Vector3 center, Vector2 size, int columns, int rows, Color[,] colors = null)
            => _NewShapeModifierCount(BuildBoxGrid(center, size, columns, rows, true, colors));
        private static int BuildBoxGrid(Vector3 center, Vector2 size, int columns, int rows, bool solid, Color[,] colors = null)
        {
            if (columns <= 0 || rows <= 0)
                return 0;

            Vector2 arrayDimensions = new Vector2(columns, rows);
            int count = columns * rows;
            float width = size.x;
            float height = size.y;

            float cellWidth = width / arrayDimensions.x;
            float cellHeight = height / arrayDimensions.y;
            Vector2 cellSize = new Vector2(cellWidth, cellHeight);
            Vector2 origin = -new Vector2(width / 2, height / 2) + new Vector2(cellWidth / 2, cellHeight / 2);

            _NewShape(center, count);
            bool noOverrideColor = colors == null || colors.Length == 0;
            int colorsLen0 = noOverrideColor ? 0 : colors.GetLength(0);
            int colorsLen1 = noOverrideColor ? 0 : colors.GetLength(1);
            for (int i = 0; i < arrayDimensions.x; i++)
            {
                for (int j = 0; j < arrayDimensions.y; j++)
                {
                    if (solid)
                        _SolidBox(origin + new Vector2(i * cellWidth, j * cellHeight), cellSize, 0, noOverrideColor ? null : colors[i % colorsLen0, j % colorsLen1]);
                    else
                        _OpenBox(origin + new Vector2(i * cellWidth, j * cellHeight), cellSize, 0, noOverrideColor ? null : colors[i % colorsLen0, j % colorsLen1]);
                }
            }

            return 1;
        }

        private static (Vector2, float, BorderType, bool, bool) AdjustWeightedBoxParams(Vector2 size, float borderWidth, BorderType borderType)
        {
            int fillBox = 0; // 0 - no, 1 - yes, 2 - only
            size = size.Abs();
            bool outsideFill = false;
            bool insideFill = false;

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
                        outsideFill = fillBox < 2;
                        insideFill = fillBox > 0;
                        return (size, borderWidth, borderType, insideFill, outsideFill);
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

            outsideFill = fillBox < 2;
            insideFill = fillBox > 0;
            return (size, borderWidth, borderType, insideFill, outsideFill);
        }

        /// <summary>
        /// Draws a box by specifying parameters
        /// </summary>
        public static ShapeModifier DrawCustomBox(Vector3 position, Vector2 size, BoxParams boxParams)
            => _NewShapeModifierCount(BuildCustomBox(position, size.Abs(), boxParams));
        private static int BuildCustomBox(Vector3 position, Vector2 size, BoxParams boxParams)
        {
            if (boxParams.borderWidth == 0)
            {
                if (boxParams.solid)
                    return BuildSolidBox(position, size, boxParams.rotation);
                else
                    return BuildOpenBox(position, size, boxParams.rotation);
            }

            (Vector2 newSize, float newBorderWidth, BorderType newBorderType, bool insideFill, bool outsideFill) = AdjustWeightedBoxParams(size, boxParams.borderWidth, boxParams.borderType);


            float edgeRadius = Mathf.Lerp(0, newBorderWidth, Mathf.Clamp01(boxParams.roundCorners01));
            if (boxParams.borderType == BorderType.Centered)
                edgeRadius = Mathf.Lerp(0, Mathf.Abs(boxParams.borderWidth / 2), Mathf.Clamp01(boxParams.roundCorners01));
            float adjBorderWidth = newBorderWidth - edgeRadius;

            int shapesCount = 0;

            if (boxParams.solidBorder)
            {
                if (outsideFill)
                {
                    if (adjBorderWidth > 0)
                        shapesCount += BuildWeightedBox(position, newSize, adjBorderWidth, BorderType.Outside, boxParams.rotation);

                    if (edgeRadius > 0)
                        shapesCount += BuildSolidBoxEdgeRadius(position, newSize + Vector2.one * adjBorderWidth * 2, edgeRadius, boxParams.rotation, false);
                }
                if (insideFill)
                {
                    if (outsideFill || edgeRadius == 0)
                        shapesCount += BuildSolidBox(position, newSize, boxParams.rotation);
                    else
                    {
                        float minLength = Mathf.Min(newSize.x, newSize.y);
                        edgeRadius = Mathf.Abs(Mathf.Lerp(0, minLength / 2, Mathf.Clamp01(boxParams.roundCorners01)));
                        Vector2 adjSize = newSize - Vector2.one * edgeRadius * 2;
                        shapesCount += BuildSolidBoxEdgeRadius(position, adjSize, edgeRadius, boxParams.rotation, true);
                    }
                }
            }
            else
            {
                if (outsideFill)
                {
                    if (edgeRadius == 0)
                        shapesCount += BuildOpenBox(position, newSize + Vector2.one * adjBorderWidth * 2, boxParams.rotation);
                    else
                        shapesCount += BuildOpenBoxEdgeRadius(position, newSize + Vector2.one * adjBorderWidth * 2, edgeRadius, boxParams.rotation, false);

                    bool centeredOverflow = boxParams.borderType == BorderType.Centered && Mathf.Abs(boxParams.borderWidth) >= Mathf.Min(size.x, size.y);
                    if (!boxParams.solid && !boxParams.hideBox && !centeredOverflow)
                        shapesCount += BuildOpenBox(position, newSize, boxParams.rotation);
                }
                else if (insideFill)
                {
                    if (edgeRadius == 0)
                        shapesCount += BuildOpenBox(position, newSize, boxParams.rotation);
                    else
                    {
                        float minLength = Mathf.Min(newSize.x, newSize.y);
                        edgeRadius = Mathf.Abs(Mathf.Lerp(0, minLength / 2, Mathf.Clamp01(boxParams.roundCorners01)));
                        Vector2 adjSize = newSize - Vector2.one * edgeRadius * 2;
                        shapesCount += BuildOpenBoxEdgeRadius(position, adjSize, edgeRadius, boxParams.rotation, false);
                    }
                }
            }

            if (!boxParams.hideBox && !insideFill && boxParams.solid)
                shapesCount += BuildSolidBox(position, newSize, boxParams.rotation);

            return shapesCount;
        }
        #endregion

        #region ### Circles
        /// <summary>
        /// Returns the default number of edges for an arc with 'radius' and 'arcAngle' (in degrees)
        /// </summary>
        public static int GetDefaultNumEdges(float radius, float arcAngle) => (int)(12 * Mathf.Sqrt(radius * 2) / (360f / Mathf.Abs(arcAngle)));
        private static int GetNumEdges(float radius, float arcAngle, int numEdges)
        {
            int defaultMult = numEdges >= 0 ? 1 : Mathf.Abs(numEdges);
            if (numEdges <= 0)
                numEdges = GetDefaultNumEdges(radius, arcAngle) * defaultMult;

            return numEdges;
        }

        /// <summary>
        /// Returns the positions of each vertex of a circle with 'numEdges'
        /// </summary>
        public static Vector2[] GetCircleVertices(float radius, int numEdges = 0)
            => InternalGetCircleVertices(radius, 360, 0, numEdges);

        /// <summary>
        /// Returns the positions of each vertex of an arc with 'numEdges'
        /// </summary>
        public static Vector2[] GetArcVertices(float radius, float arcAngle, float offsetAngle, int numEdges = 0)
            => InternalGetCircleVertices(radius, arcAngle, offsetAngle, numEdges);

        private static Vector2[] InternalGetCircleVertices(float radius, float arcAngle, float offsetAngle, int numEdges)
        {
            radius = Mathf.Abs(radius);
            numEdges = GetNumEdges(radius, arcAngle, numEdges);

            Vector2[] vertices = new Vector2[numEdges + 1];

            for (int i = 0; i <= numEdges; i++)
                vertices[i] = Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * radius;

            return vertices;
        }

        /// <summary>
        /// Draws an open circle at 'position' with 'radius' by drawing a polygon with 'numEdges' (automatically calculated if 0)
        /// </summary>
        public static ShapeModifier DrawOpenCircle(Vector3 position, float radius, int numEdges = 0)
            => _NewShapeModifierCount(BuildOpenCircle(position, radius, numEdges));
        public static int BuildOpenCircle(Vector3 position, float radius, int numEdges = 0)
        {
            if (radius == 0)
                return 0;

            _NewShape(position, 1);
            _OpenCircle(Vector3.zero, radius, 360, 0, numEdges);
            return 1;
        }
        /// <summary>
        /// Approximates a solid circle at 'position' with 'radius' by drawing a polygon with 'numEdges' (automatically calculated if 0)
        /// </summary>
        public static ShapeModifier DrawSolidCircle(Vector3 position, float radius, int numEdges = 0)
            => _NewShapeModifierCount(BuildSolidCircle(position, radius, numEdges));
        public static int BuildSolidCircle(Vector3 position, float radius, int numEdges = 0)
        {
            if (radius == 0)
                return 0;

            _NewShape(position, 1);
            _SolidCircle(Vector3.zero, radius, 360, 0, numEdges);
            return 1;
        }
        /// <summary>
        /// Draws an open arc with 'angle' at 'position' with 'radius' by drawing a partial polygon with 'numEdges' (automatically calculated if 0)
        /// </summary>
        public static ShapeModifier DrawOpenArc(Vector3 position, float radius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType = ArcCloseType.Center, int numEdges = 0)
            => _NewShapeModifierCount(BuildOpenArc(position, radius, arcAngle, offsetAngle, arcCloseType, numEdges));
        public static int BuildOpenArc(Vector3 position, float radius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType = ArcCloseType.Center, int numEdges = 0)
        {
            if (arcAngle == 0 || radius == 0)
                return 0;

            _NewShape(position, arcCloseType == ArcCloseType.None ? 1 : 2);
            _OpenCircle(Vector3.zero, radius, arcAngle, offsetAngle, numEdges);

            switch (arcCloseType)
            {
                case ArcCloseType.Flat:
                    _LineStrip(stackalloc Vector3[]
                    {
                        Vector2.right.Rotate(offsetAngle) * radius,
                        Vector2.right.Rotate(offsetAngle + arcAngle) * radius
                    }, false);
                    break;
                case ArcCloseType.Center:
                    _LineStrip(stackalloc Vector3[]
                    {
                        Vector2.right.Rotate(offsetAngle) * radius,
                        Vector2.zero,
                        Vector2.right.Rotate(offsetAngle + arcAngle) * radius
                    }, false);
                    break;
                case ArcCloseType.Edge:
                    _LineStrip(stackalloc Vector3[]
                    {
                        Vector2.right.Rotate(offsetAngle) * radius,
                        EdgeMinMaxPoint(Vector2.zero, -Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * Min_Max_Bias, Vector2.right.Rotate(offsetAngle) * radius, Vector2.right.Rotate(arcAngle + offsetAngle) * radius, arcAngle),
                        Vector2.right.Rotate(offsetAngle + arcAngle) * radius
                    }, false);
                    break;

            }

            return 1;
        }
        /// <summary>
        /// Draws a solid arc with 'angle' at 'position' with 'radius' by drawing a partial polygon with 'numEdges' (automatically calculated if 0)
        /// </summary>
        public static ShapeModifier DrawSolidArc(Vector3 position, float radius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType = ArcCloseType.Center, int numEdges = 0)
            => _NewShapeModifierCount(BuildSolidArc(position, radius, arcAngle, offsetAngle, arcCloseType, numEdges));
        public static int BuildSolidArc(Vector3 position, float radius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType = ArcCloseType.Center, int numEdges = 0)
        {
            if (arcAngle == 0 || radius == 0)
                return 0;

            _NewShape(position, 1);
            _SolidArc(Vector3.zero, radius, arcAngle, offsetAngle, arcCloseType, numEdges);

            return 1;
        }

        /// <summary>
        /// Draws a weighted circle by filling the area between 2 circles with 'innerRadius' and 'outerRadius'
        /// </summary>
        public static ShapeModifier DrawWeightedCircle(Vector3 position, float innerRadius, float outerRadius, int numEdges = 0)
            => _NewShapeModifierCount(BuildWeightedCircle(position, innerRadius, outerRadius, 360, 0, ArcCloseType.None, numEdges));

        /// <summary>
        /// Draws a weighted arc by filling the area between 2 arcs with 'innerRadius' and 'outerRadius'. Arc is closed if ends are connected by arcCloseType
        /// </summary>
        public static ShapeModifier DrawWeightedArc(Vector3 position, float innerRadius, float outerRadius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType = ArcCloseType.None, int numEdges = 0)
            => _NewShapeModifierCount(BuildWeightedCircle(position, innerRadius, outerRadius, arcAngle, offsetAngle, arcCloseType, numEdges));

        /// <summary>
        /// Draws a weighted circle with 'radius' and an edge thickness of 'borderWidth'
        /// </summary>
        public static ShapeModifier DrawWeightedCircle(Vector3 position, float radius, float borderWidth, BorderType borderType, int numEdges = 0)
            => _NewShapeModifierCount(BuildWeightedCircle(position, radius, 360, 0, borderWidth, borderType, ArcCloseType.None, numEdges));

        /// <summary>
        /// Draws a weighted arc with 'radius' and an edge thickness of 'borderWidth'. Arc is closed if ends are connected by arcCloseType
        /// </summary>
        public static ShapeModifier DrawWeightedArc(Vector3 position, float radius, float arcAngle, float offsetAngle, float borderWidth, BorderType borderType, ArcCloseType arcCloseType = ArcCloseType.None, int numEdges = 0)
         => _NewShapeModifierCount(BuildWeightedCircle(position, radius, arcAngle, offsetAngle, borderWidth, arcCloseType, numEdges));

        /// <summary>
        /// Draws a weighted circle bounded by 'point1' and 'point2' with an edge thickness of 'borderWidth'
        /// </summary>
        public static ShapeModifier DrawWeightedCircle(Vector2 point1, Vector2 point2, float borderWidth, BorderType borderType, int numEdges = 0)
        {
            float radius = Vector2.Distance(point1, point2) / 2;
            Vector2 position = (point1 + point2) / 2;

            return _NewShapeModifierCount(BuildWeightedCircle(position, radius, 360, 0, borderWidth, borderType, ArcCloseType.None, numEdges));
        }
        /// <summary>
        /// Draws a weighted arc bounded by 'point1' and 'point2' with an edge thickness of 'borderWidth'
        /// </summary>
        public static ShapeModifier DrawWeightedArc(Vector2 point1, Vector2 point2, float arcAngle, float offsetAngle, float borderWidth, BorderType borderType, ArcCloseType arcCloseType = ArcCloseType.None, int numEdges = 0)
        {
            float radius = Vector2.Distance(point1, point2) / 2;
            Vector2 position = (point1 + point2) / 2;

            return _NewShapeModifierCount(BuildWeightedCircle(position, radius, arcAngle, offsetAngle, borderWidth, borderType, arcCloseType, numEdges));
        }
        private static int BuildWeightedCircle(Vector3 position, float radius, float arcAngle, float offsetAngle, float borderWidth, BorderType borderType, ArcCloseType arcCloseType = ArcCloseType.None, int numEdges = 0, float roundCenter01 = 1)
        {
            radius = Mathf.Abs(radius);
            (borderWidth, borderType) = AdjustForNegativeBorderWidth(borderWidth, borderType);

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

            return BuildWeightedCircle(position, innerRadius, outerRadius, arcAngle, offsetAngle, arcCloseType, numEdges, roundCenter01);
        }
        private static int BuildWeightedCircle(Vector3 position, float innerRadius, float outerRadius, float arcAngle, float offsetAngle, ArcCloseType arcCloseType = ArcCloseType.None, int numEdges = 0, float roundCenter01 = 1)
        {
            if (arcAngle == 0)
                return 0;

            if (innerRadius == outerRadius)
            {
                return BuildOpenArc(position, innerRadius, arcAngle, offsetAngle, arcCloseType, numEdges);
            }

            roundCenter01 = Mathf.Clamp01(roundCenter01);

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

            numEdges = GetNumEdges(outerRadius, arcAngle, numEdges);

            float borderWidth = outerRadius - innerRadius;

            int numShapes = 0;
            // fill arc if too thick
            if (arcAngleAbs < 360)
            {
                Vector2 innerStart = Vector2.right.Rotate(offsetAngle) * innerRadius;
                Vector2 innerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                Vector2 outerStart = Vector2.right.Rotate(offsetAngle) * outerRadius;
                Vector2 outerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;

                bool fillCompletely = false;
                switch (arcCloseType)
                {
                    case ArcCloseType.Flat:
                        float distance = outerRadius * 2;
                        if (arcAngleAbs >= 180)
                        {
                            distance = Vector2.Distance((outerStart + outerEnd) / 2, Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * outerRadius);

                            fillCompletely = borderWidth > (distance / 2);
                            if (fillCompletely)
                            {
                                return BuildSolidArc(position, outerRadius, arcAngle, offsetAngle, arcCloseType, numEdges);
                            }
                        }
                        else
                        {
                            distance = Vector2.Distance((innerStart + innerEnd) / 2, Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * outerRadius);
                            fillCompletely = borderWidth > (distance / 2);
                            if (fillCompletely)
                            {
                                _NewShape(position, 1);
                                _SolidArc(Vector3.zero, innerRadius, arcAngle, offsetAngle, arcCloseType, numEdges);
                                numShapes++;
                                arcCloseType = ArcCloseType.None;
                            }
                        }
                        break;
                    case ArcCloseType.Center:
                        if (innerRadius <= outerRadius / 2)
                        {
                            return BuildSolidArc(position, outerRadius, arcAngle, offsetAngle, arcCloseType, numEdges);
                        }
                        break;
                    case ArcCloseType.Edge:
                        Vector2 _outerStart = Vector2.right.Rotate(offsetAngle) * outerRadius;
                        Vector2 _outerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;
                        Vector2 _outerCorner = EdgeMinMaxPoint(Vector2.zero, -Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * Min_Max_Bias, Vector2.right.Rotate(offsetAngle) * outerRadius, Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius, arcAngle);
                        if (_outerCorner == _outerEnd || _outerCorner == _outerStart)
                        {
                            distance = outerRadius * 2;
                            if (arcAngleAbs >= 180)
                            {
                                distance = Vector2.Distance((outerStart + outerEnd) / 2, Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * outerRadius);

                                fillCompletely = borderWidth > (distance / 2);
                                if (fillCompletely)
                                {
                                    return BuildSolidArc(position, outerRadius, arcAngle, offsetAngle, arcCloseType, numEdges);
                                }
                            }
                            else
                            {
                                distance = Vector2.Distance((innerStart + innerEnd) / 2, Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * outerRadius);
                                fillCompletely = borderWidth > (distance / 2);
                                if (fillCompletely)
                                {
                                    _NewShape(position, 1);
                                    _SolidArc(Vector3.zero, innerRadius, arcAngle, offsetAngle, arcCloseType, numEdges);
                                    numShapes++;
                                    arcCloseType = ArcCloseType.None;
                                }
                            }
                        }
                        break;
                }
            }

            _NewShape(position, 1);
            numShapes++;
            // draw arc
            NativeList<Vector3> vertices = new(Allocator.Temp);
            for (int i = 1; i <= numEdges; i++)
            {
                Vector2 outer0 = Vector2.right.Rotate(arcAngle * ((float)(i - 1) / (float)numEdges) + offsetAngle) * outerRadius;
                Vector2 outer1 = Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * outerRadius;
                Vector2 inner0 = Vector2.right.Rotate(arcAngle * ((float)(i - 1) / (float)numEdges) + offsetAngle) * innerRadius;
                Vector2 inner1 = Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * innerRadius;

                vertices.Add(outer0);
                vertices.Add(outer1);
                vertices.Add(inner1);
                if (inner0 != inner1)
                    vertices.Add(inner0);
            }
            if (innerRadius != 0)
                _SolidQuads(vertices);
            else
                _Triangles(vertices, true);

            // if arc and needs connector - continue
            if (arcAngleAbs >= 360 || arcCloseType == ArcCloseType.None)
                return numShapes;


            NativeList<Vector3> closingVerticesWest = new(Allocator.Temp);
            NativeList<Vector3> closingVerticesEast = new(Allocator.Temp);

            bool GetNearestCircleEdge(Vector2 point, float circleRadius, out Vector2 p1, out Vector2 p2, out Vector2Int edgeIndex)
            {
                p1 = Vector2.zero;
                p2 = Vector2.zero;
                edgeIndex = new Vector2Int(-1, -1);

                int index1 = 0;
                int index2 = 1;
                Vector2 vertex0 = Vector2.right.Rotate(offsetAngle) * circleRadius;
                float anglePoint = Vector2.SignedAngle(vertex0, point).PositiveAngle();
                for (int i = 1; i <= numEdges; i++)
                {
                    Vector2 vertex1 = Vector2.right.Rotate(arcAngle * ((float)index1 / (float)numEdges) + offsetAngle) * circleRadius;
                    Vector2 vertex2 = Vector2.right.Rotate(arcAngle * ((float)index2 / (float)numEdges) + offsetAngle) * circleRadius;
                    float angle1 = Vector2.SignedAngle(vertex0, vertex1).PositiveAngle();
                    float angle2 = Vector2.SignedAngle(vertex0, vertex2).PositiveAngle();
                    if (signAngle == 1 ? (anglePoint > angle1 && anglePoint <= angle2) : (anglePoint <= angle1 && anglePoint > angle2))
                    {
                        p1 = vertex1;
                        p2 = vertex2;
                        edgeIndex = new(index1, index2);
                        return true;
                    }

                    index1++;
                    index2++;
                }

                return false;
            }

            (NativeList<Vector3>, NativeList<Vector3>) GetWestEastInnerVectorLists(Vector2 outerCorner, Vector2 innerCorner, float innerRadiusLimit, bool ignoreDrawInnerCorner = false)
            {
                NativeList<Vector3> westVertices = new(Allocator.Temp);
                NativeList<Vector3> eastVertices = new(Allocator.Temp);

                westVertices.Add(outerCorner);
                Vector2 westEdgeDirection = Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius - outerCorner;
                for (int i = numEdges; i >= numEdges / 2; i--)
                {
                    Vector2 testPosition = Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * innerRadius;
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
                            Vector2 pointA2 = Vector2.right.Rotate(arcAngle * ((float)(i + 1) / (float)numEdges) + offsetAngle) * innerRadius;
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

                Vector2 innerRadiusPoint = Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * innerRadiusLimit;
                bool innerCornerOutsideBounds = innerCorner.magnitude > innerRadiusLimit;

                if (innerCornerOutsideBounds)
                {
                    Vector2 lastPosition = westVertices[westVertices.Length - 1];

                    Vector2 rayDirection = (lastPosition - innerCorner).normalized;

                    bool hitCircle = Extensions.FindFirstRayCircleIntersection(innerCorner, rayDirection, Vector2.zero, innerRadiusLimit, out Vector2 circleIntersectionPoint);
                    if (!hitCircle)
                        hitCircle = Extensions.FindSegmentRayIntersection(innerCorner, lastPosition, Vector2.zero, westEdgeDirection.Rotate(-90 * signAngle), out circleIntersectionPoint);

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
                                float angleToInnerRadiusMidpoint = Vector2.SignedAngle(-innerCorner, Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle));
                                if (signAngle == 1 ? angleToInnerRadiusMidpoint < 0 : angleToInnerRadiusMidpoint > 0)
                                {
                                    Vector2 pos = Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * innerRadiusLimit;
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
                Vector2 eastEdgeDirection = Vector2.right.Rotate(offsetAngle) * innerRadius - outerCorner;
                for (int i = 0; i <= numEdges / 2; i++)
                {
                    Vector2 testPosition = Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * innerRadius;
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
                            Vector2 pointA2 = Vector2.right.Rotate(arcAngle * ((float)(i - 1) / (float)numEdges) + offsetAngle) * innerRadius;
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
                    Vector2 lastPosition = eastVertices[eastVertices.Length - 1];

                    Vector2 rayDirection = (lastPosition - innerCorner).normalized;

                    bool hitCircle = Extensions.FindFirstRayCircleIntersection(innerCorner, rayDirection, Vector2.zero, innerRadiusLimit, out Vector2 circleIntersectionPoint);
                    if (!hitCircle)
                        hitCircle = Extensions.FindSegmentRayIntersection(innerCorner, lastPosition, Vector2.zero, eastEdgeDirection.Rotate(90 * signAngle), out circleIntersectionPoint);

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
                                float angleToInnerRadiusMidpoint = Vector2.SignedAngle(-innerCorner, Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle));
                                if (signAngle == 1 ? angleToInnerRadiusMidpoint > 0 : angleToInnerRadiusMidpoint < 0)
                                {
                                    Vector2 pos = Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle) * innerRadiusLimit;
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
                        Vector2 innerStart = Vector2.right.Rotate(offsetAngle) * innerRadius;
                        Vector2 innerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                        Vector2 outerCorner = (innerStart + innerEnd) / 2;
                        Vector2 innerCorner = outerCorner + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * borderWidth;
                        (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(outerCorner, innerCorner, innerRadius);
                    }
                    else
                    {
                        Vector2 innerStart = Vector2.right.Rotate(offsetAngle) * innerRadius;
                        Vector2 innerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                        Vector2 outerStart = Vector2.right.Rotate(offsetAngle) * outerRadius;
                        Vector2 outerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;
                        Vector2 outerCorner = (outerStart + outerEnd) / 2;
                        Vector2 innerCorner = outerCorner + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * borderWidth;
                        Vector2 midCorner = (innerStart + innerEnd) / 2;
                        (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(midCorner, innerCorner, innerRadius);

                        _NewShape(position, 1);
                        _SolidQuad(innerStart, innerEnd, outerEnd, outerStart);
                        numShapes++;
                    }
                    break;
                case ArcCloseType.Center:
                    Vector2 innerEdgeCenterPosition = Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * borderWidth;
                    Vector2 innerCenerDirection = Vector2.right.Rotate(arcAngle / 2 + offsetAngle);
                    Vector2 innerRadiusPoint = innerCenerDirection * innerRadius;
                    Vector2 westEdgeDirection = Vector2.right.Rotate(arcAngle + offsetAngle);
                    Vector2 westEdgeDirectionNormal = westEdgeDirection.Rotate(-90 * signAngle);
                    Vector2 orthogonalPoint = (Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius) + westEdgeDirectionNormal * borderWidth;
                    if (Extensions.FindSegmentIntersection(Vector2.zero, innerCenerDirection * outerRadius, orthogonalPoint, orthogonalPoint + -westEdgeDirection * outerRadius, out Vector2 intersectionPoint, false))
                    {
                        (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(Vector2.zero, intersectionPoint, Mathf.Lerp(innerRadius, borderWidth, roundCenter01), true);
                    }
                    else
                    {
                        (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(Vector2.zero, innerEdgeCenterPosition, Mathf.Lerp(innerRadius, borderWidth, roundCenter01), true);
                    }
                    break;
                case ArcCloseType.Edge:

                    Vector2 _outerStart = Vector2.right.Rotate(offsetAngle) * outerRadius;
                    Vector2 _outerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;
                    Vector2 _outerCorner = EdgeMinMaxPoint(Vector2.zero, -Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * Min_Max_Bias, Vector2.right.Rotate(offsetAngle) * outerRadius, Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius, arcAngle);

                    // if flat
                    if (_outerCorner == _outerEnd || _outerCorner == _outerStart)
                    {
                        if (arcAngleAbs <= 180)
                        {
                            Vector2 innerStart = Vector2.right.Rotate(offsetAngle) * innerRadius;
                            Vector2 innerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                            Vector2 outerCorner = (innerStart + innerEnd) / 2;
                            Vector2 innerCorner = outerCorner + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * borderWidth;
                            (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(outerCorner, innerCorner, innerRadius);
                        }
                        else
                        {
                            Vector2 innerStart = Vector2.right.Rotate(offsetAngle) * innerRadius;
                            Vector2 innerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                            Vector2 outerStart = Vector2.right.Rotate(offsetAngle) * outerRadius;
                            Vector2 outerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;
                            Vector2 outerCorner = (outerStart + outerEnd) / 2;
                            Vector2 innerCorner = outerCorner + Vector2.right.Rotate(arcAngle / 2 + offsetAngle) * borderWidth;
                            Vector2 midCorner = (innerStart + innerEnd) / 2;
                            (closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(midCorner, innerCorner, innerRadius);

                            _NewShape(position, 1);
                            _SolidQuad(innerStart, innerEnd, outerEnd, outerStart);
                            numShapes++;
                        }
                    }
                    else
                    {
                        Vector2 innerDirection = Vector2.right.Rotate(arcAngle / 2 + offsetAngle);
                        Vector2 innerStart = Vector2.right.Rotate(offsetAngle) * innerRadius;
                        Vector2 innerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius;
                        Vector2 outerStart = Vector2.right.Rotate(offsetAngle) * outerRadius;
                        Vector2 outerEnd = Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius;
                        Vector2 outerCorner = EdgeMinMaxPoint(Vector2.zero, -innerDirection * Min_Max_Bias, Vector2.right.Rotate(offsetAngle) * outerRadius, Vector2.right.Rotate(arcAngle + offsetAngle) * outerRadius, arcAngle);
                        Vector2 midCorner = EdgeMinMaxPoint(Vector2.zero, -innerDirection * Min_Max_Bias, Vector2.right.Rotate(offsetAngle) * innerRadius, Vector2.right.Rotate(arcAngle + offsetAngle) * innerRadius, arcAngle);
                        Vector2 innerCorner = _outerCorner + new Vector2(Mathf.Sign(innerDirection.x), Mathf.Sign(innerDirection.y)) * borderWidth;

                        if (innerCorner.magnitude > innerRadius)
                        {
                            Vector2 xDir = new Vector2(Mathf.Sign(innerDirection.x), 0);
                            Vector2 yDir = new Vector2(0, Mathf.Sign(innerDirection.y));
                            Vector2 pointX = -xDir * innerRadius;
                            Vector2 pointY = -yDir * innerRadius;

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

                        // i don't remember what this does
                        //(closingVerticesWest, closingVerticesEast) = GetWestEastInnerVectorLists(midCorner, innerCorner);

                        _NewShape(position, 2);
                        _SolidQuad(innerStart, outerStart, outerCorner, midCorner);
                        _SolidQuad(innerEnd, outerEnd, outerCorner, midCorner);
                        numShapes++;
                    }

                    break;

            }

            _NewShape(position, 2);
            _SolidPolygon(closingVerticesWest.AsArray().AsSpan());
            _SolidPolygon(closingVerticesEast.AsArray().AsSpan());
            numShapes++;

            closingVerticesWest.Dispose();
            closingVerticesEast.Dispose();

            return numShapes;
        }

        /// <summary>
        /// Draws an open circle at 'position' with 'radius' by drawing a polygon with 'numEdges' * 2, drawing every other edge for a dashed effect
        /// </summary>
        public static ShapeModifier DrawDashedCircle(Vector3 position, float radius, int numEdges = 0)
            => _NewShapeModifierCount(BuildDashedCircle(position, radius, 360, 0, numEdges));

        /// <summary>
        /// Draws an open arc at 'position' with 'radius' by drawing a partial polygon with 'numEdges' * 2, drawing every other edge for a dashed effect
        /// </summary>
        public static ShapeModifier DrawDashedArc(Vector3 position, float radius, float arcAngle, float offsetAngle, int numEdges = 0)
            => _NewShapeModifierCount(BuildDashedCircle(position, radius, arcAngle, offsetAngle, numEdges));

        private static int BuildDashedCircle(Vector3 position, float radius, float arcAngle, float offsetAngle, int numEdges = 0)
        {
            if (radius == 0)
                return 0;

            if (numEdges > 0)
                numEdges *= 2;

            _NewShape(position, 1);
            _OpenCircle(Vector2.zero, radius, arcAngle, offsetAngle, numEdges, true);
            return 1;
        }

        /// <summary>
        /// Draws a weighted dashed circle at 'position' with 'radius' and 'borderWidth' by drawing a weighted polygon with 'numEdges' * 2, drawing every other edge for a dashed effect
        /// </summary>
        public static ShapeModifier DrawWeightedDashedCircle(Vector3 position, float radius, float borderWidth, BorderType borderType, int numEdges = 0)
            => DrawWeightedDashedArc(position, radius, 360, 0, borderWidth, borderType, numEdges);

        /// <summary>
        /// Draws a weighted dashed arc at 'position' with 'radius' and 'borderWidth' by drawing a weighted partial polygon with 'numEdges' * 2, drawing every other edge for a dashed effect
        /// </summary>
        public static ShapeModifier DrawWeightedDashedArc(Vector3 position, float radius, float arcAngle, float offsetAngle, float borderWidth, BorderType borderType, int numEdges = 0)
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

            return _NewShapeModifierCount(BuildWeightedDashedCircle(position, innerRadius, outerRadius, arcAngle, offsetAngle, numEdges));
        }

        public static ShapeModifier DrawWeightedDashedCircle(Vector3 position, float innerRadius, float outerRadius, int numEdges = 0)
            => _NewShapeModifierCount(BuildWeightedDashedCircle(position, innerRadius, outerRadius, 360, 0, numEdges));

        /// <summary>
        /// Draws an open arc at 'position' with 'radius' by drawing a polygon with 'numEdges', drawing every other edges for a dashed effect
        /// </summary>
        public static ShapeModifier DrawWeightedDashedArc(Vector3 position, float innerRadius, float outerRadius, float arcAngle, float offsetAngle, int numEdges = 0)
            => _NewShapeModifierCount(BuildWeightedDashedCircle(position, innerRadius, outerRadius, arcAngle, offsetAngle, numEdges));
        private static int BuildWeightedDashedCircle(Vector3 position, float innerRadius, float outerRadius, float arcAngle, float offsetAngle, int numEdges = 0)
        {
            if (arcAngle == 0)
                return 0;

            if (innerRadius == outerRadius)
            {
                return BuildDashedCircle(position, innerRadius, arcAngle, offsetAngle, numEdges);
            }

            innerRadius = Mathf.Abs(innerRadius);
            outerRadius = Mathf.Abs(outerRadius);
            if (innerRadius > outerRadius)
            {
                float temp = outerRadius;
                outerRadius = innerRadius;
                innerRadius = temp;
            }

            if (numEdges > 0)
                numEdges *= 2;

            numEdges = GetNumEdges(outerRadius, arcAngle, numEdges);

            if (numEdges % 2 != 0 && Mathf.Abs(arcAngle) >= 360)
                numEdges++;
            else if (numEdges % 2 == 0 && Mathf.Abs(arcAngle) < 360)
                numEdges--;

            int numVertices = numEdges + 1;
            // vertices
            Span<Vector2> vertexAngles = stackalloc Vector2[numVertices];
            for (int i = 0; i <= numEdges; i++)
            {
                vertexAngles[i] = Vector2.right.Rotate(arcAngle * ((float)i / (float)numEdges) + offsetAngle);
            }

            _NewShape(position, numVertices / 2);
            for (int i = 0; i < numVertices - 1; i += 2)
            {
                _SolidQuad(vertexAngles[i] * outerRadius, vertexAngles[i + 1] * outerRadius, vertexAngles[i + 1] * innerRadius, vertexAngles[i] * innerRadius);
            }

            return 1;
        }

        /// <summary>
        /// Draws a circle by specifying parameters
        /// </summary>
        public static ShapeModifier DrawCustomCircle(Vector3 position, float radius, CircleParams circleParams)
            => _NewShapeModifierCount(BuildCustomCircle(position, radius, circleParams));
        private static int BuildCustomCircle(Vector3 position, float radius, CircleParams circleParams)
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

            if (!circleParams.dashed)
                return BuildWeightedCircle(position, circleParams.solid ? 0 : innerRadius, outerRadius, circleParams.arcAngle, circleParams.rotation, circleParams.arcCloseType, circleParams.numEdges, circleParams.roundCenter ? 1 : 0);
            else
                return BuildWeightedDashedCircle(position, circleParams.solid ? 0 : innerRadius, outerRadius, circleParams.arcAngle, circleParams.rotation, circleParams.numEdges);
        }

        private static Vector2 EdgeMinMaxPoint(Vector2 center, Vector2 offset, Vector2 point1, Vector2 point2, float angle)
        {
            Vector2 minXminY = new Vector2(Mathf.Min(point1.x, point2.x), Mathf.Min(point1.y, point2.y));
            Vector2 minXmaxY = new Vector2(Mathf.Min(point1.x, point2.x), Mathf.Max(point1.y, point2.y));
            Vector2 maxXminY = new Vector2(Mathf.Max(point1.x, point2.x), Mathf.Min(point1.y, point2.y));
            Vector2 maxXmaxY = new Vector2(Mathf.Max(point1.x, point2.x), Mathf.Max(point1.y, point2.y));
            Span<Vector2> vectors = stackalloc Vector2[4] { minXminY, minXmaxY, maxXminY, maxXmaxY };

            float rev = (Mathf.Abs(angle) % 360) <= 180 ? 1 : -1;

            float distanceMinMin = Vector2.Distance(minXminY, center + offset * rev);
            float distanceMinMax = Vector2.Distance(minXmaxY, center + offset * rev);
            float distanceMaxMin = Vector2.Distance(maxXminY, center + offset * rev);
            float distanceMaxMax = Vector2.Distance(maxXmaxY, center + offset * rev);
            Span<float> distances = stackalloc float[4] { distanceMinMin, distanceMinMax, distanceMaxMin, distanceMaxMax };

            float minDistance = Mathf.Min(Mathf.Min(Mathf.Min(distanceMinMin, distanceMinMax), distanceMaxMin), distanceMaxMax);
            float maxDistance = Mathf.Max(Mathf.Max(Mathf.Max(distanceMinMin, distanceMinMax), distanceMaxMin), distanceMaxMax);
            int index = distances.IndexOf((Mathf.Abs(angle) % 360) <= 180 ? minDistance : maxDistance);

            return vectors[index];
        }

        private static Vector2 GetPointOnUnitCircle(float radius, float angle) => Vector2.right.Rotate(angle) * radius;
        #endregion

        #region ### Lines and Polygons
        /// <summary>
        /// Draws a line starting at 'from' ending at 'to'
        /// </summary>
        public static ShapeModifier DrawLine(Vector3 from, Vector3 to)
            => _NewShapeModifierCount(BuildLine(from, to));
        private static int BuildLine(Vector3 from, Vector3 to)
        {
            _NewShape(coordinateOrigin, 1);
            _Lines(stackalloc Vector3[2] { from, to });
            return 1;
        }

        /// <summary>
        /// Draws a line starting at 'from' ending at 'to' with a thickness of 'lineWeight'
        /// </summary>
        public static ShapeModifier DrawWeightedLine(Vector2 from, Vector2 to, float lineWeight, bool roundedTips)
            => _NewShapeModifierCount(BuildWeightedLine(from, to, lineWeight, roundedTips));
        private static int BuildWeightedLine(Vector2 from, Vector2 to, float lineWeight, bool roundedTips)
        {
            lineWeight = Mathf.Abs(lineWeight);
            if (lineWeight == 0)
                return BuildLine(from, to);

            Vector2 centerPosition = (from + to) / 2;
            float rotation = Vector2.SignedAngle(Vector2.right, to - from);
            _NewShape(coordinateOrigin, roundedTips ? 3 : 1);
            _SolidBox(centerPosition, new Vector2(Vector2.Distance(from, to), lineWeight), rotation);
            if (roundedTips)
            {
                float halfHeight = Vector2.Distance(from, to) / 2;
                _SolidArc((centerPosition + (Vector2)coordinateOrigin) + Vector2.right.Rotate(rotation) * halfHeight, lineWeight / 2, 180, rotation - 90, ArcCloseType.Flat, 0);
                _SolidArc((centerPosition + (Vector2)coordinateOrigin) - Vector2.right.Rotate(rotation) * halfHeight, lineWeight / 2, 180, rotation + 90, ArcCloseType.Flat, 0);
            }
            return 1;
        }

        /// <summary>
        /// Draws a dashed line starting at 'from' ending at 'to'.
        /// </summary>
        public static ShapeModifier DrawDashedLine(Vector3 from, Vector3 to, float dashLength, float gapSize)
            => _NewShapeModifierCount(BuildWeightedDashedLine(from, to, dashLength, gapSize, 0));

        /// <summary>
        /// Draws a dashed line starting at 'from' ending at 'to' with a thickness of 'lineWeight'.
        /// </summary>
        public static ShapeModifier DrawWeightedDashedLine(Vector3 from, Vector3 to, float dashLength, float gapSize, float lineWidth)
           => _NewShapeModifierCount(BuildWeightedDashedLine(from, to, dashLength, gapSize, lineWidth));
        private static int BuildWeightedDashedLine(Vector3 from, Vector3 to, float dashLength, float gapSize, float lineWeight)
        {
            lineWeight = Mathf.Abs(lineWeight);

            if (dashLength <= 0)
                return 0;

            if (gapSize <= 0)
                return BuildWeightedLine(from, to, lineWeight, false);

            float distance = Vector2.Distance(from, to);
            float dashPlusGap = dashLength + gapSize;
            int numDashes = Mathf.CeilToInt(distance / dashPlusGap);
            Vector3 direction = (to - from).normalized;

            bool extraLine = false;
            Span<Vector3> vertices = stackalloc Vector3[numDashes * 2];
            int index = 0;
            for (int i = 0; i < numDashes; i++)
            {
                vertices[index++] = from + direction * dashPlusGap * i;

                if (i == numDashes - 1 && (dashPlusGap * i + dashLength) > distance)
                {
                    vertices[index++] = to;
                }
                else
                {
                    vertices[index++] = from + direction * (dashPlusGap * i + dashLength);
                    if (i == numDashes - 1 && (distance - (dashPlusGap * i + dashLength) > lineWeight))
                        extraLine = true;
                }
            }

            int numShapes = 0;
            if (lineWeight == 0)
            {
                _NewShape(coordinateOrigin, 1);
                _Lines(vertices);
                numShapes++;
            }
            else
            {
                index = 0;
                for (int i = 0; i < numDashes; i++)
                {
                    BuildWeightedLine(vertices[index], vertices[index + 1], lineWeight, false);
                    index += 2;
                }
                numShapes += numDashes;
            }

            if (extraLine)
            {
                Span<Vector3> addedVertices = stackalloc Vector3[2];
                addedVertices[0] = to - direction * lineWeight;
                addedVertices[1] = to;
                BuildWeightedLine(addedVertices[0], addedVertices[1], lineWeight, false);
                numShapes++;
            }

            return numShapes;
        }

        /// <summary>
        /// Draws a dotted line starting at 'from' ending at 'to' with circle radii of 'dotRadius'.
        /// </summary>

        public static ShapeModifier DrawDottedLine(Vector2 from, Vector2 to, float dotRadius, float gapSize)
            => _NewShapeModifierCount(BuildDottedLine(from, to, dotRadius, gapSize));
        private static int BuildDottedLine(Vector2 from, Vector2 to, float dotRadius, float gapSize)
        {
            if (dotRadius == 0)
                return 0;

            dotRadius = Mathf.Abs(dotRadius);
            gapSize = Mathf.Max(gapSize, .001f);

            if (from == to)
                return BuildSolidCircle(from, dotRadius, -2);

            float distance = Vector2.Distance(from, to);
            int numDots = Mathf.FloorToInt(distance / gapSize);
            Vector2 direction = (to - from).normalized;

            Span<Vector3> vertices = stackalloc Vector3[numDots];
            for (int i = 0; i < numDots; i++)
            {
                vertices[i] = from + direction * gapSize * i;
            }

            for (int i = 0; i < numDots; i++)
            {
                BuildSolidCircle(vertices[i], dotRadius, -2);
            }

            return numDots;
        }

        /// <summary>
        /// Draws a bezier curve starting at 'from' ending at 'to'. Curve [typically between -1 and 1]
        /// </summary>
        public static ShapeModifier DrawBezier(Vector2 from, Vector2 to, float curve = .75f, int numEdges = 0)
            => _NewShapeModifierCount(BuildBezier(from, to, curve, numEdges));
        public static int BuildBezier(Vector2 from, Vector2 to, float curve = .75f, int numEdges = 0)
        {
            float lerpCenter = Extensions.Remap(-1, 1, 0, 1, curve);

            Vector2 p1c = new Vector2(from.x, to.y);
            Vector2 p4c = new Vector2(to.x, from.y);

            Vector2 p2 = Vector2.LerpUnclamped(p1c, p4c, lerpCenter);
            Vector2 p3 = Vector2.LerpUnclamped(p1c, p4c, 1 - lerpCenter);

            int defaultMult = numEdges >= 0 ? 1 : Mathf.Abs(numEdges);

            if (numEdges <= 0)
                numEdges = (int)Mathf.Clamp(Mathf.Pow(25, Mathf.Sqrt(Mathf.Sqrt(Mathf.Sqrt(Mathf.Abs(curve))))), 1, 75) * defaultMult;

            Span<Vector2> vertices = stackalloc Vector2[numEdges + 1];

            float t = 0;
            for (int i = 0; i <= numEdges; i++)
            {
                t = (float)i / (float)numEdges;
                Vector2 point = Mathf.Pow(1 - t, 3) * from +
                                3 * Mathf.Pow(1 - t, 2) * t * p2 +
                                3 * (1 - t) * Mathf.Pow(t, 2) * p3 +
                                Mathf.Pow(t, 3) * to;
                vertices[i] = point;
            }

            return BuildLineStrip(vertices, false);
        }

        /// <summary>
        /// Draws a path connecting the points in 'points'
        /// </summary>
        public static ShapeModifier DrawPath(List<Vector2> points) => _NewShapeModifierCount(BuildLineStrip(points, false));
        /// <summary>
        /// Draws a path connecting the points in 'points'
        /// </summary>
        public static ShapeModifier DrawPath(List<Vector3> points) => _NewShapeModifierCount(BuildLineStrip(points, false));
        /// <summary>
        /// Draws a path connecting the points in 'points'
        /// </summary>
        public static ShapeModifier DrawPath(Span<Vector2> points) => _NewShapeModifierCount(BuildLineStrip(points, false));
        /// <summary>
        /// Draws a path connecting the points in 'points'
        /// </summary>
        public static ShapeModifier DrawPath(Span<Vector3> points) => _NewShapeModifierCount(BuildLineStrip(points, false));

        /// <summary>
        /// Draws an open polygon connecting the points in 'vertices'
        /// </summary>
        public static ShapeModifier DrawOpenPolygon(List<Vector2> vertices) => _NewShapeModifierCount(BuildLineStrip(vertices, true));
        /// <summary>
        /// Draws an open polygon connecting the points in 'vertices'
        /// </summary>
        public static ShapeModifier DrawOpenPolygon(List<Vector3> vertices) => _NewShapeModifierCount(BuildLineStrip(vertices, true));
        /// <summary>
        /// Draws an open polygon connecting the points in 'vertices'
        /// </summary>
        public static ShapeModifier DrawOpenPolygon(Span<Vector2> vertices) => _NewShapeModifierCount(BuildLineStrip(vertices, true));
        /// <summary>
        /// Draws an open polygon connecting the points in 'vertices'
        /// </summary>
        public static ShapeModifier DrawOpenPolygon(Span<Vector3> vertices) => _NewShapeModifierCount(BuildLineStrip(vertices, true));

        /// <summary>
        /// Draws a solid polygon connecting the points in 'vertices'.
        /// Optional 'canter' defines position triangles are fanned out from (Default is the average position of the vertices)
        /// </summary>
        public static ShapeModifier DrawSolidPolygon(List<Vector2> vertices, Vector2? center = null) => _NewShapeModifierCount(BuildSolidPolygon(vertices, center));
        /// <summary>
        /// Draws a solid polygon connecting the points in 'vertices'.
        /// Optional 'canter' defines position triangles are fanned out from (Default is the average position of the vertices)
        /// </summary>
        public static ShapeModifier DrawSolidPolygon(List<Vector3> vertices, Vector2? center = null) => _NewShapeModifierCount(BuildSolidPolygon(vertices, center));
        /// <summary>
        /// Draws a solid polygon connecting the points in 'vertices'.
        /// Optional 'canter' defines position triangles are fanned out from (Default is the average position of the vertices)
        /// </summary>
        public static ShapeModifier DrawSolidPolygon(Span<Vector2> vertices, Vector2? center = null) => _NewShapeModifierCount(BuildSolidPolygon(vertices, center));
        /// <summary>
        /// Draws a solid polygon connecting the points in 'vertices'.
        /// Optional 'canter' defines position triangles are fanned out from (Default is the average position of the vertices)
        /// </summary>
        public static ShapeModifier DrawSolidPolygon(Span<Vector3> vertices, Vector2? center = null) => _NewShapeModifierCount(BuildSolidPolygon(vertices, center));

        private static int BuildLineStrip(List<Vector2> vertices, bool closed)
        {
            if (vertices == null)
                return 0;

            Span<Vector3> vertices3 = stackalloc Vector3[vertices.Count];
            for (int i = 0; i < vertices3.Length; i++)
                vertices3[i] = vertices[i];

            return BuildLineStrip(vertices3, closed);
        }
        private static int BuildLineStrip(List<Vector3> vertices, bool closed)
        {
            if (vertices == null)
                return 0;

            Span<Vector3> vertices3 = stackalloc Vector3[vertices.Count];
            for (int i = 0; i < vertices3.Length; i++)
                vertices3[i] = vertices[i];

            return BuildLineStrip(vertices3, closed);
        }
        private static int BuildLineStrip(Span<Vector2> vertices, bool closed)
        {
            Span<Vector3> vertices3 = stackalloc Vector3[vertices.Length];
            for (int i = 0; i < vertices3.Length; i++)
                vertices3[i] = vertices[i];

            return BuildLineStrip(vertices3, closed);
        }

        private static int BuildLineStrip(Span<Vector3> vertices, bool closed)
        {
            if (vertices == null || vertices.Length == 0)
                return 0;

            if (closed && vertices.Length == 3)
            {
                return BuildTriangle(vertices[0], vertices[1], vertices[2], false);
            }
            else if (closed && vertices.Length == 4)
            {
                return BuildOpenQuad(vertices[0], vertices[1], vertices[2], vertices[3]);
            }

            _NewShape(coordinateOrigin, 1);
            _LineStrip(vertices, closed);
            return 1;
        }

        private static int BuildSolidPolygon(List<Vector2> vertices, Vector2? center = null)
        {
            if (vertices == null)
                return 0;

            Span<Vector3> vertices3 = stackalloc Vector3[vertices.Count];
            for (int i = 0; i < vertices3.Length; i++)
                vertices3[i] = vertices[i];

            return BuildSolidPolygon(vertices3, center);
        }
        private static int BuildSolidPolygon(List<Vector3> vertices, Vector2? center = null)
        {
            if (vertices == null)
                return 0;

            Span<Vector3> vertices3 = stackalloc Vector3[vertices.Count];
            for (int i = 0; i < vertices3.Length; i++)
                vertices3[i] = vertices[i];

            return BuildSolidPolygon(vertices3, center);
        }
        private static int BuildSolidPolygon(Span<Vector2> vertices, Vector2? center = null)
        {
            Span<Vector3> vertices3 = stackalloc Vector3[vertices.Length];
            for (int i = 0; i < vertices3.Length; i++)
                vertices3[i] = vertices[i];

            return BuildSolidPolygon(vertices3, center);
        }

        private static int BuildSolidPolygon(Span<Vector3> vertices, Vector2? center = null)
        {
            if (vertices.Length == 0)
                return 0;

            if (vertices.Length <= 2)
            {
                return BuildLineStrip(vertices, false);
            }
            else if (vertices.Length == 3)
            {
                return BuildTriangle(vertices[0], vertices[1], vertices[2], true);
            }
            else if (vertices.Length == 4)
            {
                return BuildSolidQuad(vertices[0], vertices[1], vertices[2], vertices[3]);
            }

            _NewShape(coordinateOrigin, 1);

            if (center == null)
                _SolidPolygon(vertices);
            else
                _SolidFanOut(center.Value, vertices, true);

            return 1;
        }
        /// <summary>
        /// Draws a solid polygon connecting the points in 'vertices'.
        /// 'colors' chooses the color for each triangle filled in.
        /// Optional center defines the position triangles are fanned out from. (Default is the average position of the vertices)
        /// </summary>
        public static ShapeModifier DrawMultiColoredPolygon(List<Vector2> points, List<Color> colors, Vector2? center = null)
        {
            if (points == null)
                return _NullShapeModifier();

            Span<Vector2> vertices = stackalloc Vector2[points.Count];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = points[i];

            Span<Color> colorsSpan = stackalloc Color[colors.Count];
            for (int i = 0; i < colorsSpan.Length; i++)
                colorsSpan[i] = colors[i];

            return DrawMultiColoredPolygon(vertices, colorsSpan, center);
        }
        /// <summary>
        /// Draws a solid polygon connecting the points in 'vertices'.
        /// 'colors' chooses the color for each triangle filled in.
        /// Optional center defines the position triangles are fanned out from. (Default is the average position of the vertices)
        /// </summary>
        public static ShapeModifier DrawMultiColoredPolygon(List<Vector2> points, Span<Color> colors, Vector2? center = null)
        {
            if (points == null)
                return _NullShapeModifier();

            Span<Vector2> vertices = stackalloc Vector2[points.Count];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = points[i];

            return DrawMultiColoredPolygon(vertices, colors, center);
        }
        /// <summary>
        /// Draws a solid polygon connecting the points in 'vertices'.
        /// 'colors' chooses the color for each triangle filled in.
        /// Optional center defines the position triangles are fanned out from. (Default is the average position of the vertices)
        /// </summary>
        public static ShapeModifier DrawMultiColoredPolygon(Span<Vector2> points, List<Color> colors, Vector2? center = null)
        {
            if (points == null)
                return _NullShapeModifier();

            Span<Color> colorsSpan = stackalloc Color[colors.Count];
            for (int i = 0; i < colorsSpan.Length; i++)
                colorsSpan[i] = colors[i];

            return DrawMultiColoredPolygon(points, colorsSpan, center);
        }
        /// <summary>
        /// Draws a solid polygon connecting the points in 'vertices'.
        /// 'colors' chooses the color for each triangle filled in.
        /// Optional center defines the position triangles are fanned out from. (Default is the average position of the vertices)
        /// </summary>
        public static ShapeModifier DrawMultiColoredPolygon(Span<Vector2> points, Span<Color> colors, Vector2? center = null)
        {
            if (points.Length == 0)
                return _NullShapeModifier();

            if (points.Length <= 2)
            {
                if (colors != null && colors.Length > 0)
                    return DrawPath(points).SetColor(colors[0]);
                else
                    return DrawPath(points);
            }

            Span<Vector3> vertices = stackalloc Vector3[points.Length];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = points[i];

            if (vertices.Length == 3)
            {
                if (colors != null && colors.Length > 0)
                    return DrawSolidTriangle(vertices[0], vertices[1], vertices[2]).SetColor(colors[0]);
                else
                    return DrawSolidTriangle(vertices[0], vertices[1], vertices[2]);
            }

            _NewShape(coordinateOrigin, points.Length);
            if (center == null)
            {
                Vector3 averageVertex = Vector3.zero;
                for (int i = 0; i < vertices.Length; i++)
                {
                    averageVertex += vertices[i];
                }
                averageVertex /= vertices.Length;
                center = averageVertex;
            }
            for (int i = 0; i < points.Length; i++)
            {
                _Triangle(center.Value, points[i], points[(i + 1) % points.Length], true, colors[i % colors.Length]);
            }

            return _NewShapeModifierBacktracked(1);
        }
        #endregion

        #region ### Triangles
        /// <summary>
        /// Returns the 3 vertices of a triangle defined by a height, width, skew, rotation, and center offset
        /// </summary>
        public static (Vector2, Vector2, Vector2) GetTriangleVertices(Vector2 centerOffset, float height, float width, float skew, float rotation)
        {
            float adjustedSkew = Extensions.Remap(-1, 1, 0, 1, skew);
            Vector2 adjustedOffset = (Vector2.up * centerOffset.y * height / 2) + (Vector2.right * centerOffset.x * width / 2);

            Vector2 v1 = new Vector2(-width / 2, -height / 2);
            Vector2 v2 = new Vector2(width / 2, -height / 2);
            Vector2 v3 = new Vector2(Mathf.LerpUnclamped(-width / 2, width / 2, adjustedSkew), height / 2);

            v1 = v1.Rotate(rotation) + adjustedOffset.Rotate(rotation);
            v2 = v2.Rotate(rotation) + adjustedOffset.Rotate(rotation);
            v3 = v3.Rotate(rotation) + adjustedOffset.Rotate(rotation);

            return (v1, v2, v3);
        }
        /// <summary>
        /// Returns the intercenter of a triangle defined by 3 vertices.
        /// The intercenter is the center of the inscribed circle (incircle) that touches all three edges.
        /// </summary>
        public static Vector2 GetTriangleIntercenter(Vector3 A, Vector3 B, Vector3 C)
        {
            float a = Vector2.Distance(B, C);
            float b = Vector2.Distance(A, C);
            float c = Vector2.Distance(A, B);

            float perimeter = a + b + c;
            if (perimeter <= 0) return A;

            Vector2 weightedAngleSum = (A * a) + (B * b) + (C * c);
            Vector2 I = weightedAngleSum / perimeter;

            return I;
        }
        /// <summary>
        /// Returns the radius of the incircle of a triangle defined by 3 vertices.
        /// The incircle is the inscribed circle that touches all three edges.
        /// </summary>
        public static float GetTriangleInradius(Vector3 A, Vector3 B, Vector3 C)
        {
            float a = Vector2.Distance(B, C);
            float b = Vector2.Distance(A, C);
            float c = Vector2.Distance(A, B);

            float halfPerimeter = (a + b + c) / 2;
            if (halfPerimeter <= 0) return 0;

            // Shoelace Formula!
            float area = Mathf.Abs(Vector3.Cross(B - A, C - A).z) * 0.5f;
            float I = area / halfPerimeter;

            return I;
        }

        /// <summary>
        /// Draws an open triangle based on a center position, height, and width. Angle rotates the triangle. Skew offsets the point opposite the base edge
        /// </summary>
        public static ShapeModifier DrawOpenTriangle(Vector3 position, Vector2 centerOffset, float height, float width, float skew, float rotation)
            => DrawTriangle(position, centerOffset, height, width, skew, rotation, false);
        /// <summary>
        /// Draws a solid triangle based on a center position, height, and width. Angle rotates the triangle. Skew offsets the point opposite the base edge
        /// </summary>
        public static ShapeModifier DrawSolidTriangle(Vector3 position, Vector2 centerOffset, float height, float width, float skew, float rotation)
            => DrawTriangle(position, centerOffset, height, width, skew, rotation, true);
        private static ShapeModifier DrawTriangle(Vector3 position, Vector2 centerOffset, float height, float width, float skew, float rotation, bool solid)
            => _NewShapeModifierCount(BuildTriangle(position, centerOffset, height, width, skew, rotation, solid));
        private static int BuildTriangle(Vector3 position, Vector2 centerOffset, float height, float width, float skew, float rotation, bool solid)
        {
            (Vector2 v1, Vector2 v2, Vector2 v3) = GetTriangleVertices(centerOffset, height, width, skew, rotation);
            return BuildTriangle(position, v1, v2, v3, solid);
        }

        private static ShapeModifier DrawOpenTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
            => _NewShapeModifierCount(BuildTriangle(coordinateOrigin, v1, v2, v3, false));
        private static ShapeModifier DrawSolidTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
            => _NewShapeModifierCount(BuildTriangle(coordinateOrigin, v1, v2, v3, true));
        private static ShapeModifier DrawTriangle(Vector3 v1, Vector3 v2, Vector3 v3, bool solid)
            => _NewShapeModifierCount(BuildTriangle(coordinateOrigin, v1, v2, v3, solid));
        private static int BuildTriangle(Vector3 v1, Vector3 v2, Vector3 v3, bool solid)
            => BuildTriangle(coordinateOrigin, v1, v2, v3, solid);
        private static int BuildTriangle(Vector3 origin, Vector3 v1, Vector3 v2, Vector3 v3, bool solid)
        {
            _NewShape(origin, 1);
            _Triangle(v1, v2, v3, solid);
            return 1;
        }

        /// <summary>
        /// Draws a weighted triangle based on a center position, height, width, and border width. Angle rotates the triangle. Skew offsets the point opposite the base edge
        /// </summary>
        public static ShapeModifier DrawWeightedTriangle(Vector3 position, Vector2 centerOffset, float height, float width, float skew, float rotation, float borderWidth, BorderType borderType)
            => _NewShapeModifierCount(BuildWeightedTriangle(position, centerOffset, height, width, skew, rotation, borderWidth, borderType));
        private static int BuildWeightedTriangle(Vector3 position, Vector2 centerOffset, float height, float width, float skew, float rotation, float borderWidth, BorderType borderType)
        {
            (Vector2 v1, Vector2 v2, Vector2 v3) = GetTriangleVertices(centerOffset, height, width, skew, rotation);
            return BuildWeightedTriangle(position, v1, v2, v3, borderWidth, borderType);
        }
        private static int BuildWeightedTriangle(Vector3 origin, Vector2 v1, Vector2 v2, Vector2 v3, float borderWidth, BorderType borderType)
        {
            if (borderWidth == 0)
            {
                return BuildTriangle(origin, v1, v2, v3, false);
            }

            Vector2 intercenter = GetTriangleIntercenter(v1, v2, v3);
            float inradius = GetTriangleInradius(v1, v2, v3);
            float distI1 = Vector2.Distance(v1, intercenter);
            float distI2 = Vector2.Distance(v2, intercenter);
            float distI3 = Vector2.Distance(v3, intercenter);
            float scaleV1 = distI1 / inradius;
            float scaleV2 = distI2 / inradius;
            float scaleV3 = distI3 / inradius;
            Vector2 directionItoV1 = (v1 - intercenter).normalized;
            Vector2 directionItoV2 = (v2 - intercenter).normalized;
            Vector2 directionItoV3 = (v3 - intercenter).normalized;

            (borderWidth, borderType) = AdjustForNegativeBorderWidth(borderWidth, borderType);

            bool solid =
                (borderType == BorderType.Inside && borderWidth >= inradius) ||
                (borderType == BorderType.Outside && borderWidth <= -inradius) ||
                (borderType == BorderType.Centered && borderWidth >= inradius * 2);

            if (solid)
            {
                switch (borderType)
                {
                    case BorderType.Inside:
                    case BorderType.Outside:
                        return BuildTriangle(origin, v1, v2, v3, true);
                    case BorderType.Centered:
                        Vector2 V1 = v1 + (borderWidth / 2) * scaleV1 * directionItoV1;
                        Vector2 V2 = v2 + (borderWidth / 2) * scaleV2 * directionItoV2;
                        Vector2 V3 = v3 + (borderWidth / 2) * scaleV3 * directionItoV3;
                        return BuildTriangle(origin, V1, V2, V3, true);
                }
                return 0;
            }
            else
            {
                _NewShape(origin, 1);
                switch (borderType)
                {
                    case BorderType.Inside:
                        Vector2 V1 = v1 - (borderWidth * scaleV1 * directionItoV1);
                        Vector2 V2 = v2 - (borderWidth * scaleV2 * directionItoV2);
                        Vector2 V3 = v3 - (borderWidth * scaleV3 * directionItoV3);
                        _SolidQuads(new(Allocator.Temp)
                        {
                            v1, v2, V2, V1,
                            v2, v3, V3, V2,
                            v3, v1, V1, V3
                        });
                        break;
                    case BorderType.Outside:
                        V1 = v1 + (borderWidth * scaleV1 * directionItoV1);
                        V2 = v2 + (borderWidth * scaleV2 * directionItoV2);
                        V3 = v3 + (borderWidth * scaleV3 * directionItoV3);
                        _SolidQuads(new(Allocator.Temp)
                        {
                            v1, v2, V2, V1,
                            v2, v3, V3, V2,
                            v3, v1, V1, V3
                        });
                        break;
                    case BorderType.Centered:
                        float halfWidth = borderWidth / 2;
                        Vector2 V1A = v1 + (halfWidth * scaleV1 * directionItoV1);
                        Vector2 V2A = v2 + (halfWidth * scaleV2 * directionItoV2);
                        Vector2 V3A = v3 + (halfWidth * scaleV3 * directionItoV3);
                        Vector2 V1B = v1 - (halfWidth * scaleV1 * directionItoV1);
                        Vector2 V2B = v2 - (halfWidth * scaleV2 * directionItoV2);
                        Vector2 V3B = v3 - (halfWidth * scaleV3 * directionItoV3);
                        _SolidQuads(new(Allocator.Temp)
                        {
                            V1A, V2A, V2B, V1B,
                            V2A, V3A, V3B, V2B,
                            V3A, V1A, V1B, V3B
                        });
                        break;
                    default:
                        return 0;
                }

                return 1;
            }
        }

        /// <summary>
        /// Draws open triangles using every 3 vertices in 'vertices'
        /// </summary>
        [Obsolete("This method is no longer in use")]
        public static void DrawOpenTriangles(Vector2[] vertices, Color? colorSetting = null) { }

        /// <summary>
        /// Draws solid triangles using every 3 vertices in 'vertices'
        /// </summary>
        [Obsolete("This method is no longer in use")]
        public static void DrawSolidTriangles(Vector2[] vertices, Color? colorSetting = null) { }
        #endregion

        #region ### Capsules
        /// <summary>
        /// Draws an open capsule at 'position' based on box size and capsule direction
        /// </summary>
        public static ShapeModifier DrawOpenCapsule(Vector3 position, Vector2 size, CapsuleDirection2D direction, float angle = 0)
            => _NewShapeModifierCount(BuildCapsule(position, size, direction, angle, false));

        /// <summary>
        /// Draws an open capsule starting at 'from' ending at 'to' with 'radius'
        /// </summary>
        public static ShapeModifier DrawOpenCapsule(Vector2 from, Vector2 to, float radius)
            => _NewShapeModifierCount(BuildCapsule(from, to, radius, false));

        /// <summary>
        /// Draws a solid capsule at 'position' based on box size and capsule direction
        /// </summary>
        public static ShapeModifier DrawSolidCapsule(Vector3 position, Vector2 size, CapsuleDirection2D direction, float angle = 0)
            => _NewShapeModifierCount(BuildCapsule(position, size, direction, angle, true));

        /// <summary>
        /// Draws a solid capsule starting at 'from' ending at 'to' with 'radius'
        /// </summary>
        public static ShapeModifier DrawSolidCapsule(Vector2 from, Vector2 to, float radius)
                => _NewShapeModifierCount(BuildCapsule(from, to, radius, true));

        private static int BuildCapsule(Vector2 from, Vector2 to, float radius, bool solid)
        {
            Vector2 center = Vector2.Lerp(from, to, .5f);
            Vector2 size = new Vector2(radius * 2, Vector2.Distance(from, to) + radius * 2);
            float angle = Vector2.SignedAngle(Vector2.up, from - center);

            return BuildCapsule(center, size, CapsuleDirection2D.Vertical, angle, solid);
        }
        private static int BuildCapsule(Vector3 position, Vector2 size, CapsuleDirection2D direction, float angle, bool solid)
        {
            size = size.Abs();
            float radius = direction == CapsuleDirection2D.Vertical ? size.x / 2 : size.y / 2;
            float difference = direction == CapsuleDirection2D.Vertical ?
                (size.y > size.x ? (size.y - size.x) / 2 : 0) :
                (size.x > size.y ? (size.x - size.y) / 2 : 0);

            float offsetAngle = (direction == CapsuleDirection2D.Vertical ? 0 : 90) + angle;
            Vector2 curveOffsetDirection = (direction == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.left).Rotate(angle);
            Vector2 orientationSize = direction == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.right;

            _NewShape(position, 3);
            if (!solid)
            {
                bool vertical = direction == CapsuleDirection2D.Vertical;
                _PartialOpenBox(Vector3.zero, (size - (radius * 2 * orientationSize)).ZeroNegatives(), angle, !vertical, !vertical, vertical, vertical);
                _OpenCircle(curveOffsetDirection * difference, radius, 180, offsetAngle, 0, false);
                _OpenCircle(-curveOffsetDirection * difference, radius, 180, 180 + offsetAngle, 0, false);
            }
            else
            {
                _SolidBox(Vector3.zero, (size - (radius * 2 * orientationSize)).ZeroNegatives(), angle);
                _SolidCircle(curveOffsetDirection * difference, radius, 180, offsetAngle, 0);
                _SolidCircle(-curveOffsetDirection * difference, radius, 180, 180 + offsetAngle, 0);
            }

            return 1;
        }

        private static Vector2 GetWeightedCapsuleInnerCapsuleSize(Vector2 from, Vector2 to, float radius, float borderWidth, BorderType borderType)
        {
            Vector2 size = new Vector2(radius * 2, Vector2.Distance(from, to) + radius * 2);
            return GetWeightedCapsuleInnerCapsuleSize(size, CapsuleDirection2D.Vertical, borderWidth, borderType);
        }

        private static Vector2 GetWeightedCapsuleInnerCapsuleSize(Vector2 size, CapsuleDirection2D direction, float borderWidth, BorderType borderType)
        {
            float radius = direction == CapsuleDirection2D.Vertical ? size.x / 2 : size.y / 2;

            (borderWidth, borderType) = AdjustForNegativeBorderWidth(borderWidth, borderType);
            bool solid =
                (borderType == BorderType.Outside && borderWidth <= -radius) ||
                (borderType == BorderType.Inside && borderWidth >= radius) ||
                (borderType == BorderType.Centered && borderWidth >= radius * 2);

            if (!solid)
            {
                switch (borderType)
                {
                    case BorderType.Outside:
                        return size;
                    case BorderType.Inside:
                        return size = Vector2.one * borderWidth * 2;
                    case BorderType.Centered:
                        return size = Vector2.one * borderWidth;
                }
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Draws an weighted capsule with edge thickness 'borderWidth' at 'position' based on box size and capsule direction
        /// </summary>
        public static ShapeModifier DrawWeightedCapsule(Vector3 position, Vector2 size, CapsuleDirection2D direction, float angle, float borderWidth, BorderType borderType)
            => _NewShapeModifierCount(BuildWeightedCapsule(position, size, direction, angle, borderWidth, borderType));
        /// <summary>
        /// Draws a weighted capsule with edge thickness 'borderWidth' starting at 'from' ending at 'to' with 'radius'
        /// </summary>
        public static ShapeModifier DrawWeightedCapsule(Vector2 from, Vector2 to, float radius, float borderWidth, BorderType borderType)
        {
            Vector2 center = Vector2.Lerp(from, to, .5f);
            Vector2 size = new Vector2(radius * 2, Vector2.Distance(from, to) + radius * 2);
            float angle = Vector2.SignedAngle(Vector2.up, from - center);
            return _NewShapeModifierCount(BuildWeightedCapsule(center, size, CapsuleDirection2D.Vertical, angle, borderWidth, borderType));
        }
        private static int BuildWeightedCapsule(Vector3 position, Vector2 size, CapsuleDirection2D direction, float angle, float borderWidth, BorderType borderType)
        {
            if (borderWidth == 0)
                return BuildCapsule(position, size, direction, angle, false);

            size = size.Abs();
            float radius = direction == CapsuleDirection2D.Vertical ? size.x / 2 : size.y / 2;
            float difference = direction == CapsuleDirection2D.Vertical ?
                (size.y > size.x ? (size.y - size.x) / 2 : 0) :
                (size.x > size.y ? (size.x - size.y) / 2 : 0);

            float offsetAngle = (direction == CapsuleDirection2D.Vertical ? 0 : 90) + angle;
            Vector2 curveOffsetDirection = (direction == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.left).Rotate(angle);
            Vector2 orientationSize = direction == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.right;


            (borderWidth, borderType) = AdjustForNegativeBorderWidth(borderWidth, borderType);
            bool solid =
                (borderType == BorderType.Outside && borderWidth <= -radius) ||
                (borderType == BorderType.Inside && borderWidth >= radius) ||
                (borderType == BorderType.Centered && borderWidth >= radius * 2);

            int numShapes = 0;
            Vector2 boxSize = (size - (radius * 2 * orientationSize)).ZeroNegatives();
            if (!solid)
            {
                numShapes += BuildWeightedCircle(position + (Vector3)(curveOffsetDirection * difference), radius, 180, offsetAngle, borderWidth, borderType, ArcCloseType.None);
                numShapes += BuildWeightedCircle(position - (Vector3)(curveOffsetDirection * difference), radius, 180, 180 + offsetAngle, borderWidth, borderType, ArcCloseType.None);

                float rectOffset = 0;
                switch (borderType)
                {
                    case BorderType.Outside:
                        rectOffset = borderWidth / 2;
                        break;
                    case BorderType.Inside:
                        rectOffset = -borderWidth / 2;
                        break;
                }

                bool vertical = direction == CapsuleDirection2D.Vertical;
                float angleAdd = vertical ? 0 : 90;
                numShapes += BuildSolidBox(position + (Vector3)(curveOffsetDirection.Rotate90CCW() * (radius + rectOffset)), new Vector2(borderWidth, boxSize.Rotate(angleAdd).y), angle + angleAdd);
                numShapes += BuildSolidBox(position + (Vector3)(curveOffsetDirection.Rotate90CW() * (radius + rectOffset)), new Vector2(borderWidth, boxSize.Rotate(angleAdd).y), angle + angleAdd);
            }
            else
            {
                switch (borderType)
                {
                    case BorderType.Outside:
                    case BorderType.Inside:
                        numShapes += BuildCapsule(position, size, direction, angle, true);
                        break;
                    case BorderType.Centered:
                        numShapes += BuildCapsule(position, size + Vector2.one * borderWidth, direction, angle, true);
                        break;
                }
            }

            return numShapes;
        }

        /// <summary>
        /// Draws a solid path with 'thickness' by connecting capsules with endpoints at 'points'
        /// </summary>
        public static ShapeModifier DrawSolidCapsulePath(List<Vector2> points, float thickness)
            => _NewShapeModifierCount(BuildSolidCapsulePath(points, thickness));
        /// <summary>
        /// Draws a solid path with 'thickness' by connecting capsules with endpoints at 'points'
        /// </summary>
        public static ShapeModifier DrawSolidCapsulePath(Span<Vector2> points, float thickness)
            => _NewShapeModifierCount(BuildSolidCapsulePath(points, thickness));
        private static int BuildSolidCapsulePath(List<Vector2> points, float thickness)
        {
            if (points == null)
                return 0;

            Span<Vector2> vertices = stackalloc Vector2[points.Count];
            for (int i = 0; i < points.Count; i++)
                vertices[i] = points[i];

            return BuildSolidCapsulePath(vertices, thickness);
        }
        private static int BuildSolidCapsulePath(Span<Vector2> points, float thickness)
        {
            int numShapes = 0;
            for (int i = 1; i < points.Length; i++)
                numShapes += BuildCapsule(points[i - 1], points[i], thickness / 2, true);

            return numShapes;
        }
        #endregion

        #region ### Misc
        /// <summary>
        /// Returns a positive border width and opposite BorderType to match
        /// </summary>
        public static (float, BorderType) AdjustForNegativeBorderWidth(float borderWidth, BorderType borderType)
        {
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

            return (borderWidth, borderType);
        }

        /// <summary>
        /// Draws any 2D collider shape
        /// </summary>
        static List<Vector2> colliderPaths = new();
        public static ShapeModifier DrawCollider2D(Collider2D collider, bool solid)
        {
            colliderPaths.Clear();
            if (collider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = (BoxCollider2D)collider;

                Vector3 position = boxCollider.transform.position + boxCollider.transform.right * boxCollider.offset.x + boxCollider.transform.up * boxCollider.offset.y;
                Vector2 size = boxCollider.size.ScaleEach(boxCollider.transform.lossyScale.Abs());
                if (solid)
                    return DrawSolidBoxEdgeRadius(position, size, boxCollider.edgeRadius, 0, true, BorderType.Outside).SetLookRotation(collider.transform.forward, collider.transform.up);
                else
                    return DrawOpenBoxEdgeRadius(position, size, boxCollider.edgeRadius, 0, true, BorderType.Outside).SetLookRotation(collider.transform.forward, collider.transform.up);
            }
            else if (collider is CompositeCollider2D)
            {
                CompositeCollider2D compositeCollider = (CompositeCollider2D)collider;

                int numShapes = 0;
                for (int i = 0; i < compositeCollider.pathCount; i++)
                {
                    compositeCollider.GetPath(i, colliderPaths);

                    //for (int j = 0; j < colliderPaths.Count; j++)
                    //{
                    //    colliderPaths[j] = colliderPaths[j].Rotate(compositeCollider.transform.rotation.eulerAngles.z);
                    //}

                    if (solid)
                        numShapes += BuildSolidPolygon(colliderPaths);
                    else
                        numShapes += BuildSolidPolygon(colliderPaths);
                }

                return _NewShapeModifierCount(numShapes).SetOrigin(compositeCollider.transform.position).SetLookRotation(collider.transform.forward, collider.transform.up);
            }
            else if (collider is CircleCollider2D)
            {
                CircleCollider2D circleCollider = (CircleCollider2D)collider;

                Vector3 position = circleCollider.transform.position + (circleCollider.transform.right * circleCollider.offset.x) + (circleCollider.transform.up * circleCollider.offset.y);
                float radius = circleCollider.radius * circleCollider.transform.lossyScale.Abs().Max();
                if (solid)
                    return DrawSolidCircle(position, radius).SetLookRotation(collider.transform.forward, collider.transform.up);
                else
                    return DrawOpenCircle(position, radius).SetLookRotation(collider.transform.forward, collider.transform.up);
            }
            else if (collider is CapsuleCollider2D)
            {
                CapsuleCollider2D capsuleCollider2D = (CapsuleCollider2D)collider;
                Vector3 position = capsuleCollider2D.transform.position + (capsuleCollider2D.transform.right * capsuleCollider2D.offset.x) + (capsuleCollider2D.transform.up * capsuleCollider2D.offset.y);
                Vector2 size = capsuleCollider2D.size.ScaleEach(capsuleCollider2D.transform.lossyScale.Abs());
                float parentScale = capsuleCollider2D.transform.parent != null ? capsuleCollider2D.transform.parent.lossyScale.Abs().Max() : 1;
                if (solid)
                    return DrawSolidCapsule(position, size, capsuleCollider2D.direction, 0/*capsuleCollider2D.transform.rotation.eulerAngles.z * parentScale*/).SetLookRotation(collider.transform.forward, collider.transform.up);
                else
                    return DrawOpenCapsule(position, size, capsuleCollider2D.direction, 0).SetLookRotation(collider.transform.forward, collider.transform.up);
            }
            else if (collider is PolygonCollider2D)
            {
                PolygonCollider2D polygonCollider2D = (PolygonCollider2D)collider;
                Vector2 parentScale = polygonCollider2D.transform.parent != null ? polygonCollider2D.transform.parent.lossyScale : Vector2.one;
                polygonCollider2D.GetPath(0, colliderPaths);

                for (int i = 0; i < colliderPaths.Count; i++)
                {
                    colliderPaths[i] = colliderPaths[i].ScaleEach(polygonCollider2D.transform.localScale);//.Rotate(polygonCollider2D.transform.rotation.eulerAngles.z).ScaleEach(parentScale);
                }
                if (solid)
                    return DrawSolidPolygon(colliderPaths).SetOrigin(polygonCollider2D.transform.position).SetLookRotation(collider.transform.forward, collider.transform.up);
                else
                    return DrawOpenPolygon(colliderPaths).SetOrigin(polygonCollider2D.transform.position).SetLookRotation(collider.transform.forward, collider.transform.up);
            }
            else if (collider is EdgeCollider2D)
            {
                EdgeCollider2D edgeCollider2D = (EdgeCollider2D)collider;
                Vector2 parentScale = edgeCollider2D.transform.parent != null ? edgeCollider2D.transform.parent.lossyScale : Vector2.one;
                edgeCollider2D.GetPoints(colliderPaths);

                for (int i = 0; i < colliderPaths.Count; i++)
                {
                    colliderPaths[i] = colliderPaths[i].ScaleEach(edgeCollider2D.transform.localScale);//.Rotate(edgeCollider2D.transform.rotation.eulerAngles.z).ScaleEach(parentScale);
                }
                if (solid)
                    return DrawSolidPolygon(colliderPaths).SetOrigin(edgeCollider2D.transform.position).SetLookRotation(collider.transform.forward, collider.transform.up);
                else
                    return DrawPath(colliderPaths).SetOrigin(edgeCollider2D.transform.position).SetLookRotation(collider.transform.forward, collider.transform.up);
            }
            else
            {
                //Debug.LogError($"GLGizmos cannot draw type of {collider.GetType()}");
            }

            return _NullShapeModifier();
        }
        #endregion

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
        public static ShapeModifier DrawText(string text, Vector3 position, TMP_FontAsset font, float fontSize, TextBoxParams textBoxParams = new())
            => _NewShapeModifierCount(BuildText(text, position, font, fontSize, textBoxParams));
        private static int BuildText(string text, Vector3 position, TMP_FontAsset font, float fontSize, TextBoxParams textBoxParams)
        {
            textBoxParams.textBoxSize = textBoxParams.textBoxSize.Abs();
            if (font == null)
                font = TMP_Settings.defaultFontAsset;

            _NewShapeText(position, 1);
            _Text(text, font, fontSize, textBoxParams);
            return 1;
        }
        #endregion

        #region ### 3D Wire Shapes
        /// <summary>
        /// Draws a wireframe box at 'center' with 'size'
        /// </summary>
        public static ShapeModifier DrawWireCube(Vector3 center, Vector3 size)
            => _NewShapeModifierCount(BuildWireCube(center, size));
        private static int BuildWireCube(Vector3 center, Vector3 size)
        {
            float half_x = size.x / 2;
            float half_y = size.y / 2;
            float half_z = size.z / 2;

            _NewShape(center, 3);
            _OpenBox(-Vector3.forward * half_z, new Vector2(size.x, size.y), 0);
            _OpenBox(Vector3.forward * half_z, new Vector2(size.x, size.y), 0);

            Vector3 frontTL = new Vector3(-half_x, half_y, -half_z);
            Vector3 frontTR = new Vector3(half_x, half_y, -half_z);
            Vector3 frontBL = new Vector3(-half_x, -half_y, -half_z);
            Vector3 frontBR = new Vector3(half_x, -half_y, -half_z);
            Vector3 backTL = new Vector3(-half_x, half_y, half_z);
            Vector3 backTR = new Vector3(half_x, half_y, half_z);
            Vector3 backBL = new Vector3(-half_x, -half_y, half_z);
            Vector3 backBR = new Vector3(half_x, -half_y, half_z);
            Span<Vector3> connectorLines = stackalloc Vector3[8]
            {
                frontTL, backTL,
                frontTR, backTR,
                frontBL, backBL,
                frontBR, backBR
            };
            _Lines(connectorLines);

            return 1;
        }

        /// <summary>
        /// Draws a wireframe box at 'center' with 'size' and an inner edge thickness of 'borderWidth'
        /// </summary>
        public static ShapeModifier DrawWeightedWireCube(Vector3 center, Vector3 size, float borderWidth)
            => _NewShapeModifierCount(BuildWeightedWireCube(center, size, Mathf.Abs(borderWidth), BorderType.Inside));
        private static int BuildWeightedWireCube(Vector3 center, Vector3 size, float borderWidth, BorderType borderType)
        {
            float half_x = size.x / 2;
            float half_y = size.y / 2;
            float half_z = size.z / 2;

            int numBuilds = 0;
            // Front
            numBuilds += BuildWeightedBoxLookRotation(center, -Vector3.forward * half_z, Quaternion.identity, new Vector2(size.x, size.y), borderWidth, borderType);
            // Back
            numBuilds += BuildWeightedBoxLookRotation(center, Vector3.forward * half_z, Quaternion.identity, new Vector2(size.x, size.y), borderWidth, borderType);
            // Top
            numBuilds += BuildWeightedBoxLookRotation(center, Vector3.up * half_y, Quaternion.LookRotation(Vector3.up), new Vector2(size.x, size.z), borderWidth, borderType);
            // Bottom
            numBuilds += BuildWeightedBoxLookRotation(center, -Vector3.up * half_y, Quaternion.LookRotation(Vector3.up), new Vector2(size.x, size.z), borderWidth, borderType);
            // Right
            numBuilds += BuildWeightedBoxLookRotation(center, Vector3.right * half_x, Quaternion.LookRotation(Vector3.right), new Vector2(size.z, size.y), borderWidth, borderType);
            // Left
            numBuilds += BuildWeightedBoxLookRotation(center, -Vector3.right * half_x, Quaternion.LookRotation(Vector3.right), new Vector2(size.z, size.y), borderWidth, borderType);

            return numBuilds;
        }
        private static int BuildWeightedBoxLookRotation(Vector3 center, Vector3 offset, Quaternion lookRotation, Vector2 size, float borderWidth, BorderType borderType, float rotation = 0)
        {
            if (borderWidth == 0)
            {
                _NewShape(center, 1);
                _OpenBox(offset, lookRotation, size, rotation);
                return 1;
            }

            bool outsideFill;
            bool insideFill;
            (size, borderWidth, borderType, insideFill, outsideFill) = AdjustWeightedBoxParams(size, borderWidth, borderType);

            int count = (insideFill ? 1 : 0) + (outsideFill ? 1 : 0);
            _NewShape(center, count);

            if (insideFill)
                _SolidBox(offset, lookRotation, size, rotation);

            if (outsideFill)
            {
                Vector2 halfSize = size / 2;

                Vector3 innerTL = (Vector3)new Vector2(-halfSize.x, halfSize.y).Rotate(rotation);
                Vector3 innerTR = (Vector3)new Vector2(halfSize.x, halfSize.y).Rotate(rotation);
                Vector3 innerBL = (Vector3)new Vector2(-halfSize.x, -halfSize.y).Rotate(rotation);
                Vector3 innerBR = (Vector3)new Vector2(halfSize.x, -halfSize.y).Rotate(rotation);

                Vector3 outerTL = (Vector3)(new Vector2(-halfSize.x, halfSize.y) + new Vector2(-borderWidth, borderWidth)).Rotate(rotation);
                Vector3 outerTR = (Vector3)(new Vector2(halfSize.x, halfSize.y) + new Vector2(borderWidth, borderWidth)).Rotate(rotation);
                Vector3 outerBL = (Vector3)(new Vector2(-halfSize.x, -halfSize.y) + new Vector2(-borderWidth, -borderWidth)).Rotate(rotation);
                Vector3 outerBR = (Vector3)(new Vector2(halfSize.x, -halfSize.y) + new Vector2(borderWidth, -borderWidth)).Rotate(rotation);

                _SolidQuads(new NativeList<Vector3>(Allocator.Temp)
                {
                    innerTL, outerTL, outerTR, innerTR,
                    innerTR, outerTR, outerBR, innerBR,
                    innerBR, outerBR, outerBL, innerBL,
                    innerBL, outerBL, outerTL, innerTL
                }, offset, lookRotation);
            }

            return 1;
        }

        /// <summary>
        /// Draws a wireframe sphere at 'center' with 'radius'
        /// </summary>
        public static ShapeModifier DrawWireSphere(Vector3 center, float radius, int numEdges = 0)
            => _NewShapeModifierCount(BuildWireSphere(center, radius, numEdges));
        private static int BuildWireSphere(Vector3 center, float radius, int numEdges)
        {
            _NewShape(center, 3);
            _OpenCircle(Vector3.zero, radius, 360, 0, numEdges, false);
            _OpenCircle(Vector3.zero, Quaternion.LookRotation(Vector3.up), radius, 360, 0, numEdges, false);
            _OpenCircle(Vector3.zero, Quaternion.LookRotation(Vector3.right), radius, 360, 0, numEdges, false);

            return 1;
        }

        /// <summary>
        /// Draws an wireframe cylinder at 'center' with 'size'
        /// </summary>
        public static ShapeModifier DrawWireCylinder(Vector3 center, Vector2 size)
            => _NewShapeModifierCount(BuildWireCylinder(center, size, -2));
        private static int BuildWireCylinder(Vector3 center, Vector2 size, int numEdges)
        {
            size = size.Abs();
            float half_x = size.x / 2;
            float half_y = size.y / 2;

            _NewShape(center, 3);
            _OpenCircle(Vector3.up * half_y, Quaternion.LookRotation(Vector3.up), half_x, 360, 0, numEdges);
            _OpenCircle(-Vector3.up * half_y, Quaternion.LookRotation(-Vector3.up), half_x, 360, 0, numEdges);

            Span<Vector3> vertices = stackalloc Vector3[4 * 2];
            for (int i = 0; i < vertices.Length; i += 2)
            {
                Vector3 circleVertexTop = Vector2.right.Rotate((360 * (float)i / (float)vertices.Length)) * half_x;
                Vector3 circleVertexBottom = Vector2.right.Rotate((360 * -(float)i / (float)vertices.Length)) * half_x;
                vertices[i] = Quaternion.LookRotation(Vector3.up) * circleVertexTop + Vector3.up * half_y;
                vertices[i + 1] = Quaternion.LookRotation(Vector3.down) * circleVertexBottom + Vector3.down * half_y;
            }

            _Lines(vertices);

            return 1;
        }

        /// <summary>
        /// Draws an wireframe capsule at 'center' with 'size'
        /// </summary>
        public static ShapeModifier DrawWireCapsule(Vector3 center, Vector2 size)
            => _NewShapeModifierCount(BuildWireCapsule(center, size));
        //public static ShapeModifier DrawWireCapsule(Vector2 from, Vector2 to, float radius)
        //{
        //    Vector2 center = Vector2.Lerp(from, to, .5f);
        //    Vector2 size = new Vector2(radius * 2, Vector2.Distance(from, to) + radius * 2);
        //    float angle = Vector2.SignedAngle(Vector2.up, from - center);
        //    return _NewShapeModifierCount(BuildWireCapsule(center, size));
        //}
        private static int BuildWireCapsule(Vector3 center, Vector2 size)
        {
            size = size.Abs();
            float half_x = size.x / 2;
            float half_y = size.y / 2;
            float circleDist = Mathf.Max(half_y - half_x, 0);

            int count = circleDist > 0 ? 3 : 2;
            _NewShape(center, count);
            _OpenCircle(Vector3.up * circleDist, Quaternion.LookRotation(Vector3.up), half_x, 360, 0, -2);
            if (count == 3)
                _OpenCircle(-Vector3.up * circleDist, Quaternion.LookRotation(-Vector3.up), half_x, 360, 0, -2);

            BuildCapsuleLookRotation(center, size, Quaternion.identity, CapsuleDirection2D.Vertical, 0);
            BuildCapsuleLookRotation(center, size, Quaternion.LookRotation(Vector3.right), CapsuleDirection2D.Vertical, 0);

            return 4;
        }
        private static int BuildCapsuleLookRotation(Vector3 position, Vector2 size, Quaternion lookRotation, CapsuleDirection2D direction, float angle)
        {
            size = size.Abs();
            float radius = direction == CapsuleDirection2D.Vertical ? size.x / 2 : size.y / 2;
            float difference = direction == CapsuleDirection2D.Vertical ?
                (size.y > size.x ? (size.y - size.x) / 2 : 0) :
                (size.x > size.y ? (size.x - size.y) / 2 : 0);

            float offsetAngle = (direction == CapsuleDirection2D.Vertical ? 0 : 90) + angle;
            Vector2 curveOffsetDirection = (direction == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.left).Rotate(angle);
            Vector2 orientationSize = direction == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.right;

            _NewShape(position, 3);
            bool vertical = direction == CapsuleDirection2D.Vertical;
            _PartialOpenBox(Vector3.zero, lookRotation, (size - (radius * 2 * orientationSize)).ZeroNegatives(), angle, !vertical, !vertical, vertical, vertical);
            _OpenCircle(curveOffsetDirection * difference, lookRotation, radius, 180, offsetAngle, -2, false);
            _OpenCircle(-curveOffsetDirection * difference, lookRotation, radius, 180, 180 + offsetAngle, -2, false);

            return 1;
        }

        /// <summary>
        /// Draws a wireframe cone with 'origin', 'direction', 'radius' and 'distance'.
        /// 'rotation' rotates the polygon base (circle by default) with 'numEdges' (automatically calculated if 0)
        /// 'numLines' determines how many lines connect the origin point to the base
        /// </summary>
        public static ShapeModifier DrawWireCone(Vector3 origin, Vector3 direction, float radius, float distance, float rotation, int numEdges = 0, int numLines = 0)
            => _NewShapeModifierCount(BuildWireCone(origin, direction, radius, distance, rotation, numEdges, numLines));
        /// <summary>
        /// Draws a wireframe cone with 'origin', 'direction', 'angle' and 'distance'.
        /// 'rotation' rotates the polygon base (circle by default) with 'numEdges' (automatically calculated if 0)
        /// 'numLines' determines how many lines connect the origin point to the base
        /// </summary>
        public static ShapeModifier DrawWireConeByAngle(Vector3 origin, Vector3 direction, float angle, float distance, float rotation, int numEdges = 0, int numLines = 0)
        {
            float radius = Mathf.Tan(angle * Mathf.Deg2Rad / 2) * distance;
            return _NewShapeModifierCount(BuildWireCone(origin, direction, radius, distance, rotation, numEdges, numLines));
        }
        private static int BuildWireCone(Vector3 origin, Vector3 direction, float radius, float distance, float rotation, int numEdges, int numLines)
        {
            if (numEdges == 0 || numEdges == 1)
                numEdges = -2;

            _NewShape(origin, 2);
            _OpenCircle(direction * distance, Quaternion.LookRotation(direction), radius, 360, rotation, numEdges);

            // Get Circle Vertices
            if (numLines == 0)
            {
                numLines = 4;

                if (numEdges > 0 && numEdges <= 6)
                    numLines = numEdges;
                else if (numEdges >= 7 && numEdges <= 12)
                {
                    numLines = 4;

                    if (numEdges % 4 == 0)
                        numLines = 4;
                    else if (numEdges % 3 == 0)
                        numLines = 3;
                    else if (numEdges % 5 == 0)
                        numLines = 5;
                }

                Span<Vector3> vertices = stackalloc Vector3[numLines * 2];
                for (int i = 0; i < vertices.Length; i += 2)
                {
                    vertices[i] = Vector2.zero;

                    Vector3 circleVertex = Vector2.right.Rotate((360 * (float)i / (float)vertices.Length) + rotation) * radius;
                    vertices[i + 1] = Quaternion.LookRotation(direction) * circleVertex + direction * distance;
                }

                _Lines(vertices);
            }
            else if (numLines < 0)
            {
                if (numEdges <= 0)
                    numEdges = GetNumEdges(radius, 360, numEdges);

                int skipIncrement = Mathf.Abs(numLines);

                Span<Vector3> vertices = stackalloc Vector3[((numEdges / Mathf.Abs(numLines)) + 1) * 2];
                int index = 0;
                for (int i = 0; i < vertices.Length; i += 2)
                {
                    vertices[i] = Vector2.zero;

                    Vector3 circleVertex = Vector2.right.Rotate(360 * ((float)index / (float)numEdges) + rotation) * radius;
                    vertices[i + 1] = Quaternion.LookRotation(direction) * circleVertex + direction * distance;

                    index += Mathf.Abs(numLines);
                }

                _Lines(vertices);
            }
            else
            {
                Span<Vector3> vertices = stackalloc Vector3[numLines * 2];
                for (int i = 0; i < vertices.Length; i += 2)
                {
                    vertices[i] = Vector2.zero;

                    Vector3 circleVertex = Vector2.right.Rotate((360 * (float)i / (float)vertices.Length) + rotation) * radius;
                    vertices[i + 1] = Quaternion.LookRotation(direction) * circleVertex + direction * distance;
                }

                _Lines(vertices);
            }

            return 1;
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
                GLmat.SetInt("_ZWrite", 0); // set to 1?
                // 4 = LEqual (Less than or equal depth) 8 = Always, might be 4 by default tho idk
                //GLmat.SetInt("_ZTest", 4);
            }

            if (tmp == null)
            {
                GameObject tmpGO = new GameObject("GLGizmos_TMP_Reference");
                tmpGO.hideFlags = HideFlags.HideAndDontSave;
                tmp = tmpGO.AddComponent<TextMeshPro>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSizeMin = 0;
                tmp.renderer.enabled = false;
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
            int unitConvert = (int)unitsFrom * 10 + (int)unitsTo;
            switch (unitConvert)
            {
                case 01: /*01 Degrees -> Radians*/ return Mathf.Deg2Rad * value;
                case 10: /*10 Radians -> Degrees*/ return Mathf.Rad2Deg * value;
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
        public static Vector3 Rotate(this Vector3 v, float angle, Vector3 axis, AngleUnits units = AngleUnits.Degrees)
        {
            angle = AngleUnitConversion(angle, units, AngleUnits.Degrees);

            // Create a rotation object
            Quaternion rotation = Quaternion.AngleAxis(angle, axis);

            // Multiplying a Quaternion by a Vector3 rotates that vector
            return rotation * v;
        }

        public static Vector2 ScaleEach(this Vector2 v, float scaleX, float scaleY) => new Vector2(v.x * scaleX, v.y * scaleY);
        public static Vector2 ScaleEach(this Vector2 v, Vector2 scaleXY) => new Vector2(v.x * scaleXY.x, v.y * scaleXY.y);
        public static Vector2 ZeroNegatives(this Vector2 v) => new Vector2(v.x > 0 ? v.x : 0, v.y > 0 ? v.y : 0);
        public static Vector2 Abs(this Vector3 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        public static Vector2 Abs(this Vector2 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        public static float Max(this Vector2 v) => Mathf.Max(v.x, v.y);
        public static float PositiveAngle(this float f) => f < 0 ? f + 360 : f;

        #region AI Generated Helpers
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

        // Entry point: This is what you call: myList.HybridSort();
        public static void HybridSort<T>(this List<T> list) where T : IComparable<T>
        {
            if (list == null || list.Count <= 1) return;
            HybridSort(list, 0, list.Count - 1);
        }

        // Recursive logic
        public static void HybridSort<T>(this List<T> list, int left, int right) where T : IComparable<T>
        {
            int Threshold = 24;

            if (left >= right) return;

            if (right - left <= Threshold)
            {
                InsertionSort(list, left, right);
                return;
            }

            T pivot = list[(left + right) / 2];
            int i = left - 1;
            int j = right + 1;

            while (true)
            {
                do { i++; } while (list[i].CompareTo(pivot) < 0);
                do { j--; } while (list[j].CompareTo(pivot) > 0);

                if (i >= j) break;

                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }

            list.HybridSort(left, j);
            list.HybridSort(j + 1, right);
        }

        private static void InsertionSort<T>(List<T> list, int left, int right) where T : IComparable<T>
        {
            for (int i = left + 1; i <= right; i++)
            {
                T key = list[i];
                int j = i - 1;
                while (j >= left && list[j].CompareTo(key) > 0)
                {
                    list[j + 1] = list[j];
                    j--;
                }
                list[j + 1] = key;
            }
        }
        #endregion
    }
}

