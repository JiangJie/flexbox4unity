#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Flexbox
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FlexContainer))]
    public sealed class FlexContainerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var flexContainer = target as FlexContainer;

            if (GUILayout.Button("更新布局"))
            {
                flexContainer.ForceDoLayout();
            }
        }
    }
}
#endif
