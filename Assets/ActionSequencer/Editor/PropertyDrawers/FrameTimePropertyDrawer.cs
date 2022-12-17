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
            
            switch (SequenceEditorGUI.TimeMode)
            {
                case SequenceEditorModel.TimeMode.Seconds:
                    EditorGUI.PropertyField(position, property, label);
                    break;
                case SequenceEditorModel.TimeMode.Frames30:
                    DrawFrameField(30);
                    break;
                case SequenceEditorModel.TimeMode.Frames60:
                    DrawFrameField(60);
                    break;
            }
        }
    }
}