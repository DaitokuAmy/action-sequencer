using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using ActionSequencer.Editor.VisualElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceClip用のPresenter
    /// </summary>
    public class SequenceClipPresenter : Presenter<SequenceClipModel, VisualElement> {
        // Editor用Model
        private SequenceEditorModel _editorModel;

        // Track格納用
        private SequenceTrackListView _trackListView;

        // TrackのPresenterリスト
        private readonly List<SequenceTrackPresenter> _trackPresenters = new List<SequenceTrackPresenter>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceClipPresenter(SequenceClipModel model, VisualElement trackLabelListView,
            SequenceTrackListView trackListView, SequenceEditorModel editorModel)
            : base(model, trackLabelListView) {
            _editorModel = editorModel;
            _trackListView = trackListView;

            // Event監視
            AddDisposable(Model.AddedTrackModelSubject
                .Subscribe(AddedTrackModelSubject));
            AddDisposable(Model.RemovedTrackModelSubject
                .Subscribe(RemovedTrackModelSubject));
            AddDisposable(Model.AddedEventModelSubject
                .Subscribe(AddedEventModelSubject));
            AddDisposable(Model.RemovedEventModelSubject
                .Subscribe(RemovedEventModelSubject));
            AddDisposable(Model.MovedEventModelSubject
                .Subscribe(MovedEventModelSubject));
            
            _trackListView.RulerView.MaskElement = _trackListView.parent.parent;
            
            AddDisposable(_editorModel.TimeToSize
                .Subscribe(_ => {
                    _trackListView.RulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                }));
            AddDisposable(_editorModel.CurrentTimeMode
                .Subscribe(timeMode => {
                    _trackListView.RulerView.MemoryCycles = SequenceEditorUtility.GetMemoryCycles(timeMode);
                    _trackListView.RulerView.TickCycle = SequenceEditorUtility.GetTickCycle(timeMode);
                    _trackListView.RulerView.MemorySize = SequenceEditorUtility.CalcMemorySize(_editorModel);
                }));
            
            // TrackListのPaddingを表示領域まで拡張（Gray/Rulerを端まで表示するため）
            void ApplyListPadding() {
                var viewport = _trackListView.parent.parent;
                var totalWidth = viewport.layout.width - 4; // ScrollBarが出ないようにするためのOffsetを引く
                var baseWidth = _trackListView.contentRect.width;
                var padding = Mathf.Max(200, totalWidth - baseWidth);
                _trackListView.style.paddingRight = padding;
            }
            AddChangedCallback<GeometryChangedEvent>(_trackListView.parent.parent, _ => ApplyListPadding());
            AddChangedCallback<GeometryChangedEvent>(_trackListView.contentContainer,_ => ApplyListPadding());

            // 既に登録済のModelを解釈
            for (var i = 0; i < Model.TrackModels.Count; i++) {
                var trackModel = _editorModel.ClipModel.TrackModels[i];
                AddedTrackModelSubject(trackModel);
            }
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose() {
            foreach (var presenter in _trackPresenters) {
                presenter.Dispose();
            }

            _trackPresenters.Clear();

            base.Dispose();
        }

        /// <summary>
        /// TrackModel追加時
        /// </summary>
        private void AddedTrackModelSubject(SequenceTrackModel model) {
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
        private void RemovedTrackModelSubject(SequenceTrackModel model) {
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
        private void AddedEventModelSubject(SequenceEventModel model) {
            RefreshTracks();
        }

        /// <summary>
        /// TrackEvent削除時
        /// </summary>
        private void RemovedEventModelSubject(SequenceEventModel model) {
            RefreshTracks();
        }

        /// <summary>
        /// TrackEvent移動時
        /// </summary>
        private void MovedEventModelSubject() {
            RefreshTracks();
        }

        /// <summary>
        /// Track情報のリフレッシュ
        /// </summary>
        private void RefreshTracks() {
            _trackPresenters.Sort((a, b) => Model.GetTrackIndex(a.Model) - Model.GetTrackIndex(b.Model));

            var offset = 100;
            for (var i = 0; i < _trackPresenters.Count; i++) {
                var presenter = _trackPresenters[i];
                offset = presenter.View.SetTabIndices(offset);
            }

            View.Sort((a, b) =>
                _trackPresenters.FindIndex(x => x.View == a) - _trackPresenters.FindIndex(x => x.View == b));
            _trackListView.Sort((a, b) =>
                _trackPresenters.FindIndex(x => x.TrackView == a) - _trackPresenters.FindIndex(x => x.TrackView == b));
        }
    }
}