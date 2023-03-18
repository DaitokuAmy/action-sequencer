using System;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEvent用Model基底
    /// </summary>
    internal abstract class SequenceEventModel : SerializedObjectModel {
        private SerializedProperty _active;
        private SerializedProperty _label;

        // Eventのアクティブ状態
        public bool Active {
            get => _active.boolValue;
            set {
                SerializedObject.Update();
                _active.boolValue = value;
                SerializedObject.ApplyModifiedProperties();
                ChangedActiveSubject.Invoke(value);
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
                ChangedLabelSubject.Invoke(value);
            }
        }

        // アクティブ状態変化時
        public Subject<bool> ChangedActiveSubject { get; } = new Subject<bool>();

        // ラベル変更時
        public Subject<string> ChangedLabelSubject { get; } = new Subject<string>();

        // 親のTrackModel
        public SequenceTrackModel TrackModel { get; private set; }

        // テーマ色
        public Color ThemeColor { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceEventModel(SequenceEvent target, SequenceTrackModel trackModel)
            : base(target) {
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