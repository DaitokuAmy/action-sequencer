using System;
using System.Collections.Generic;
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
        private List<Object> _selectedTargets = new List<Object>();
        private float _timeToSize = 100.0f;
        private int _fps = 60;
        
        public event Action<Object[]> OnChangedSelectedTargets;
        public event Action<float> OnChangedTimeToSize;
        public event Action<int> OnChangedFPS;

        public event Action<Object, SequenceEventManipulator.DragType> OnEventDragStart;
        public event Action<Object, SequenceEventManipulator.DragInfo> OnEventDragging;

        public Object[] SelectedTargets => _selectedTargets.ToArray();
        public VisualElement RootElement { get; private set; }
        public SequenceClipModel ClipModel { get; private set; }
        public float TimeToSize
        {
            get => _timeToSize;
            set
            {
                _timeToSize = Mathf.Max(1, value);
                OnChangedTimeToSize?.Invoke(_timeToSize);
            }
        }
        public float FPS
        {
            get => _fps;
            set
            {
                _fps = (int)Mathf.Clamp(value, 10, 120);
                OnChangedFPS?.Invoke(_fps);
            }
        }

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
            return Mathf.RoundToInt(time * FPS) / FPS;
        }
    }
}