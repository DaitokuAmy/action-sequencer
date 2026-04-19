using System;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Event Presenter 生成用 Factory
    /// </summary>
    internal sealed class SequenceEventPresentationFactory {
        private readonly SelectionService _selectionService;
        private readonly TimelineViewService _timelineService;
        private readonly Func<EventEditingService> _eventEditingServiceProvider;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="selectionService">選択状態を扱うサービス</param>
        /// <param name="timelineService">タイムライン設定を扱うサービス</param>
        /// <param name="eventEditingServiceProvider">Event 編集サービスの取得関数</param>
        public SequenceEventPresentationFactory(
            SelectionService selectionService,
            TimelineViewService timelineService,
            Func<EventEditingService> eventEditingServiceProvider) {
            _selectionService = selectionService;
            _timelineService = timelineService;
            _eventEditingServiceProvider = eventEditingServiceProvider;
        }

        /// <summary>
        /// EventPresentation を生成
        /// </summary>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="trackPresentation">追加先の TrackPresentation</param>
        /// <param name="eventModel">生成対象の EventModel</param>
        /// <returns>生成した EventPresentation</returns>
        public EventPresentationContext CreateEventPresentation(
            SequenceEditorModel editorModel,
            TrackPresentationContext trackPresentation,
            SequenceEventModel eventModel) {
            var labelElementView = trackPresentation.AddLabelElement();
            labelElementView.userData = eventModel.Target;

            return eventModel switch {
                SignalSequenceEventModel signalEventModel => CreateSignalEventPresentation(
                    editorModel,
                    trackPresentation.TrackView,
                    labelElementView,
                    signalEventModel),
                RangeSequenceEventModel rangeEventModel => CreateRangeEventPresentation(
                    editorModel,
                    trackPresentation.TrackView,
                    labelElementView,
                    rangeEventModel),
                _ => throw new NotSupportedException($"Unsupported event model: {eventModel.GetType().Name}"),
            };
        }

        /// <summary>
        /// SignalEvent 用 Presentation を生成
        /// </summary>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="trackView">追加先の TrackView</param>
        /// <param name="labelElementView">追加先のラベル要素</param>
        /// <param name="eventModel">生成対象の EventModel</param>
        /// <returns>生成した EventPresentation</returns>
        private EventPresentationContext CreateSignalEventPresentation(
            SequenceEditorModel editorModel,
            SequenceTrackView trackView,
            SequenceTrackLabelElementView labelElementView,
            SignalSequenceEventModel eventModel) {
            var eventView = new SignalSequenceEventView {
                userData = eventModel.Target
            };
            trackView.AddEventView(eventView);

            var presenter = new SignalSequenceEventPresenter(
                eventModel,
                eventView,
                trackView,
                labelElementView,
                editorModel,
                _selectionService,
                _timelineService,
                _eventEditingServiceProvider());
            return new EventPresentationContext(presenter, eventView, labelElementView);
        }

        /// <summary>
        /// RangeEvent 用 Presentation を生成
        /// </summary>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="trackView">追加先の TrackView</param>
        /// <param name="labelElementView">追加先のラベル要素</param>
        /// <param name="eventModel">生成対象の EventModel</param>
        /// <returns>生成した EventPresentation</returns>
        private EventPresentationContext CreateRangeEventPresentation(
            SequenceEditorModel editorModel,
            SequenceTrackView trackView,
            SequenceTrackLabelElementView labelElementView,
            RangeSequenceEventModel eventModel) {
            var eventView = new RangeSequenceEventView {
                userData = eventModel.Target
            };
            trackView.AddEventView(eventView);

            var presenter = new RangeSequenceEventPresenter(
                eventModel,
                eventView,
                trackView,
                labelElementView,
                editorModel,
                _selectionService,
                _timelineService,
                _eventEditingServiceProvider());
            return new EventPresentationContext(presenter, eventView, labelElementView);
        }
    }
}
