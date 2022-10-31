using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// RangeSequenceEvent用のPresenter
    /// </summary>
    public class RangeSequenceEventPresenter : SequenceEventPresenter
    {
        private RangeSequenceEventModel _model;
        private RangeSequenceEventView _view;

        private float _dragStartEnterTime;
        private float _dragStartExitTime;
        private List<IDisposable> _disposables = new List<IDisposable>();
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RangeSequenceEventPresenter(RangeSequenceEventModel model, RangeSequenceEventView view, SequenceEditorModel editorModel)
            : base(model, view, editorModel)
        {
            _model = model;
            _view = view;
            
            _view.Unbind();
            _view.TrackPropertyValue(Model.SerializedObject.FindProperty("enterTime"), prop =>
            {
                _model.EnterTime = prop.floatValue;
            });
            _view.TrackPropertyValue(Model.SerializedObject.FindProperty("exitTime"), prop =>
            {
                _model.ExitTime = prop.floatValue;
            });

            _disposables.Add(EditorModel.TimeToSize
                .Subscribe(_ => SetStyleByTime(_model.EnterTime, _model.ExitTime)));

            // Modelの時間変更監視
            _model.OnChangedEnterTime += OnChangedEnterTime;
            _model.OnChangedExitTime += OnChangedExitTime;
            
            OnChangedEnterTime(_model.EnterTime);
            OnChangedExitTime(_model.ExitTime);
        }
        
        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            _model.OnChangedEnterTime -= OnChangedEnterTime;
            _model.OnChangedExitTime -= OnChangedExitTime;
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
            _disposables.Clear();
        }

        /// <summary>
        /// Drag開始処理
        /// </summary>
        protected override void OnDragStart(SequenceEventManipulator.DragType dragType, bool otherEvent)
        {
            _dragStartEnterTime = _model.EnterTime;
            _dragStartExitTime = _model.ExitTime;
        }
        
        /// <summary>
        /// Drag中処理
        /// </summary>
        protected override void OnDragging(SequenceEventManipulator.DragInfo info, bool otherEvent)
        {
            var delta = info.current - info.start;
            var deltaTime = SizeToTime(delta);
            switch (info.type)
            {
                // スライド
                case SequenceEventManipulator.DragType.Middle:
                    _model.MoveEnterTime(EditorModel.GetAbsorptionTime(_dragStartEnterTime + deltaTime), exitTime => EditorModel.GetAbsorptionTime(exitTime));
                    break;
                case SequenceEventManipulator.DragType.LeftSide:
                    _model.EnterTime = EditorModel.GetAbsorptionTime(_dragStartEnterTime + deltaTime);
                    break;
                case SequenceEventManipulator.DragType.RightSide:
                    _model.ExitTime = EditorModel.GetAbsorptionTime(_dragStartExitTime + deltaTime);
                    break;
            }
        }
        
        /// <summary>
        /// EnterTime変更時
        /// </summary>
        private void OnChangedEnterTime(float time)
        {
            SetStyleByTime(_model.EnterTime, _model.ExitTime);
        }

        /// <summary>
        /// ExitTime変更時
        /// </summary>
        private void OnChangedExitTime(float time)
        {
            SetStyleByTime(_model.EnterTime, _model.ExitTime);
        }

        /// <summary>
        /// 時間をViewに反映
        /// </summary>
        private void SetStyleByTime(float enterTime, float exitTime)
        {
            var centerTime = (enterTime + exitTime) * 0.5f;
            var centerPos = TimeToSize(centerTime);
            var rightPos = TimeToSize(exitTime);
            var leftPos = TimeToSize(enterTime);
            leftPos = centerPos + Mathf.Min(leftPos - centerPos, -5);
            rightPos = centerPos + Mathf.Max(rightPos - centerPos, 5);
            _view.style.marginLeft = leftPos;
            _view.style.width = rightPos - leftPos;
            EditorModel.RootElement.MarkDirtyRepaint();
        }
    }
}
