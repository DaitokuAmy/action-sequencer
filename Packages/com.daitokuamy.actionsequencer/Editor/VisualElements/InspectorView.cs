using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor.VisualElements {
    /// <summary>
    /// InspectorView
    /// </summary>
    public class InspectorView : VisualElement {
        public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits> {
        }

        private IMGUIContainer _container;
        private UnityEditor.Editor _inspectorEditor;

        internal SequenceEditorModel.TimeMode TimeMode { get; set; } = SequenceEditorModel.TimeMode.Seconds;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public InspectorView() {
            RegisterCallback<DetachFromPanelEvent>(evt => { ClearTarget(); });
        }

        /// <summary>
        /// ターゲットの設定
        /// </summary>
        public void SetTarget(Object target) {
            ClearTarget();
            _inspectorEditor = UnityEditor.Editor.CreateEditor(target);
            CreateContainer();
        }

        /// <summary>
        /// ターゲットの設定
        /// </summary>
        public void SetTarget(Object[] targets) {
            ClearTarget();
            _inspectorEditor = UnityEditor.Editor.CreateEditor(targets);
            CreateContainer();
        }

        /// <summary>
        /// Editor情報の解放
        /// </summary>
        public void ClearTarget() {
            if (_inspectorEditor != null) {
                Object.DestroyImmediate(_inspectorEditor);
                _inspectorEditor = null;
            }
        }

        /// <summary>
        /// Inspector用コンテナの生成
        /// </summary>
        private void CreateContainer() {
            if (_container != null) {
                return;
            }

            _container = new IMGUIContainer(() => {
                if (_inspectorEditor == null) {
                    return;
                }

                var prevMode = SequenceEditorGUI.TimeMode;
                SequenceEditorGUI.TimeMode = TimeMode;
                try {
                    _inspectorEditor.OnInspectorGUI();
                }
                catch (Exception ex) {
                    Debug.LogException(ex);
                    ClearTarget();
                }
                SequenceEditorGUI.TimeMode = prevMode;
            });
            Add(_container);
        }
    }
}