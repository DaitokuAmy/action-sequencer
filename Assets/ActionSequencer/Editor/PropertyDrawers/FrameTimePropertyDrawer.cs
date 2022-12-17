using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor.PropertyDrawers
{
    /// <summary>
    /// FrameTimeAttribute用のPropertyDrawer
    /// </summary>
    [CustomPropertyDrawer(typeof(FrameTimeAttribute))]
    public class FrameTimePropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// GUI描画処理
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            void DrawFrameField(int frameRate)
            {
                var val = Mathf.RoundToInt(property.floatValue * frameRate);
                var frameLabel = new GUIContent(label);
                frameLabel.text = $"{ObjectNames.NicifyVariableName(((FrameTimeAttribute)attribute).FrameLabel)}({frameRate})";
                EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    val = EditorGUI.IntField(position, frameLabel, val);
                    if (scope.changed)
                    {
                        property.floatValue = val * (1.0f / frameRate);
                    }
                }
                EditorGUI.showMixedValue = false;
            }

            if (property.serializedObject.targetObject is SequenceClip clip) {
                if (clip.frameRate < 0) {
                    EditorGUI.PropertyField(position, property, label);
                }
                else {
                    DrawFrameField(clip.frameRate);
                }
            }
            else {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}