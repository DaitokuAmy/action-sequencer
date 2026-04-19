using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SignalSequenceEvent 用 Presenter
    /// </summary>
    internal sealed class SignalSequenceEventPresenter : SequenceEventPresenter {
        private float _dragStartTime;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="model">対応する SignalEventModel</param>
        /// <param name="view">対応する SignalEventView</param>
        /// <param name="labelElementView">ラベル要素の View</param>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="selectionService">選択状態を扱うサービス</param>
        /// <param name="timelineService">タイムライン設定を扱うサービス</param>
        /// <param name="eventEditingService">Event 編集サービス</param>
        public SignalSequenceEventPresenter(
            SignalSequenceEventModel model,
            SignalSequenceEventView view,
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

        private SignalSequenceEventModel SignalModel => (SignalSequenceEventModel)Model;
        private SignalSequenceEventView SignalView => (SignalSequenceEventView)View;

        /// <inheritdoc/>
        protected override void OnDragStart(SequenceEventManipulator.DragType dragType, bool otherEvent) {
            _dragStartTime = SignalModel.Time;
        }

        /// <inheritdoc/>
        protected override void OnDragging(SequenceEventManipulator.DragInfo dragInfo, bool otherEvent) {
            if (TryGetSharedMiddleDragDeltaTime(dragInfo, out var sharedDeltaTime)) {
                EventEditingService.SetSignalTime(SignalModel, _dragStartTime + sharedDeltaTime);
                return;
            }

            var deltaTime = SizeToTime(dragInfo.Current - dragInfo.Start);
            var snappedTime = TimelineService.GetAbsorptionTime(_dragStartTime + deltaTime);
            EventEditingService.SetSignalTime(SignalModel, snappedTime);
        }

        /// <inheritdoc/>
        protected override void RefreshGeometry() {
            SignalView.Position = TimeToSize(SignalModel.Time);
            SignalView.Width = TimeToSize(SignalModel.ViewDuration);
        }

        /// <inheritdoc/>
        protected override void Refresh() {
            base.Refresh();
            SignalView.style.backgroundColor = Color.clear;
            SignalView.SignalColor = SignalModel.Active ? SignalModel.ThemeColor : Color.gray;
        }

        /// <inheritdoc/>
        protected override float GetTimelineTextOffset() {
            return 15.0f;
        }
    }
}
