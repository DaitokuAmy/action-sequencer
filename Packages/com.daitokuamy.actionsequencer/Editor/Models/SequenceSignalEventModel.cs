using System;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SignalEvent用Model
    /// </summary>
    internal class SignalSequenceEventModel : SequenceEventModel {
        private SerializedProperty _time;

        public Subject<float> ChangedTimeSubject { get; } = new Subject<float>();

        public float Time {
            get => _time.floatValue;
            set {
                SerializedObject.Update();
                _time.floatValue = Mathf.Max(0.0f, value);
                SerializedObject.ApplyModifiedProperties();
                ChangedTimeSubject.Invoke(_time.floatValue);
                SetDirty();
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SignalSequenceEventModel(SignalSequenceEvent target, SequenceTrackModel trackModel)
            : base(target, trackModel) {
            _time = SerializedObject.FindProperty("time");
        }
    }
}