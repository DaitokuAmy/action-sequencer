using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceClipのEditor拡張
    /// </summary>
    [CustomEditor(typeof(SequenceClip)), CanEditMultipleObjects]
    public class SequenceClipEditor : UnityEditor.Editor {
        /// <summary>
        /// インスペクタ描画
        /// </summary>
        public override void OnInspectorGUI() {
            using (new EditorGUI.DisabledScope(true)) {
                var scriptProp = serializedObject.FindProperty("m_Script");
                EditorGUILayout.PropertyField(scriptProp);
            }

            serializedObject.Update();
            var includeClipsProp = serializedObject.FindProperty("includeClips");
            EditorGUILayout.PropertyField(includeClipsProp, true);
            var filterDataProp = serializedObject.FindProperty("filterData");
            EditorGUILayout.PropertyField(filterDataProp);
            serializedObject.ApplyModifiedProperties();

            if (targets.Length == 1) {
                if (GUILayout.Button("Open")) {
                    SequenceEditorWindow.Open(target as SequenceClip);
                }
            }
        }

        /// <summary>
        /// アクティブ時処理
        /// </summary>
        private void OnEnable() {
            // 選択中アセットのクリーンアップ
            foreach (var t in targets) {
                if (t is SequenceClip clip) {
                    SequenceEditorUtility.CleanUnusedSubAssets(clip);
                }
            }
        }
    }
}