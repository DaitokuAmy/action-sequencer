using System;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// RangeEvent用Model
    /// </summary>
    public class RangeSequenceEventModel : SequenceEventModel
    {
        private SerializedProperty _enterTime;
        private SerializedProperty _exitTime;

        public Subject<float> ChangedEnterTimeSubject { get; } = new Subject<float>();
        public Subject<float> ChangedExitTimeSubject { get; } = new Subject<float>();

        public float EnterTime
        {
            get => _enterTime.floatValue;
            set
            {
                SerializedObject.Update();
                _enterTime.floatValue = Mathf.Clamp(value, 0.0f, _exitTime.floatValue);
                SerializedObject.ApplyModifiedProperties();
                ChangedEnterTimeSubject.Invoke(_enterTime.floatValue);
                SetDirty();
            }
        }
        public float ExitTime
        {
            get => _exitTime.floatValue;
            set
            {
                SerializedObject.Update();
                _exitTime.floatValue = Mathf.Max(value, _enterTime.floatValue);
                SerializedObject.ApplyModifiedProperties();
                ChangedExitTimeSubject.Invoke(_exitTime.floatValue);
                SetDirty();
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RangeSequenceEventModel(RangeSequenceEvent target, SequenceTrackModel trackModel)
            : base(target, trackModel)
        {
            _enterTime = SerializedObject.FindProperty("enterTime");
            _exitTime = SerializedObject.FindProperty("exitTime");
        }

        /// <summary>
        /// Durationを変えずにEnterTimeを指定する
        /// </summary>
        public void MoveEnterTime(float enterTime, Func<float, float> exitTimeFilter = null)
        {
            var duration = _exitTime.floatValue - _enterTime.floatValue;
            
            SerializedObject.Update();
            _enterTime.floatValue = Mathf.Max(0.0f, enterTime);
            var exitTime = _enterTime.floatValue + duration;
            _exitTime.floatValue = exitTimeFilter?.Invoke(exitTime) ?? exitTime;
            SerializedObject.ApplyModifiedProperties();
            ChangedEnterTimeSubject.Invoke(_enterTime.floatValue);
            ChangedExitTimeSubject.Invoke(_exitTime.floatValue);
            SetDirty();
        }
    }
}