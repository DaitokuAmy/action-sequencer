using System;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SignalEvent用Model
    /// </summary>
    internal class SignalSequenceEventModel : SequenceEventModel {
        private SignalSequenceEvent _targetEvent;
        private SerializedProperty _time;
        private SerializedProperty _viewDuration;

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
        public float ViewDuration {
            get => _targetEvent != null ? _targetEvent.ViewDuration : 0.0f;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SignalSequenceEventModel(SignalSequenceEvent target, SequenceTrackModel trackModel)
            : base(target, trackModel) {
            _targetEvent = target;
            _time = SerializedObject.FindProperty("time");
        }
    }
}