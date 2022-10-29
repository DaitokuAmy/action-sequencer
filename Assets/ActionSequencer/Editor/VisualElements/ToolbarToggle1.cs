using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor.VisualElements
{
    /// <summary>
    /// InspectorView
    /// </summary>
    public class InspectorView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits> {}

        private IMGUIContainer _container;
        private UnityEditor.Editor _inspectorEditor;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public InspectorView() {
            _container = new IMGUIContainer(() => {
                if (_inspectorEditor == null) {
                    return;
                }
                _inspectorEditor.OnInspectorGUI();
            });
            Add(_container);
            
            RegisterCallback<DetachFromPanelEvent>(evt => {
                ClearTarget();
            });
        }

        /// <summary>
        /// ターゲットの設定
        /// </summary>
        public void SetTarget(Object target) {
            ClearTarget();
            _inspectorEditor = UnityEditor.Editor.CreateEditor(target);
        }

        /// <summary>
        /// ターゲットの設定
        /// </summary>
        public void SetTarget(Object[] targets) {
            ClearTarget();
            _inspectorEditor = UnityEditor.Editor.CreateEditor(targets);
        }

        /// <summary>
        /// Editor情報の解放
        /// </summary>
        public void ClearTarget() {
            if (_inspectorEditor == null) {
                Object.DestroyImmediate(_inspectorEditor);
                _inspectorEditor = null;
            }
        }
    }
}
