using System;
using System.Reflection;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceEvent用Model基底
    /// </summary>
    public abstract class SequenceEventModel : SerializedObjectModel
    {
        private SerializedProperty _active;
        
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

        // アクティブ状態変化時
        public event Action<bool> OnChangedActive;

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
            
            TrackModel = trackModel;
            ThemeColor = SequenceEditorUtility.GetThemeColor(Target.GetType());
        }
    }
}