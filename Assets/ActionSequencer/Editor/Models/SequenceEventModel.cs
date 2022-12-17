using System;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceEvent用Model基底
    /// </summary>
    public abstract class SequenceEventModel : SerializedObjectModel
    {
        private SerializedProperty _active;
        private SerializedProperty _label;
        
        // Eventのアクティブ状態
        public bool Active
        {
            get => _active.boolValue;
            set
            {
                SerializedObject.Update();
                _active.boolValue = value;
                SerializedObject.ApplyModifiedProperties();
                OnChangedActive?.Invoke(value);
            }
        }
        // Eventのラベル
        public string Label {
            get => _label.stringValue;
            set {
                if (_label.stringValue == value) {
                    return;
                }
                SerializedObject.Update();
                _label.stringValue = value;
                SerializedObject.ApplyModifiedProperties();
                OnChangedLabel?.Invoke(value);
            }
        }

        // アクティブ状態変化時
        public event Action<bool> OnChangedActive;
        // ラベル変更時
        public event Action<string> OnChangedLabel;

        // 親のTrackModel
        public SequenceTrackModel TrackModel { get; private set; }
        // テーマ色
        public Color ThemeColor { get; private set; }
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceEventModel(SequenceEvent target, SequenceTrackModel trackModel)
            : base(target)
        {
            _active = SerializedObject.FindProperty("active");
            _label = SerializedObject.FindProperty("label");
            
            TrackModel = trackModel;
            ThemeColor = SequenceEditorUtility.GetThemeColor(Target.GetType());
        }
        
        /// <summary>
        /// ラベルのリセット
        /// </summary>
        public void ResetLabel() {
            Label = SequenceEditorUtility.GetDisplayName(Target.GetType());
        }
    }
}