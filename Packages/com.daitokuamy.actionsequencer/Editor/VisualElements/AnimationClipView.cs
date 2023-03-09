using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor.VisualElements {
    /// <summary>
    /// AnimationClipのプレビュー用のView
    /// </summary>
    public class AnimationClipView : VisualElement {
        public new class UxmlFactory : UxmlFactory<AnimationClipView, UxmlTraits> {
        }

        private IMGUIContainer _container;
        private UnityEditor.Editor _animationClipEditor;
        private bool _editorInitialized = false;
        private GUIStyle _previewStyle;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AnimationClipView() {
            RegisterCallback<DetachFromPanelEvent>(evt => { ClearTarget(); });
        }

        /// <summary>
        /// ターゲットの設定
        /// </summary>
        public void SetTarget(AnimationClip target) {
            ClearTarget();
            CreateEditor(target);
            CreateContainer();
        }

        /// <summary>
        /// Editor情報の解放
        /// </summary>
        public void ClearTarget() {
            CreateEditor(null);
        }

        /// <summary>
        /// Inspector用コンテナの生成
        /// </summary>
        private void CreateContainer() {
            if (_container != null) {
                return;
            }

            _container = new IMGUIContainer(() => {
                if (_animationClipEditor == null) {
                    return;
                }

                // Style初期化
                if (_previewStyle == null) {
                    _previewStyle = new GUIStyle {
                        normal = {
                            background = EditorGUIUtility.whiteTexture
                        }
                    };
                }

                // 初期化をInspectorで行っているので一回呼ぶ
                if (!_editorInitialized) {
                    _animationClipEditor.OnInspectorGUI();
                    _editorInitialized = true;
                }
                
                // Preview描画
                if (_animationClipEditor.HasPreviewGUI()) {
                    using (new GUILayout.HorizontalScope()) {
                        _animationClipEditor.OnPreviewSettings();
                    }
                    _animationClipEditor.OnInteractivePreviewGUI(contentContainer.layout, _previewStyle);
                }
            });
            Add(_container);
        }

        /// <summary>
        /// AnimationClip用のEditorを生成
        /// </summary>
        private void CreateEditor(AnimationClip clip) {
            if (_animationClipEditor != null && _animationClipEditor.target == clip) {
                return;
            }

            if (_animationClipEditor != null) {
                Object.DestroyImmediate(_animationClipEditor);
                _animationClipEditor = null;
            }

            if (clip != null) {
                _animationClipEditor = UnityEditor.Editor.CreateEditor(clip);
                _editorInitialized = false;
            }
        }
    }
}