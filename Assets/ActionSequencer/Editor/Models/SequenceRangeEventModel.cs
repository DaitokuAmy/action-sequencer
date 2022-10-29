using System;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// RangeEvent用Model
    /// </summary>
    public class SequenceRangeEventModel : SerializedObjectModel
    {
        private SerializedProperty _enterTime;
        private SerializedProperty _exitTime;

        public event Action<float> OnChangedEnterTime; 
        public event Action<float> OnChangedExitTime;

        public float EnterTime
        {
            get => _enterTime.floatValue;
            set
            {
                SerializedObject.Update();
                _enterTime.floatValue = Mathf.Clamp(value, 0.0f, _exitTime.floatValue);
                SerializedObject.ApplyModifiedProperties();
                OnChangedEnterTime?.Invoke(_enterTime.floatValue);
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
                OnChangedExitTime?.Invoke(_exitTime.floatValue);
                SetDirty();
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceRangeEventModel(SequenceRangeEvent target)
            : base(target)
        {
            _enterTime = SerializedObject.FindProperty("enterTime");
            _exitTime = SerializedObject.FindProperty("exitTime");
        }

        /// <summary>
        /// Durationを変えずにEnterTimeを指定する
        /// </summary>
        public void MoveEnterTime(float enterTime)
        {
            var duration = _exitTime.floatValue - _enterTime.floatValue;
            
            SerializedObject.Update();
            _enterTime.floatValue = Mathf.Max(0.0f, enterTime);
            _exitTime.floatValue = _enterTime.floatValue + duration;
            SerializedObject.ApplyModifiedProperties();
            OnChangedEnterTime?.Invoke(_enterTime.floatValue);
            OnChangedExitTime?.Invoke(_exitTime.floatValue);
            SetDirty();
        }
    }
}