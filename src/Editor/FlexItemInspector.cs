#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Flexbox
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FlexItem))]
    public sealed class FlexItemInspector : Editor
    {
        SerializedProperty m_FlexGrow;
        SerializedProperty m_FlexShrink;
        SerializedProperty m_FlexBasis;
        SerializedProperty m_MinSize;
        SerializedProperty m_MaxSize;
        SerializedProperty m_AlignSelf;
        SerializedProperty m_Order;

        private Vector2 CreateDefaultVector2Value(Vector2 initValue)
        {
            var transform = (target as FlexItem)?.transform;
            FlexContainer container;
            if (transform != null && transform.parent != null && transform.parent.TryGetComponent(out container))
            {
                // 根据主轴方向，不修改交叉轴的值
                if (container.FlexDirection == FlexDirection.Row || container.FlexDirection == FlexDirection.RowReverse)
                {
                    return new Vector2(initValue[0], -1);
                }
                else if (container.FlexDirection == FlexDirection.Column || container.FlexDirection == FlexDirection.ColumnReverse)
                {
                    return new Vector2(-1, initValue[1]);
                }
            }

            return initValue;
        }

        private void OnEnable()
        {
            m_FlexGrow = serializedObject.FindProperty("m_FlexGrow");
            m_FlexShrink = serializedObject.FindProperty("m_FlexShrink");
            m_FlexBasis = serializedObject.FindProperty("m_FlexBasis");
            m_MinSize = serializedObject.FindProperty("m_MinSize");
            m_MaxSize = serializedObject.FindProperty("m_MaxSize");
            m_AlignSelf = serializedObject.FindProperty("m_AlignSelf");
            m_Order = serializedObject.FindProperty("m_Order");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_FlexGrow);
            EditorGUILayout.PropertyField(m_FlexShrink);
            LayoutElementField(m_FlexBasis, x => CreateDefaultVector2Value(x.sizeDelta));
            LayoutElementField(m_MinSize, CreateDefaultVector2Value(Vector2.zero));
            LayoutElementField(m_MaxSize, x => CreateDefaultVector2Value(x.sizeDelta));
            EditorGUILayout.PropertyField(m_AlignSelf);
            EditorGUILayout.PropertyField(m_Order);

            serializedObject.ApplyModifiedProperties();
        }

        private void LayoutElementField(SerializedProperty property, Vector2 defaultValue)
        {
            LayoutElementField(property, _ => defaultValue);
        }

        private void LayoutElementField(SerializedProperty property, System.Func<RectTransform, Vector2> defaultValue)
        {
            var position = EditorGUILayout.GetControlRect();

            // Label
            var label = EditorGUI.BeginProperty(position, null, property);

            // Rects
            var fieldPosition = EditorGUI.PrefixLabel(position, label);

            var toggleRect = fieldPosition;
            toggleRect.width = 16;

            var fieldRect = fieldPosition;
            fieldRect.xMin += 16;

            // Checkbox
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUI.ToggleLeft(toggleRect, GUIContent.none, property.vector2Value[0] >= 0 || property.vector2Value[1] >= 0);
            if (EditorGUI.EndChangeCheck())
            {
                // This could be made better to set all of the targets to their initial width, but mimizing code change for now
                property.vector2Value = enabled ? defaultValue((target as FlexItem)?.transform as RectTransform) : new Vector2(-1, -1);
            }

            if (!property.hasMultipleDifferentValues && (property.vector2Value[0] >= 0 || property.vector2Value[1] >= 0))
            {
                EditorGUIUtility.labelWidth = 4; // Small invisible label area for drag zone functionality
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUI.Vector2Field(fieldRect, new GUIContent(" "), property.vector2Value);
                if (EditorGUI.EndChangeCheck())
                {
                    property.vector2Value = newValue;
                }
                EditorGUIUtility.labelWidth = 0;
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
