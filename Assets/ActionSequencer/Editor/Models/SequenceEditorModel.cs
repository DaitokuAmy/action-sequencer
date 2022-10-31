using System;
using System.Collections.Generic;
using ActionSequencer.Editor.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceEditor用のModel
    /// </summary>
    public class SequenceEditorModel : Model
    {
        // 時間モード
        public enum TimeMode
        {
            Seconds,
            Frames30,
            Frames60,
        }
        
        private List<Object> _selectedTargets = new List<Object>();
        
        public event Action<Object[]> OnChangedSelectedTargets;

        public event Action<Object, SequenceEventManipulator.DragType> OnEventDragStart;
        public event Action<Object, SequenceEventManipulator.DragInfo> OnEventDragging;

        public Object[] SelectedTargets => _selectedTargets.ToArray();
        public VisualElement RootElement { get; private set; }
        public SequenceClipModel ClipModel { get; private set; }
        
        public ReactiveProperty<float> TimeToSize { get; private set; } = new ReactiveProperty<float>(100.0f, x => Mathf.Max(1.0f, x));
        public ReactiveProperty<TimeMode> CurrentTimeMode { get; private set; } = new ReactiveProperty<TimeMode>(TimeMode.Seconds);
        public ReactiveProperty<bool> TimeFit { get; private set; } = new ReactiveProperty<bool>(true);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceEditorModel(VisualElement rootElement)
        {
            RootElement = rootElement;
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            
            SetSequenceClip(null);
        }

        /// <summary>
        /// SequenceClipの設定
        /// </summary>
        public SequenceClipModel SetSequenceClip(SequenceClip clip)
        {
            if (ClipModel != null)
            {
                ClipModel.Dispose();
                ClipModel = null;
            }

            if (clip != null)
            {
                ClipModel = new SequenceClipModel(clip);
            }
            
            return ClipModel;
        }

        /// <summary>
        /// 選択対象の変更
        /// </summary>
        public void SetTarget(Object target)
        {
            _selectedTargets.Clear();
            AddTarget(target);
        }

        /// <summary>
        /// 選択対象の追加
        /// </summary>
        public void AddTarget(Object target)
        {
            if (_selectedTargets.Contains(target))
            {
                return;
            }
            _selectedTargets.Add(target);
            OnChangedSelectedTargets?.Invoke(SelectedTargets);
        }

        /// <summary>
        /// Drag開始
        /// </summary>
        public void StartDragEvent(Object target, SequenceEventManipulator.DragType dragType)
        {
            OnEventDragStart?.Invoke(target, dragType);
        }

        /// <summary>
        /// Drag終了
        /// </summary>
        public void DraggingEvent(Object target, SequenceEventManipulator.DragInfo info)
        {
            OnEventDragging?.Invoke(target, info);
        }

        /// <summary>
        /// 吸着させた場合の時間を取得
        /// </summary>
        public float GetAbsorptionTime(float time)
        {
            if (!TimeFit.Value)
            {
                return time;
            }
            
            switch (CurrentTimeMode.Value)
            {
                case TimeMode.Seconds:
                    return Mathf.RoundToInt(time * 20) / 20.0f;
                case TimeMode.Frames30:
                    return Mathf.RoundToInt(time * 30) / 30.0f;
                case TimeMode.Frames60:
                    return Mathf.RoundToInt(time * 60) / 60.0f;
            }

            return time;
        }
    }
}