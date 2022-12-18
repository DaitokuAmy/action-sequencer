using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceClip用のPresenter
    /// </summary>
    public class SequenceClipPresenter : Presenter<SequenceClipModel, VisualElement> {
        // Editor用Model
        private SequenceEditorModel _editorModel;
        // Track格納用
        private VisualElement _trackListView;
        // TrackのPresenterリスト
        private readonly List<SequenceTrackPresenter> _trackPresenters = new List<SequenceTrackPresenter>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceClipPresenter(SequenceClipModel model, VisualElement trackLabelListView,
            VisualElement trackListView, SequenceEditorModel editorModel)
            : base(model, trackLabelListView) {
            _editorModel = editorModel;
            _trackListView = trackListView;

            // Event監視
            Model.OnAddedTrackModel += OnAddedTrackModel;
            Model.OnRemovedTrackModel += OnRemovedTrackModel;
            Model.OnAddedEventModel += OnAddedEventModel;
            Model.OnRemovedEventModel += OnRemovedEventModel;

            // 既に登録済のModelを解釈
            for (var i = 0; i < Model.TrackModels.Count; i++) {
                var trackModel = _editorModel.ClipModel.TrackModels[i];
                OnAddedTrackModel(trackModel);
            }
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose() {
            base.Dispose();

            Model.OnAddedTrackModel -= OnAddedTrackModel;
            Model.OnRemovedTrackModel -= OnRemovedTrackModel;
            Model.OnAddedEventModel -= OnAddedEventModel;
            Model.OnRemovedEventModel -= OnRemovedEventModel;
        }

        /// <summary>
        /// TrackModel追加時
        /// </summary>
        private void OnAddedTrackModel(SequenceTrackModel model) {
            var labelView = new SequenceTrackLabelView();
            View.Add(labelView);
            var trackView = new SequenceTrackView();
            _trackListView.Add(trackView);
            var presenter = new SequenceTrackPresenter(model, labelView, trackView, _editorModel);
            _trackPresenters.Add(presenter);
            RefreshTracks();
        }

        /// <summary>
        /// TrackModel削除時
        /// </summary>
        private void OnRemovedTrackModel(SequenceTrackModel model) {
            var presenter = _trackPresenters.FirstOrDefault(x => x.Model == model);
            if (presenter == null) {
                return;
            }

            View.Remove(presenter.View);
            _trackListView.Remove(presenter.TrackView);
            presenter.Dispose();
            _trackPresenters.Remove(presenter);
            RefreshTracks();
        }

        /// <summary>
        /// TrackEvent追加時
        /// </summary>
        private void OnAddedEventModel(SequenceEventModel model) {
            RefreshTracks();
        }

        /// <summary>
        /// TrackEvent削除時
        /// </summary>
        private void OnRemovedEventModel(SequenceEventModel model) {
            RefreshTracks();
        }

        /// <summary>
        /// Track情報のリフレッシュ
        /// </summary>
        private void RefreshTracks() {
            var offset = 100;
            for (var i = 0; i < _trackPresenters.Count; i++) {
                var presenter = _trackPresenters[i];
                offset = presenter.View.SetTabIndices(offset);
            }
        }
    }
}