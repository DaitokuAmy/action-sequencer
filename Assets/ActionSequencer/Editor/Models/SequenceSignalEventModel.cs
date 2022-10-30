using System;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SignalEvent用Model
    /// </summary>
    public class SequenceSignalEventModel : SequenceEventModel
    {
        private SerializedProperty _time;

        public event Action<float> OnChangedTime;
        
        public float Time
        {
            get => _time.floatValue;
            set
            {
                SerializedObject.Update();
                _time.floatValue = Mathf.Max(0.0f, value);
                SerializedObject.ApplyModifiedProperties();
                OnChangedTime?.Invoke(_time.floatValue);
                SetDirty();
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceSignalEventModel(SequenceSignalEvent target)
            : base(target)
        {
            _time = SerializedObject.FindProperty("time");
        }
    }
}