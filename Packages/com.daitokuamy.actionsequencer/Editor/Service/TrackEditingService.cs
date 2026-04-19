using System;
using System.Linq;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Track 編集サービス
    /// </summary>
    internal sealed class TrackEditingService {
        private readonly SequenceEditorModel _model;
        private readonly SelectionService _selectionService;
        private readonly SequenceClipRepository _repository;
        private readonly ITrackPresentationCoordinator _trackPresentationCoordinator;
        private readonly IEventPresentationCoordinator _eventPresentationCoordinator;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="model">編集中のモデル</param>
        /// <param name="selectionService">選択状態を扱うサービス</param>
        /// <param name="repository">永続化境界</param>
        /// <param name="trackPresentationCoordinator">Track Presentation の Coordinator</param>
        /// <param name="eventPresentationCoordinator">Event Presentation の Coordinator</param>
        public TrackEditingService(
            SequenceEditorModel model,
            SelectionService selectionService,
            SequenceClipRepository repository,
            ITrackPresentationCoordinator trackPresentationCoordinator,
            IEventPresentationCoordinator eventPresentationCoordinator) {
            _model = model;
            _selectionService = selectionService;
            _repository = repository;
            _trackPresentationCoordinator = trackPresentationCoordinator;
            _eventPresentationCoordinator = eventPresentationCoordinator;
        }

        /// <summary>Track 一覧が変化したときに発火する</summary>
        public event Action TrackListChanged;
        /// <summary>Track の表示内容が変化したときに発火する</summary>
        public event Action TrackChanged;

        /// <summary>
        /// Track を作成
        /// </summary>
        /// <returns>作成後の TrackModel</returns>
        public SequenceTrackModel CreateTrack() {
            var track = _repository.CreateTrack(_model.CurrentClip);
            ReloadClipModel();
            RebuildPresentation();
            TrackListChanged?.Invoke();
            return _model.ClipModel?.FindTrackModel(track);
        }

        /// <summary>
        /// Track を削除
        /// </summary>
        /// <param name="trackModel">削除対象</param>
        public void DeleteTrack(SequenceTrackModel trackModel) {
            if (trackModel == null) {
                return;
            }

            _repository.DeleteTrack(_model.CurrentClip, trackModel.Target);
            ReloadClipModel();
            RebuildPresentation();
            TrackListChanged?.Invoke();
        }

        /// <summary>
        /// Track を移動
        /// </summary>
        /// <param name="trackModel">移動対象</param>
        /// <param name="targetIndex">移動先 index</param>
        public void MoveTrack(SequenceTrackModel trackModel, int targetIndex) {
            if (trackModel == null) {
                return;
            }

            _repository.MoveTrack(_model.CurrentClip, trackModel.Target, targetIndex);
            ReloadClipModel();
            RebuildPresentation();
            TrackListChanged?.Invoke();
        }

        /// <summary>
        /// Track ラベルを変更
        /// </summary>
        /// <param name="trackModel">変更対象</param>
        /// <param name="label">変更後のラベル</param>
        public void RenameTrack(SequenceTrackModel trackModel, string label) {
            if (trackModel == null || trackModel.Label == label) {
                return;
            }

            _repository.RenameTrack(trackModel.Target, label);
            if (trackModel.SetLabel(label)) {
                TrackChanged?.Invoke();
            }
        }

        /// <summary>
        /// Foldout 状態を変更
        /// </summary>
        /// <param name="trackModel">変更対象</param>
        /// <param name="foldout">変更後の状態</param>
        public void SetFoldout(SequenceTrackModel trackModel, bool foldout) {
            if (trackModel == null || trackModel.Foldout == foldout) {
                return;
            }

            if (trackModel.SetFoldout(foldout)) {
                TrackChanged?.Invoke();
            }
        }

        /// <summary>
        /// 現在の Model に合わせて Presentation を再構築
        /// </summary>
        internal void RebuildPresentation() {
            _eventPresentationCoordinator?.Clear();
            _trackPresentationCoordinator?.Rebuild(_model, _model.ClipModel?.TrackModels
                ?? Array.Empty<SequenceTrackModel>());
            _eventPresentationCoordinator?.Rebuild(
                _model,
                _model.ClipModel?.TrackModels ?? Array.Empty<SequenceTrackModel>(),
                _trackPresentationCoordinator?.TrackPresentations
                ?? Array.Empty<TrackPresentationContext>());
        }

        /// <summary>
        /// 現在のクリップモデルを再構築して選択状態を復元
        /// </summary>
        private void ReloadClipModel() {
            if (_model.CurrentClip == null) {
                _model.SetClipModel(null);
                _selectionService.ClearSelection();
                return;
            }

            var selectedTargets = _selectionService.SelectedTargets.ToArray();
            _repository.CleanBrokenReferences(_model.CurrentClip);
            _model.SetClipModel(_repository.Load(_model.CurrentClip));
            _selectionService.RestoreSelection(selectedTargets);
        }
    }
}
