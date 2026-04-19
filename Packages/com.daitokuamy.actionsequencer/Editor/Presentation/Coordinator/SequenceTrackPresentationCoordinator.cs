using System.Collections.Generic;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceTrack 用の PresentationCoordinator
    /// </summary>
    internal sealed class SequenceTrackPresentationCoordinator : ITrackPresentationCoordinator {
        private readonly SequenceTrackPresentationFactory _factory;
        private readonly SequenceEditorView _editorView;
        private readonly List<TrackPresentationContext> _trackPresentations = new();

        private SequenceClipPresenter _clipPresenter;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="factory">Track Presentation 生成用 Factory</param>
        /// <param name="editorView">Editor 全体の View</param>
        public SequenceTrackPresentationCoordinator(
            SequenceTrackPresentationFactory factory,
            SequenceEditorView editorView) {
            _factory = factory;
            _editorView = editorView;
        }

        /// <inheritdoc/>
        public IReadOnlyList<TrackPresentationContext> TrackPresentations => _trackPresentations;

        /// <inheritdoc/>
        public void Rebuild(SequenceEditorModel editorModel, IReadOnlyList<SequenceTrackModel> trackModels) {
            Clear();

            if (editorModel == null || trackModels == null || trackModels.Count == 0) {
                return;
            }

            _clipPresenter = _factory.CreateClipPresenter(editorModel, _editorView);

            foreach (var trackModel in trackModels) {
                var labelView = new SequenceTrackLabelView();
                _editorView.TrackLabelListView.Add(labelView);

                var trackView = new SequenceTrackView();
                _editorView.TrackListView.Add(trackView);

                _trackPresentations.Add(_factory.CreateTrackPresentation(editorModel, trackModel, labelView, trackView));
            }

            RefreshTabIndices();
        }

        /// <inheritdoc/>
        public void Clear() {
            foreach (var trackPresentation in _trackPresentations) {
                trackPresentation.Dispose();
            }

            _trackPresentations.Clear();
            _clipPresenter?.Dispose();
            _clipPresenter = null;
            _editorView.TrackLabelListView.Clear();
            _editorView.TrackListView.Clear();
        }

        /// <inheritdoc/>
        public void Dispose() {
            Clear();
        }

        /// <summary>
        /// 現在の表示順に合わせて tab index を振り直す
        /// </summary>
        private void RefreshTabIndices() {
            var offset = 100;
            foreach (var trackPresentation in _trackPresentations) {
                offset = trackPresentation.SetTabIndices(offset);
            }
        }
    }
}
