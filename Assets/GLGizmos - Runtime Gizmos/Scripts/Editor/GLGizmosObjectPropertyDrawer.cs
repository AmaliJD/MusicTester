using UnityEngine;
using UnityEditor;
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

            SerializedProperty ObjectPositionProperty1 = property.FindPropertyRelative("ObjectPosition1");
            SerializedProperty ObjectPositionProperty2 = property.FindPropertyRelative("ObjectPosition2");

            if (!initialized)
            {
                if (firstElement)
                    InitializeFirstElement(property, ObjectPositionProperty1, ObjectPositionProperty2);
                else
                    InitializeNewElement(property);
            }

            if (property.isExpanded)
            {
                SerializedProperty gizmoTypeProperty = property.FindPropertyRelative("gizmoType");
                EditorGUILayout.PropertyField(gizmoTypeProperty, new GUIContent("Gizmo Type"));
                GizmoType gizmoType = (GizmoType)gizmoTypeProperty.enumValueIndex;

                SerializedProperty spaceProperty1 = ObjectPositionProperty1.FindPropertyRelative("space");
                SerializedProperty spaceProperty2 = ObjectPositionProperty2.FindPropertyRelative("space");
                SerializedProperty positionTypeProperty1 = ObjectPositionProperty1.FindPropertyRelative("type");
                SerializedProperty positionTypeProperty2 = ObjectPositionProperty2.FindPropertyRelative("type");

                SerializedProperty colliderToFromProperty = property.FindPropertyRelative("useToFromPositions");

                // LOCAL SPACE
                EditorGUILayout.Space();
                bool useObjectPosition1 = gizmoType != GizmoType.Collider;
                bool useObjectPosition2 = gizmoType == GizmoType.Line || (gizmoType == GizmoType.Capsule && colliderToFromProperty.boolValue);

                if (gizmoType == GizmoType.Capsule)
                {
                    EditorGUILayout.PropertyField(colliderToFromProperty, new GUIContent("Use Position Anchors"));
                    EditorGUILayout.Space();
                }

                if (useObjectPosition1)
                    EditorGUILayout.PropertyField(spaceProperty1, new GUIContent("Local Space"));
                if (useObjectPosition2)
                    EditorGUILayout.PropertyField(spaceProperty2, new GUIContent("Local Space 2"));

                // TRANSFORM
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
                if (useObjectPosition1)
                    DrawTransformSection(property, ObjectPositionProperty1, useObjectPosition2 ? 1 : 0);
                if (useObjectPosition2)
                    DrawTransformSection(property, ObjectPositionProperty2, 2);


                LocalSpace space1 = (LocalSpace)spaceProperty1.enumValueFlag;
                LocalSpace space2 = (LocalSpace)spaceProperty2.enumValueFlag;
                // OBJECT
                EditorGUILayout.Space();
                if (gizmoType == GizmoType.Box)
                {
                    DrawScaleSection(property, ObjectPositionProperty1, "size", "Size", "Size");

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("rotation"), new GUIContent("Rotation"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Box Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("solid"), new GUIContent("Solid"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("hideBox"), new GUIContent("Hide Inner Box"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("roundCorners01"), new GUIContent("Round Corners"));

                    EditorGUILayout.Space();
                    DrawBorderSection(property);
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("solidBorder"), new GUIContent("Solid Border"));
                }
                else if (gizmoType == GizmoType.Circle)
                {
                    DrawScaleSection(property, ObjectPositionProperty1, "radius", "Radius", "Radius");

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("rotation"), new GUIContent("Rotation"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Arc Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("arcAngle"), new GUIContent("Arc Angle"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("arcCloseType"), new GUIContent("Arc Close"));

                    EditorGUILayout.PropertyField(property.FindPropertyRelative("numEdges"), new GUIContent("numEdges"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("dashed"), new GUIContent("Dashed"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("solid"), new GUIContent("Solid"));

                    EditorGUILayout.Space();
                    DrawBorderSection(property);
                }
                else if (gizmoType == GizmoType.Line)
                {
                    EditorGUILayout.LabelField("Line Properties", EditorStyles.boldLabel);

                    SerializedProperty lineTypeProperty = property.FindPropertyRelative("lineType");
                    EditorGUILayout.PropertyField(lineTypeProperty, new GUIContent("Line Type"));
                    LineType lineType = (LineType)lineTypeProperty.enumValueIndex;

                    switch (lineType)
                    {
                        case LineType.Solid:
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("weight"), new GUIContent("Line Weight"));
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("roundedTips"), new GUIContent("Rounded Tips"));
                            break;
                        case LineType.Bezier:
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("bezierCurve"), new GUIContent("Curve Strength"));
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("numEdges"), new GUIContent("numEdges"));
                            break;
                        case LineType.Dashed:
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("weight"), new GUIContent("Line Weight"));
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("dashLength"), new GUIContent("Dash Length"));
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("gapSize"), new GUIContent("Gap Size"));
                            break;
                        case LineType.Dotted:
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("weight"), new GUIContent("Line Weight"));
                            EditorGUILayout.PropertyField(property.FindPropertyRelative("gapSize"), new GUIContent("Gap Size"));
                            break;
                    }
                }
                else if (gizmoType == GizmoType.Capsule)
                {
                    if (colliderToFromProperty.boolValue)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Capsule Properties", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("radius"), new GUIContent("Radius"));
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("solid"), new GUIContent("Solid"));

                        EditorGUILayout.Space();
                        DrawBorderSection(property);
                    }
                    else
                    {
                        EditorGUILayout.Space();
                        DrawScaleSection(property, ObjectPositionProperty1, "size", "Size", "Size");

                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("rotation"), new GUIContent("Rotation"));

                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Capsule Properties", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("capsuleDirection"), new GUIContent("Direction"));
                        EditorGUILayout.PropertyField(property.FindPropertyRelative("solid"), new GUIContent("Solid"));

                        EditorGUILayout.Space();
                        DrawBorderSection(property);
                    }
                }
                else if (gizmoType == GizmoType.Triangle)
                {
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("centerOffset"), new GUIContent("Center Offset"));

                    EditorGUILayout.Space();
                    DrawScaleSection(property, ObjectPositionProperty1, "size", "Size", "Width & Height");

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("rotation"), new GUIContent("Rotation"));

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Triangle Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("skew"), new GUIContent("Skew"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("solid"), new GUIContent("Solid"));

                    EditorGUILayout.Space();
                    DrawBorderSection(property);
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
                    EditorGUILayout.Space();
                    DrawScaleSection(property, ObjectPositionProperty1, "size", "Size", "Size");

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("rotation"), new GUIContent("Rotation"));

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

                EditorGUILayout.Space();
                DrawSettingsSection(property);
            }
        }

        void InitializeFirstElement(SerializedProperty property, SerializedProperty ObjectPositionProperty1, SerializedProperty ObjectPositionProperty2)
        {
            ObjectPositionProperty1.FindPropertyRelative("space").enumValueFlag = 7;
            ObjectPositionProperty2.FindPropertyRelative("space").enumValueFlag = 7;

            property.FindPropertyRelative("size").vector2Value = Vector2.one;
            property.FindPropertyRelative("radius").floatValue = 0.5f;
            property.FindPropertyRelative("borderType").enumValueIndex = (int)BorderType.Outside;
            property.FindPropertyRelative("solidBorder").boolValue = true;
            property.FindPropertyRelative("color").colorValue = Color.white;
            property.FindPropertyRelative("inheritLayer").boolValue = true;

            property.FindPropertyRelative("arcAngle").floatValue = 360;
            property.FindPropertyRelative("bezierCurve").floatValue = .75f;

            property.FindPropertyRelative("dashLength").floatValue = 1;
            property.FindPropertyRelative("gapSize").floatValue = .5f;

            property.FindPropertyRelative("text").stringValue = "Text";
            property.FindPropertyRelative("fontSize").floatValue = 5f;
            property.FindPropertyRelative("textBoxColor").colorValue = new Color(.5f, 1, 0, .25f);
            property.FindPropertyRelative("textAlignment").enumValueFlag = (int)TextAlignmentOptions.Center;
        }

        void InitializeNewElement(SerializedProperty property)
        {
            property.FindPropertyRelative("inheritColor").boolValue = true;
            property.FindPropertyRelative("inheritLayer").boolValue = true;

            if ((GizmoType)property.FindPropertyRelative("gizmoType").enumValueIndex != GizmoType.Text)
            {
                property.FindPropertyRelative("text").stringValue = "Text";
            }
        }

        void DrawTransformSection(SerializedProperty property, SerializedProperty ObjectPositionProperty, int number)
        {
            string num = number > 0 ? $" {number.ToString()}" : "";

            SerializedProperty positionTypeProperty = ObjectPositionProperty.FindPropertyRelative("type");
            EditorGUILayout.PropertyField(positionTypeProperty, new GUIContent($"Position{num} Type"));

            SerializedProperty positionOffsetProperty = ObjectPositionProperty.FindPropertyRelative("offset");
            switch ((PositionType)positionTypeProperty.enumValueIndex)
            {
                case PositionType.Transform:
                    SerializedProperty positionTransformProperty = property.FindPropertyRelative("transform");
                    EditorGUILayout.PropertyField(positionTransformProperty, new GUIContent($"Target"));
                    EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent($"Offset{num}"));
                    break;
                case PositionType.Raw:
                    EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent($"Position{num}"));
                    break;
                case PositionType.This:
                    EditorGUILayout.PropertyField(positionOffsetProperty, new GUIContent($"Offset{num}"));
                    break;
            }
        }

        void DrawScaleSection(SerializedProperty property, SerializedProperty ObjectPositionProperty, string PropertyName, string FieldName, string DefaultName)
        {
            SerializedProperty sizeProperty = property.FindPropertyRelative(PropertyName);

            if (((LocalSpace)ObjectPositionProperty.FindPropertyRelative("space").enumValueFlag).HasFlag(LocalSpace.Scale))
            {
                SerializedProperty sizeTypeProperty = property.FindPropertyRelative("scaleSizeType");
                EditorGUILayout.PropertyField(sizeTypeProperty, new GUIContent("Size Type"));
                ScaleSizeType sizeType = (ScaleSizeType)sizeTypeProperty.enumValueIndex;

                if (sizeType == ScaleSizeType.Add)
                    EditorGUILayout.PropertyField(sizeProperty, new GUIContent($"{FieldName} (add)"));
                else
                    EditorGUILayout.PropertyField(sizeProperty, new GUIContent($"{FieldName} (multiply)"));
            }
            else
                EditorGUILayout.PropertyField(sizeProperty, new GUIContent(DefaultName));
        }

        void DrawBorderSection(SerializedProperty property)
        {
            EditorGUILayout.LabelField("Border", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("weight"), new GUIContent("Border Width"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("borderType"), new GUIContent("Border Type"));
        }

        void DrawSettingsSection(SerializedProperty property)
        {
            EditorGUILayout.LabelField("Draw Settings", EditorStyles.boldLabel);
            SerializedProperty inheritColorProperty = property.FindPropertyRelative("inheritColor");
            bool inheritColor = inheritColorProperty.boolValue;
            if (!inheritColor)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("color"), new GUIContent("Color"));
            }
            EditorGUILayout.PropertyField(inheritColorProperty, new GUIContent("Inherit Color"));

            // layer
            EditorGUILayout.Space();
            SerializedProperty inheritLayerProperty = property.FindPropertyRelative("inheritLayer");

            bool inheritLayer = inheritLayerProperty.boolValue;
            if (!inheritLayer)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("layer"), new GUIContent("Layer"));
            }
            EditorGUILayout.PropertyField(inheritLayerProperty, new GUIContent("Inherit Layer"));

            //disable
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(property.FindPropertyRelative("disable"), new GUIContent("disable"));
        }
    }
}
