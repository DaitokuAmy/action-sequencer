using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// RangeSequenceEvent 用 Presenter
    /// </summary>
    internal sealed class RangeSequenceEventPresenter : SequenceEventPresenter {
        private float _dragStartEnterTime;
        private float _dragStartExitTime;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="model">対応する RangeEventModel</param>
        /// <param name="view">対応する RangeEventView</param>
        /// <param name="trackView">トラック要素 View</param>
        /// <param name="labelElementView">ラベル要素の View</param>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="selectionService">選択状態を扱うサービス</param>
        /// <param name="timelineService">タイムライン設定を扱うサービス</param>
        /// <param name="eventEditingService">Event 編集サービス</param>
        public RangeSequenceEventPresenter(
            RangeSequenceEventModel model,
            RangeSequenceEventView view,
            SequenceTrackView trackView,
            SequenceTrackLabelElementView labelElementView,
            SequenceEditorModel editorModel,
            SelectionService selectionService,
            TimelineViewService timelineService,
            EventEditingService eventEditingService)
            : base(
                model,
                view,
                trackView,
                labelElementView,
                editorModel,
                selectionService,
                timelineService,
                eventEditingService) {
        }

        private RangeSequenceEventModel RangeModel => (RangeSequenceEventModel)Model;
        private RangeSequenceEventView RangeView => (RangeSequenceEventView)View;

        /// <inheritdoc/>
        protected override void OnDragStart(SequenceEventManipulator.DragType dragType, bool otherEvent) {
            _dragStartEnterTime = RangeModel.EnterTime;
            _dragStartExitTime = RangeModel.ExitTime;
        }

        /// <inheritdoc/>
        protected override void OnDragging(SequenceEventManipulator.DragInfo dragInfo, bool otherEvent) {
            var deltaTime = SizeToTime(dragInfo.Current - dragInfo.Start);
            switch (dragInfo.Type) {
                case SequenceEventManipulator.DragType.Middle: {
                    var duration = _dragStartExitTime - _dragStartEnterTime;
                    if (TryGetSharedMiddleDragDeltaTime(dragInfo, out var sharedDeltaTime)) {
                        var sharedEnterTime = Mathf.Max(0.0f, _dragStartEnterTime + sharedDeltaTime);
                        EventEditingService.MoveRangeKeepingDuration(RangeModel, sharedEnterTime, sharedEnterTime + duration);
                        break;
                    }

                    var desiredEnterTime = Mathf.Max(0.0f, _dragStartEnterTime + deltaTime);
                    var enterTime = TimelineService.GetAbsorptionTime(desiredEnterTime);
                    var exitTime = enterTime + duration;
                    EventEditingService.MoveRangeKeepingDuration(RangeModel, enterTime, exitTime);
                    break;
                }
                case SequenceEventManipulator.DragType.LeftSide: {
                    var enterTime = TimelineService.GetAbsorptionTime(_dragStartEnterTime + deltaTime);
                    EventEditingService.SetRangeTimes(RangeModel, enterTime, RangeModel.ExitTime);
                    break;
                }
                case SequenceEventManipulator.DragType.RightSide: {
                    var exitTime = TimelineService.GetAbsorptionTime(_dragStartExitTime + deltaTime);
                    EventEditingService.SetRangeTimes(RangeModel, RangeModel.EnterTime, exitTime);
                    break;
                }
            }
        }

        /// <inheritdoc/>
        protected override void RefreshGeometry() {
            var centerTime = (RangeModel.EnterTime + RangeModel.ExitTime) * 0.5f;
            var centerPosition = TimeToSize(centerTime);
            var leftPosition = TimeToSize(RangeModel.EnterTime);
            var rightPosition = TimeToSize(RangeModel.ExitTime);

            leftPosition = centerPosition + Mathf.Min(leftPosition - centerPosition, -5.0f);
            rightPosition = centerPosition + Mathf.Max(rightPosition - centerPosition, 5.0f);

            RangeView.LeftPosition = leftPosition;
            RangeView.RightPosition = rightPosition;
        }
    }
}
