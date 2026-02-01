using UnityEngine;
using UnityEditor;
using GLGizmosExtensions;
using TMPro;

namespace GLDebug
{
    using static GLGizmosObject;

    [CustomPropertyDrawer(typeof(GLGizmosObject), true)]
    public class GLGizmosObjectPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string index = label.text.Substring(label.text.IndexOf(" ") + 1);
            label.text = $"Gizmo Object {index}";
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label,
                true
            );

            bool firstElement = index == "0";
            SerializedProperty initializedProperty = property.FindPropertyRelative("initialized");
            bool initialized = initializedProperty.boolValue;
            initializedProperty.boolValue = true;

            if (!initialized && firstElement)
            {
                SerializedProperty spaceProperty = property.FindPropertyRelative("space");
                spaceProperty.enumValueFlag = 7;

                SerializedProperty space2Property = property.FindPropertyRelative("space2");
                space2Property.enumValueFlag = 7;

                SerializedProperty sizeProperty = property.FindPropertyRelative("size");
                sizeProperty.vector2Value = Vector2.one;

                SerializedProperty borderTypeProperty = property.FindPropertyRelative("borderType");
                borderTypeProperty.enumValueIndex = (int)BorderType.Outside;

                SerializedProperty colorProperty = property.FindPropertyRelative("color");
                colorProperty.colorValue = Color.white;

                SerializedProperty inheritLayerProperty = property.FindPropertyRelative("inheritLayer");
                inheritLayerProperty.boolValue = true;

                SerializedProperty radiusProperty = property.FindPropertyRelative("radius");
                radiusProperty.floatValue = 0.5f;

                SerializedProperty arcAngleProperty = property.FindPropertyRelative("arcAngle");
                arcAngleProperty.floatValue = 360;

                SerializedProperty bezierCurveProperty = property.FindPropertyRelative("bezierCurve");
                bezierCurveProperty.floatValue = .75f;

                SerializedProperty dashLengthProperty = property.FindPropertyRelative("dashLength");
                dashLengthProperty.floatValue = 1;

                SerializedProperty gapSizeProperty = property.FindPropertyRelative("gapSize");
                gapSizeProperty.floatValue = .5f;

                SerializedProperty fontSizeProperty = property.FindPropertyRelative("fontSize");
                fontSizeProperty.floatValue = 5f;

                SerializedProperty textBoxColorProperty = property.FindPropertyRelative("textBoxColor");
                textBoxColorProperty.colorValue = new Color(0, 1, 0, .5f);

                SerializedProperty textAlignmentProperty = property.FindPropertyRelative("textAlignment");
                textAlignmentProperty.enumValueFlag = (int)TextAlignmentOptions.Center;
            }

            if (property.isExpanded)
            {
                SerializedProperty gizmoTypeProperty = property.FindPropertyRelative("gizmoType");
                EditorGUILayout.PropertyField(gizmoTypeProperty, new GUIContent("Gizmo Type"));
                GizmoType gizmoType = (GizmoType)gizmoTypeProperty.enumValueIndex;

                SerializedProperty spaceProperty = property.FindPropertyRelative("space");
                SerializedProperty space2Property = property.FindPropertyRelative("space2");
                SerializedProperty positionTypeProperty = property.FindPropertyRelative("positionType");
                SerializedProperty positionType2Property = property.FindPropertyRelative("positionType2");

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spaceProperty, new GUIContent("Local Space"));
                LocalSpace space = (LocalSpace)spaceProperty.enumValueFlag;

                LocalSpace space2;
                if (gizmoType == GizmoType.Line)
                {
                    EditorGUILayout.PropertyField(space2Property, new GUIContent("Local Space 2"));
                    space2 = (LocalSpace)space2Property.enumValueFlag;
                }

                EditorGUILayout.Space();
                if (gizmoType == GizmoType.Box)
                {
                    EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(positionTypeProperty, new GUIContent("Position Type"));
                    PositionType positionType = (PositionType)positionTypeProperty.enumValueIndex;

                    SerializedProperty positionOffsetProperty = property.FindPropertyRelative("positionOffset");
                    switch (positionType)
                    {
                        case PositionType.Transform:
                            SerializedProperty positionTransformProperty = property.FindPropertyRelative("positionTransform");
                            EditorGUILayout.PropertyField(positionTransformProperty, new GUIContent("Target"));
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Offset"));
                            break;
                        case PositionType.Raw:
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Position"));
                            break;
                        case PositionType.This:
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Offset"));
                            break;
                    }

                    EditorGUILayout.Space();
                    SerializedProperty sizeProperty = property.FindPropertyRelative("size");

                    if (space.FlagEnumContains(LocalSpace.Scale))
                    {
                        SerializedProperty sizeTypeProperty = property.FindPropertyRelative("scaleSizeType");
                        EditorGUILayout.PropertyField(sizeTypeProperty, new GUIContent("Size Type"));
                        ScaleSizeType sizeType = (ScaleSizeType)sizeTypeProperty.enumValueIndex;

                        if (sizeType == ScaleSizeType.Add)
                            EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Size (add)"));
                        else
                            EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Size (multiply)"));
                    }
                    else
                        EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Size"));

                    EditorGUILayout.Space();
                    SerializedProperty angleProperty = property.FindPropertyRelative("angle");
                    EditorGUILayout.PropertyField(angleProperty, new GUIContent("Rotation"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Box Properties", EditorStyles.boldLabel);
                    SerializedProperty solidProperty = property.FindPropertyRelative("solid");
                    EditorGUILayout.PropertyField(solidProperty, new GUIContent("Solid"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Border", EditorStyles.boldLabel);
                    SerializedProperty weightProperty = property.FindPropertyRelative("weight");
                    EditorGUILayout.PropertyField(weightProperty, new GUIContent("Border Width"));
                    SerializedProperty borderTypeProperty = property.FindPropertyRelative("borderType");
                    EditorGUILayout.PropertyField(borderTypeProperty, new GUIContent("Border Type"));

                    SerializedProperty edgeRadiusProperty = property.FindPropertyRelative("edgeRadius");
                    EditorGUILayout.PropertyField(edgeRadiusProperty, new GUIContent("Edge Radius"));
                    SerializedProperty solidEdgeRadiusProperty = property.FindPropertyRelative("solidEdgeRadius");
                    EditorGUILayout.PropertyField(solidEdgeRadiusProperty, new GUIContent("Solid Edge Radius"));
                    SerializedProperty cutOutBoxProperty = property.FindPropertyRelative("cutOutBox");
                    EditorGUILayout.PropertyField(cutOutBoxProperty, new GUIContent("Render Only  Edge Radius"));
                }
                else if (gizmoType == GizmoType.Circle)
                {
                    EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(positionTypeProperty, new GUIContent("Position Type"));
                    PositionType positionType = (PositionType)positionTypeProperty.enumValueIndex;

                    SerializedProperty positionOffsetProperty = property.FindPropertyRelative("positionOffset");
                    switch (positionType)
                    {
                        case PositionType.Transform:
                            SerializedProperty positionTransformProperty = property.FindPropertyRelative("positionTransform");
                            EditorGUILayout.PropertyField(positionTransformProperty, new GUIContent("Target"));
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Offset"));
                            break;
                        case PositionType.Raw:
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Position"));
                            break;
                        case PositionType.This:
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Offset"));
                            break;
                    }

                    EditorGUILayout.Space();
                    SerializedProperty radiusProperty = property.FindPropertyRelative("radius");

                    if (space.FlagEnumContains(LocalSpace.Scale))
                    {
                        SerializedProperty sizeTypeProperty = property.FindPropertyRelative("scaleSizeType");
                        EditorGUILayout.PropertyField(sizeTypeProperty, new GUIContent("Size Type"));
                        ScaleSizeType sizeType = (ScaleSizeType)sizeTypeProperty.enumValueIndex;

                        if (sizeType == ScaleSizeType.Add)
                            EditorGUILayout.PropertyField(radiusProperty, new GUIContent("Radius (add)"));
                        else
                            EditorGUILayout.PropertyField(radiusProperty, new GUIContent("Radius (multiply)"));
                    }
                    else
                        EditorGUILayout.PropertyField(radiusProperty, new GUIContent("Radius"));

                    EditorGUILayout.Space();
                    SerializedProperty angleProperty = property.FindPropertyRelative("angle");
                    EditorGUILayout.PropertyField(angleProperty, new GUIContent("Rotation"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Arc Properties", EditorStyles.boldLabel);

                    SerializedProperty arcAngleProperty = property.FindPropertyRelative("arcAngle");
                    EditorGUILayout.PropertyField(arcAngleProperty, new GUIContent("Arc Angle"));
                    SerializedProperty arcCloseProperty = property.FindPropertyRelative("arcCloseType");
                    EditorGUILayout.PropertyField(arcCloseProperty, new GUIContent("Arc Close"));

                    SerializedProperty numEdgesProperty = property.FindPropertyRelative("numEdges");
                    EditorGUILayout.PropertyField(numEdgesProperty, new GUIContent("numEdges"));
                    SerializedProperty solidProperty = property.FindPropertyRelative("solid");
                    EditorGUILayout.PropertyField(solidProperty, new GUIContent("Solid"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Border", EditorStyles.boldLabel);
                    SerializedProperty weightProperty = property.FindPropertyRelative("weight");
                    EditorGUILayout.PropertyField(weightProperty, new GUIContent("Border Width"));
                    SerializedProperty borderTypeProperty = property.FindPropertyRelative("borderType");
                    EditorGUILayout.PropertyField(borderTypeProperty, new GUIContent("Border Type"));
                }
                else if (gizmoType == GizmoType.Line)
                {
                    EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(positionTypeProperty, new GUIContent("Position Type"));
                    PositionType positionType = (PositionType)positionTypeProperty.enumValueIndex;

                    SerializedProperty positionOffsetProperty = property.FindPropertyRelative("positionOffset");
                    switch (positionType)
                    {
                        case PositionType.Transform:
                            SerializedProperty positionTransformProperty = property.FindPropertyRelative("positionTransform");
                            EditorGUILayout.PropertyField(positionTransformProperty, new GUIContent("Target"));
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Offset"));
                            break;
                        case PositionType.Raw:
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Position"));
                            break;
                        case PositionType.This:
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Offset"));
                            break;
                    }

                    EditorGUILayout.PropertyField(positionType2Property, new GUIContent("Position 2 Type"));
                    PositionType positionType2 = (PositionType)positionType2Property.enumValueIndex;

                    SerializedProperty positionOffset2Property = property.FindPropertyRelative("positionOffset2");
                    switch (positionType2)
                    {
                        case PositionType.Transform:
                            SerializedProperty positionTransform2Property = property.FindPropertyRelative("positionTransform2");
                            EditorGUILayout.PropertyField(positionTransform2Property, new GUIContent("Target 2"));
                            EditorGUILayout.PropertyField(positionOffset2Property, new GUIContent("Offset 2"));
                            break;
                        case PositionType.Raw:
                            EditorGUILayout.PropertyField(positionOffset2Property, new GUIContent("Position 2"));
                            break;
                        case PositionType.This:
                            EditorGUILayout.PropertyField(positionOffset2Property, new GUIContent("Offset 2"));
                            break;
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Line Properties", EditorStyles.boldLabel);

                    SerializedProperty lineTypeProperty = property.FindPropertyRelative("lineType");
                    EditorGUILayout.PropertyField(lineTypeProperty, new GUIContent("Line Type"));
                    LineType lineType = (LineType)lineTypeProperty.enumValueIndex;

                    switch (lineType)
                    {
                        case LineType.Solid:
                            SerializedProperty weightProperty = property.FindPropertyRelative("weight");
                            EditorGUILayout.PropertyField(weightProperty, new GUIContent("Line Weight"));
                            break;
                        case LineType.Bezier:
                            SerializedProperty bezierCurveProperty = property.FindPropertyRelative("bezierCurve");
                            EditorGUILayout.PropertyField(bezierCurveProperty, new GUIContent("Curve Strength"));
                            SerializedProperty numEdgesProperty = property.FindPropertyRelative("numEdges");
                            EditorGUILayout.PropertyField(numEdgesProperty, new GUIContent("numEdges"));
                            break;
                        case LineType.Dashed:
                            SerializedProperty dashLengthProperty = property.FindPropertyRelative("dashLength");
                            EditorGUILayout.PropertyField(dashLengthProperty, new GUIContent("Dash Length"));
                            SerializedProperty gapSizeProperty = property.FindPropertyRelative("gapSize");
                            EditorGUILayout.PropertyField(gapSizeProperty, new GUIContent("Gap Size"));
                            break;
                    }
                }
                else if (gizmoType == GizmoType.Triangle)
                {
                    EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(positionTypeProperty, new GUIContent("Position Type"));
                    PositionType positionType = (PositionType)positionTypeProperty.enumValueIndex;

                    SerializedProperty positionOffsetProperty = property.FindPropertyRelative("positionOffset");
                    switch (positionType)
                    {
                        case PositionType.Transform:
                            SerializedProperty positionTransformProperty = property.FindPropertyRelative("positionTransform");
                            EditorGUILayout.PropertyField(positionTransformProperty, new GUIContent("Target"));
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Offset"));
                            break;
                        case PositionType.Raw:
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Position"));
                            break;
                        case PositionType.This:
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Offset"));
                            break;
                    }

                    SerializedProperty centerOffsetProperty = property.FindPropertyRelative("centerOffset");
                    EditorGUILayout.PropertyField(centerOffsetProperty, new GUIContent("Center Offset"));

                    EditorGUILayout.Space();
                    SerializedProperty sizeProperty = property.FindPropertyRelative("size");

                    if (space.FlagEnumContains(LocalSpace.Scale))
                    {
                        SerializedProperty sizeTypeProperty = property.FindPropertyRelative("scaleSizeType");
                        EditorGUILayout.PropertyField(sizeTypeProperty, new GUIContent("Size Type"));
                        ScaleSizeType sizeType = (ScaleSizeType)sizeTypeProperty.enumValueIndex;

                        if (sizeType == ScaleSizeType.Add)
                            EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Size (add)"));
                        else
                            EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Size (multiply)"));
                    }
                    else
                        EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Width & Height"));

                    EditorGUILayout.Space();
                    SerializedProperty angleProperty = property.FindPropertyRelative("angle");
                    EditorGUILayout.PropertyField(angleProperty, new GUIContent("Rotation"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Triangle Properties", EditorStyles.boldLabel);
                    SerializedProperty skewProperty = property.FindPropertyRelative("skew");
                    EditorGUILayout.PropertyField(skewProperty, new GUIContent("Skew"));
                    SerializedProperty solidProperty = property.FindPropertyRelative("solid");
                    EditorGUILayout.PropertyField(solidProperty, new GUIContent("Solid"));
                }
                else if (gizmoType == GizmoType.Collider)
                {
                    SerializedProperty colliderProperty = property.FindPropertyRelative("collider2D");
                    EditorGUILayout.PropertyField(colliderProperty, new GUIContent("Collider2D"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Collider Properties", EditorStyles.boldLabel);
                    SerializedProperty solidProperty = property.FindPropertyRelative("solid");
                    EditorGUILayout.PropertyField(solidProperty, new GUIContent("Solid"));
                }
                else if (gizmoType == GizmoType.Text)
                {
                    EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(positionTypeProperty, new GUIContent("Position Type"));
                    PositionType positionType = (PositionType)positionTypeProperty.enumValueIndex;

                    SerializedProperty positionOffsetProperty = property.FindPropertyRelative("positionOffset");
                    switch (positionType)
                    {
                        case PositionType.Transform:
                            SerializedProperty positionTransformProperty = property.FindPropertyRelative("positionTransform");
                            EditorGUILayout.PropertyField(positionTransformProperty, new GUIContent("Target"));
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Offset"));
                            break;
                        case PositionType.Raw:
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Position"));
                            break;
                        case PositionType.This:
                            EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent("Offset"));
                            break;
                    }

                    EditorGUILayout.Space();
                    SerializedProperty sizeProperty = property.FindPropertyRelative("size");

                    if (space.FlagEnumContains(LocalSpace.Scale))
                    {
                        SerializedProperty sizeTypeProperty = property.FindPropertyRelative("scaleSizeType");
                        EditorGUILayout.PropertyField(sizeTypeProperty, new GUIContent("Size Type"));
                        ScaleSizeType sizeType = (ScaleSizeType)sizeTypeProperty.enumValueIndex;

                        if (sizeType == ScaleSizeType.Add)
                            EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Size (add)"));
                        else
                            EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Size (multiply)"));
                    }
                    else
                        EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Size"));

                    EditorGUILayout.Space();
                    SerializedProperty angleProperty = property.FindPropertyRelative("angle");
                    EditorGUILayout.PropertyField(angleProperty, new GUIContent("Rotation"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Text Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("text"), new GUIContent("Text"));
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("font"), new GUIContent("Font"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("fontSize"), new GUIContent("Font Size"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("autoSize"), new GUIContent("Auto Size"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("fontStyle"), new GUIContent("Font Style"));

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("characterSpacing"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("wordSpacing"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("lineSpacing"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("paragraphSpacing"));

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("textAlignment"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("positionPivot"), new GUIContent("Pivot"));

                    EditorGUILayout.Space();
                    SerializedProperty showTextBoxProperty = property.FindPropertyRelative("showTextBox");
                    EditorGUILayout.PropertyField(showTextBoxProperty);
                    if (showTextBoxProperty.boolValue)
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("textBoxColor"));
                }

                // color
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Draw Settings", EditorStyles.boldLabel);
                SerializedProperty inheritColorProperty = property.FindPropertyRelative("inheritColor");
                bool inheritColor = (bool)inheritColorProperty.boolValue;
                if (!inheritColor)
                {
                    SerializedProperty colorProperty = property.FindPropertyRelative("color");
                    EditorGUILayout.PropertyField(colorProperty, new GUIContent("Color"));
                }
                EditorGUILayout.PropertyField(inheritColorProperty, new GUIContent("Inherit Color"));

                // layer
                EditorGUILayout.Space();
                SerializedProperty inheritLayerProperty = property.FindPropertyRelative("inheritLayer");

                bool inheritLayer = (bool)inheritLayerProperty.boolValue;
                if (!inheritLayer)
                {
                    SerializedProperty layerProperty = property.FindPropertyRelative("layer");
                    EditorGUILayout.PropertyField(layerProperty, new GUIContent("Layer"));
                }
                EditorGUILayout.PropertyField(inheritLayerProperty, new GUIContent("Inherit Layer"));

                //disable
                EditorGUILayout.PropertyField(property.FindPropertyRelative("disable"), new GUIContent("disable"));
            }
        }
    }
}
