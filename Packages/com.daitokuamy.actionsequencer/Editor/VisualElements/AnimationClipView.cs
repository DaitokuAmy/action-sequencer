using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor.VisualElements {
    /// <summary>
    /// AnimationClipのプレビュー用のView
    /// </summary>
    public class AnimationClipView : VisualElement {
        public new class UxmlFactory : UxmlFactory<AnimationClipView, UxmlTraits> {
        }

        /// <summary>
        /// AnimationClipのEditor操作用
        /// </summary>
        private class AnimationClipEditor : IDisposable {
            private UnityEditor.Editor _editor;
            private object _timeControl;
            private FieldInfo _currentTimeFieldInfo;
            private FieldInfo _playingFieldInfo;

            public Object Target => _editor != null ? _editor.target : null;
            public UnityEditor.Editor Editor => _editor;
            public float CurrentTime => (_timeControl != null && _currentTimeFieldInfo != null)
                ? (float)_currentTimeFieldInfo.GetValue(_timeControl)
                : 0.0f;
            public bool IsPlaying => (_timeControl != null && _playingFieldInfo != null)
                ? (bool)_playingFieldInfo.GetValue(_timeControl)
                : false;

            /// <summary>
            /// コンストラクタ
            /// </summary>
            public AnimationClipEditor(AnimationClip clip) {
                _editor = UnityEditor.Editor.CreateEditor(clip);
            }

            /// <summary>
            /// フィールドの初期化
            /// </summary>
            public void SetupFields() {
                if (_editor == null) {
                    return;
                }

                // 制御用のPropertyを抽出
                var avatarPreviewFieldInfo = _editor.GetType().GetField("m_AvatarPreview",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                var avatarPreview = avatarPreviewFieldInfo?.GetValue(_editor);
                if (avatarPreview != null) {
                    var timeControlFieldInfo = avatarPreview.GetType().GetField("timeControl",
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    _timeControl = timeControlFieldInfo?.GetValue(avatarPreview);
                    if (_timeControl != null) {
                        _currentTimeFieldInfo = _timeControl.GetType().GetField("currentTime");
                        _playingFieldInfo = _timeControl.GetType().GetField("m_Playing", BindingFlags.Instance | BindingFlags.NonPublic);
                    }
                }
            }

            /// <summary>
            /// 廃棄時処理
            /// </summary>
            public void Dispose() {
                if (_editor == null) {
                    Object.DestroyImmediate(_editor);
                }

                _currentTimeFieldInfo = null;
                _timeControl = null;
                _editor = null;
            }
        }

        private IMGUIContainer _container;
        private AnimationClipEditor _animationClipEditor;
        private bool _initializedField;
        private GUIStyle _previewStyle;
        private float _previewOffsetTime;

        // Preview使用中か
        public bool IsValid => _animationClipEditor?.Target != null;
        // 現在の再生時間
        public float CurrentTime => (_animationClipEditor?.CurrentTime ?? 0.0f) + _previewOffsetTime;
        // オフセット時間
        public float OffsetTime => _previewOffsetTime;
        // 現在設定されているClip
        public AnimationClip CurrentClip => _animationClipEditor?.Target as AnimationClip; 
        // 変更通知
        public event Action<AnimationClip> OnChangedClipEvent; 
        public event Action<float> OnChangedOffsetTimeEvent; 

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AnimationClipView() {
            CreateContainer();
        }

        /// <summary>
        /// 外部からのTargetClipの変更
        /// </summary>
        public void ChangeTarget(AnimationClip targetClip) {
            SetTarget(targetClip, false);
        }

        /// <summary>
        /// 時間オフセットの変更
        /// </summary>
        public void ChangeOffsetTime(float offset) {
            SetOffsetTimeInternal(offset);
        }

        /// <summary>
        /// Inspector用コンテナの生成
        /// </summary>
        private void CreateContainer() {
            if (_container != null) {
                return;
            }

            _container = new IMGUIContainer(() => {
                using (new EditorGUILayout.HorizontalScope()) {
                    // Clipの指定
                    using (var changeScope = new EditorGUI.ChangeCheckScope()) {
                        var clip = _animationClipEditor?.Target as AnimationClip;
                        clip = EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false) as AnimationClip;
                        if (changeScope.changed) {
                            SetTarget(clip);
                            return;
                        }
                    }
                    // 開始時間オフセット
                    using (var changeScope = new EditorGUI.ChangeCheckScope()) {
                        var offsetTime = Mathf.Max(0.0f,
                            EditorGUILayout.FloatField(_previewOffsetTime, GUILayout.Width(50)));
                        if (changeScope.changed) {
                            SetOffsetTimeInternal(offsetTime);
                        }
                    }
                }
                
                if (_animationClipEditor?.Editor == null) {
                    return;
                }

                // Style初期化
                if (_previewStyle == null) {
                    _previewStyle = new GUIStyle {
                        normal = {
                            background = Texture2D.grayTexture
                        },
                        
                    };
                }

                // 基本操作を回している部分
                // ※表示範囲を限定するため、強引だがScrollで範囲制限を行う
                using (new EditorGUILayout.ScrollViewScope(Vector2.zero, false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none, GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
                    using (new EditorGUI.DisabledScope(true)) {
                        _animationClipEditor.Editor.OnInspectorGUI();
                    }
                }
                
                if (!_initializedField) {
                    _animationClipEditor.SetupFields();
                    _initializedField = true;
                }
                
                // 描画を上書きする
                var rect = contentContainer.layout;
                
                // Preview描画
                if (_animationClipEditor.Editor.HasPreviewGUI()) {
                    rect.yMin += EditorGUIUtility.singleLineHeight;
                    
                    using (new GUILayout.HorizontalScope()) {
                        EditorGUILayout.Space(rect.width - 160);
                        _animationClipEditor.Editor.OnPreviewSettings();
                    }
                    
                    rect.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    _animationClipEditor.Editor.OnInteractivePreviewGUI(rect, _previewStyle);
                }
            
                // 再生中はRepaint
                if (_animationClipEditor.IsPlaying) {
                    MarkDirtyRepaint();
                }
            });
            Add(_container);
        }

        /// <summary>
        /// ターゲットの設定
        /// </summary>
        private void SetTarget(AnimationClip target, bool invokeEvent = true) {
            if (_animationClipEditor != null && _animationClipEditor.Target == target) {
                return;
            }
            
            CreateEditor(target, invokeEvent);
        }

        /// <summary>
        /// OffsetTimeの反映
        /// </summary>
        private void SetOffsetTimeInternal(float offset) {
            offset = Mathf.Max(0.0f, offset);
            if ((offset - _previewOffsetTime) * (offset - _previewOffsetTime) <= float.Epsilon) {
                return;
            }

            _previewOffsetTime = offset;
            OnChangedOffsetTimeEvent?.Invoke(_previewOffsetTime);
        }

        /// <summary>
        /// Editor情報の解放
        /// </summary>
        private void ClearTarget(bool invokeEvent = true) {
            CreateEditor(null, invokeEvent);
        }

        /// <summary>
        /// AnimationClip用のEditorを生成
        /// </summary>
        private void CreateEditor(AnimationClip clip, bool invokeEvent) {
            if (_animationClipEditor != null && _animationClipEditor.Target == clip) {
                return;
            }

            if (_animationClipEditor != null) {
                _animationClipEditor.Dispose();
                _animationClipEditor = null;
            }

            _animationClipEditor = new AnimationClipEditor(clip);
            _initializedField = false;

            if (invokeEvent) {
                OnChangedClipEvent?.Invoke(clip);
            }
        }
    }
}