using ActionSequencer.Editor.Utils;
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

            serializedObject.Update();
            var filterData = serializedObject.FindProperty("filterData");
            EditorGUILayout.PropertyField(filterData);
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Open")) {
                SequenceEditorWindow.Open(target as SequenceClip);
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