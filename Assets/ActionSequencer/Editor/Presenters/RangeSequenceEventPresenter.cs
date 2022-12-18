using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// RangeSequenceEvent用のPresenter
    /// </summary>
    public class RangeSequenceEventPresenter : SequenceEventPresenter {
        private RangeSequenceEventModel _model;
        private RangeSequenceEventView _view;

        private float _dragStartEnterTime;
        private float _dragStartExitTime;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RangeSequenceEventPresenter(RangeSequenceEventModel model, RangeSequenceEventView view,
            SequenceTrackLabelElementView labelElementView, SequenceTrackModel trackModel, SequenceEditorModel editorModel)
            : base(model, view, labelElementView, trackModel, editorModel) {
            _model = model;
            _view = view;

            _view.TrackPropertyValue(Model.SerializedObject.FindProperty("enterTime"),
                prop => { _model.EnterTime = prop.floatValue; });
            _view.TrackPropertyValue(Model.SerializedObject.FindProperty("exitTime"),
                prop => { _model.ExitTime = prop.floatValue; });

            AddDisposable(EditorModel.TimeToSize
                .Subscribe(_ => SetStyleByTime(_model.EnterTime, _model.ExitTime)));

            // Modelの時間変更監視
            AddDisposable(_model.ChangedEnterTimeSubject
                .Subscribe(ChangedEnterTimeSubject));
            AddDisposable(_model.ChangedExitTimeSubject
                .Subscribe(ChangedExitTimeSubject));

            ChangedEnterTimeSubject(_model.EnterTime);
            ChangedExitTimeSubject(_model.ExitTime);
        }

        /// <summary>
        /// Drag開始処理
        /// </summary>
        protected override void OnDragStart(SequenceEventManipulator.DragType dragType, bool otherEvent) {
            _dragStartEnterTime = _model.EnterTime;
            _dragStartExitTime = _model.ExitTime;
        }

        /// <summary>
        /// Drag中処理
        /// </summary>
        protected override void OnDragging(SequenceEventManipulator.DragInfo info, bool otherEvent) {
            var delta = info.current - info.start;
            var deltaTime = SizeToTime(delta);
            switch (info.type) {
                // スライド
                case SequenceEventManipulator.DragType.Middle:
                    _model.MoveEnterTime(EditorModel.GetAbsorptionTime(_dragStartEnterTime + deltaTime),
                        exitTime => EditorModel.GetAbsorptionTime(exitTime));
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
        private void ChangedEnterTimeSubject(float time) {
            SetStyleByTime(_model.EnterTime, _model.ExitTime);
        }

        /// <summary>
        /// ExitTime変更時
        /// </summary>
        private void ChangedExitTimeSubject(float time) {
            SetStyleByTime(_model.EnterTime, _model.ExitTime);
        }

        /// <summary>
        /// 時間をViewに反映
        /// </summary>
        private void SetStyleByTime(float enterTime, float exitTime) {
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