using System;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Track Presenter 生成用 Factory
    /// </summary>
    internal sealed class SequenceTrackPresentationFactory {
        private readonly SelectionService _selectionService;
        private readonly TimelineViewService _timelineService;
        private readonly Func<TrackEditingService> _trackEditingServiceProvider;
        private readonly Func<EventEditingService> _eventEditingServiceProvider;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="selectionService">選択状態を扱うサービス</param>
        /// <param name="timelineService">タイムライン設定を扱うサービス</param>
        /// <param name="trackEditingServiceProvider">Track 編集サービスの取得関数</param>
        /// <param name="eventEditingServiceProvider">Event 編集サービスの取得関数</param>
        public SequenceTrackPresentationFactory(
            SelectionService selectionService,
            TimelineViewService timelineService,
            Func<TrackEditingService> trackEditingServiceProvider,
            Func<EventEditingService> eventEditingServiceProvider) {
            _selectionService = selectionService;
            _timelineService = timelineService;
            _trackEditingServiceProvider = trackEditingServiceProvider;
            _eventEditingServiceProvider = eventEditingServiceProvider;
        }

        /// <summary>
        /// ClipPresenter を生成
        /// </summary>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="editorView">Editor 全体の View</param>
        /// <returns>生成した ClipPresenter</returns>
        public SequenceClipPresenter CreateClipPresenter(
            SequenceEditorModel editorModel,
            SequenceEditorView editorView) {
            return new SequenceClipPresenter(
                editorView.TrackLabelListView,
                editorView.TrackScrollView,
                editorView.TrackRulerAreaView,
                editorView.TrackListView,
                editorModel,
                _timelineService);
        }

        /// <summary>
        /// TrackPresentation を生成
        /// </summary>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="trackModel">生成対象の TrackModel</param>
        /// <param name="labelView">対応する TrackLabelView</param>
        /// <param name="trackView">対応する TrackView</param>
        /// <returns>生成した TrackPresentation</returns>
        public TrackPresentationContext CreateTrackPresentation(
            SequenceEditorModel editorModel,
            SequenceTrackModel trackModel,
            SequenceTrackLabelView labelView,
            SequenceTrackView trackView) {
            var presenter = new SequenceTrackPresenter(
                trackModel,
                labelView,
                trackView,
                editorModel,
                _selectionService,
                _timelineService,
                _trackEditingServiceProvider(),
                _eventEditingServiceProvider());
            return new TrackPresentationContext(trackModel, presenter);
        }
    }
}
