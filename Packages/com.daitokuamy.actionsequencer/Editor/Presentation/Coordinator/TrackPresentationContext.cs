using System;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Track 単位の Presentation 情報
    /// </summary>
    internal sealed class TrackPresentationContext : IDisposable {
        private readonly SequenceTrackPresenter _presenter;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="model">対応する TrackModel</param>
        /// <param name="presenter">対応する TrackPresenter</param>
        public TrackPresentationContext(SequenceTrackModel model, SequenceTrackPresenter presenter) {
            Model = model;
            _presenter = presenter;
        }

        /// <summary>対応する TrackModel</summary>
        public SequenceTrackModel Model { get; private set; }

        /// <summary>対応する Track</summary>
        public SequenceTrack Target => Model.Target;

        /// <summary>Event 追加先の TrackView</summary>
        public SequenceTrackView TrackView => _presenter.TrackView;

        /// <summary>
        /// TrackLabelElementView を追加
        /// </summary>
        /// <returns>追加したラベル要素</returns>
        public SequenceTrackLabelElementView AddLabelElement() {
            return _presenter.AddLabelElement();
        }

        /// <summary>
        /// 現在の表示順に合わせて tab index を更新
        /// </summary>
        /// <param name="offset">開始 offset</param>
        /// <returns>更新後の offset</returns>
        public int SetTabIndices(int offset) {
            return _presenter.SetTabIndices(offset);
        }

        /// <summary>
        /// 対応する TrackModel を差し替える
        /// </summary>
        /// <param name="model">差し替え後の TrackModel</param>
        public void UpdateModel(SequenceTrackModel model) {
            Model = model;
            _presenter.UpdateModel(model);
        }

        /// <summary>
        /// Track 領域表示を更新
        /// </summary>
        public void RefreshTrackArea() {
            _presenter.RefreshTrackArea();
        }

        /// <inheritdoc/>
        public void Dispose() {
            _presenter.RemoveFromHierarchy();
            _presenter.Dispose();
        }
    }
}
