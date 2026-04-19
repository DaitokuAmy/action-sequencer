using System;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Event 単位の Presentation 情報
    /// </summary>
    internal sealed class EventPresentationContext : IDisposable {
        private readonly SequenceEventPresenter _presenter;
        private readonly SequenceEventView _view;
        private readonly SequenceTrackLabelElementView _labelElementView;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="presenter">管理対象の EventPresenter</param>
        /// <param name="view">管理対象の EventView</param>
        /// <param name="labelElementView">管理対象のラベル要素</param>
        public EventPresentationContext(
            SequenceEventPresenter presenter,
            SequenceEventView view,
            SequenceTrackLabelElementView labelElementView) {
            _presenter = presenter;
            _view = view;
            _labelElementView = labelElementView;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _labelElementView.RemoveFromHierarchy();
            _view.RemoveFromHierarchy();
            _presenter.Dispose();
        }
    }
}
