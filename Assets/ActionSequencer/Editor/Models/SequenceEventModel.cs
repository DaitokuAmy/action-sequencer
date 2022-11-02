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
                if (_active.boolValue == value)
                {
                    return;
                }
                
                SerializedObject.Update();
                _active.boolValue = value;
                SerializedObject.ApplyModifiedProperties();
                OnChangedActive?.Invoke(value);
            }
        }

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
            SetupThemeColor();
        }

        /// <summary>
        /// テーマカラーの初期化
        /// </summary>
        private void SetupThemeColor() {
            // Attributeチェック
            if (Target.GetType().GetCustomAttribute(typeof(SequenceEventAttribute)) is SequenceEventAttribute attr) {
                if (attr.ThemeColor.a > float.Epsilon) {
                    ThemeColor = attr.ThemeColor;
                    return;
                }
            }
            
            // 無ければ自動生成
            var prevState = Random.state;
            Random.InitState(Target.GetType().Name.GetHashCode());
            ThemeColor = Random.ColorHSV(0.0f, 1.0f, 0.4f, 0.4f, 0.9f, 0.9f);
            Random.state = prevState;
        }
    }
}