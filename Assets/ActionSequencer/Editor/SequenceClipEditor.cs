using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceClipのEditor拡張
    /// </summary>
    [CustomEditor(typeof(SequenceClip))]
    public class SequenceClipEditor : UnityEditor.Editor {
        /// <summary>
        /// インスペクタ描画
        /// </summary>
        public override void OnInspectorGUI() {
            using (new EditorGUI.DisabledScope(true)) {
                base.OnInspectorGUI();
            }

            if (GUILayout.Button("Open")) {
                SequenceEditorWindow.Open(target as SequenceClip);
            }
        }
    }
}