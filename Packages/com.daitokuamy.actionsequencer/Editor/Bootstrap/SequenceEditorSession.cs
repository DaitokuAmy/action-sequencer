using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEditor 用セッション
    /// </summary>
    internal sealed class SequenceEditorSession : IDisposable {
        private readonly SequenceClipRepository _repository;
        private readonly SequenceEditorModel _model;
        private readonly SelectionService _selectionService;
        private readonly TimelineViewService _timelineViewService;
        private readonly PreviewService _previewService;

        private SequenceEditorView _view;
        private SequenceEditorPresenter _presenter;
        private TrackEditingService _trackEditingService;
        private EventEditingService _eventEditingService;
        private SequenceTrackPresentationFactory _trackPresentationFactory;
        private SequenceEventPresentationFactory _eventPresentationFactory;
        private SequenceTrackPresentationCoordinator _trackPresentationCoordinator;
        private SequenceEventPresentationCoordinator _eventPresentationCoordinator;

        /// <summary>
        /// セッション状態変更時に発火する
        /// </summary>
        public event Action StateChanged;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceEditorSession() {
            _repository = new SequenceClipRepository();
            _model = new SequenceEditorModel();
            _selectionService = new SelectionService(_model);
            _timelineViewService = new TimelineViewService(_model, _repository);
            _previewService = new PreviewService(_repository);
        }

        /// <summary>
        /// UI ルートを初期化して依存関係を接続
        /// </summary>
        /// <param name="root">Window の UI ルート</param>
        public void Initialize(VisualElement root) {
            _view = new SequenceEditorView(root);
            _view.InitializeSplitViewKeys();

            _trackPresentationFactory = new SequenceTrackPresentationFactory(
                _selectionService,
                _timelineViewService,
                () => _trackEditingService,
                () => _eventEditingService);
            _eventPresentationFactory = new SequenceEventPresentationFactory(
                _selectionService,
                _timelineViewService,
                () => _eventEditingService);
            _trackPresentationCoordinator = new SequenceTrackPresentationCoordinator(
                _trackPresentationFactory,
                _view);
            _eventPresentationCoordinator = new SequenceEventPresentationCoordinator(_eventPresentationFactory);

            _trackEditingService = new TrackEditingService(
                _model,
                _selectionService,
                _repository,
                _trackPresentationCoordinator,
                _eventPresentationCoordinator);
            _eventEditingService = new EventEditingService(
                _model,
                _selectionService,
                _repository,
                _trackPresentationCoordinator,
                _eventPresentationCoordinator);

            _presenter = new SequenceEditorPresenter(
                _view,
                _model,
                _selectionService,
                _timelineViewService,
                _previewService,
                _trackEditingService,
                _eventEditingService);
            _presenter.OpenRequested += Open;
            _presenter.InspectorReloadRequested += ReloadFromInspector;

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        /// <summary>
        /// 指定したクリップを開き直す
        /// </summary>
        /// <param name="clip">開くルートの SequenceClip</param>
        /// <param name="includeClipIndex">選択する includeClip index</param>
        /// <param name="force">同一対象でも再初期化する場合は true</param>
        public void Open(SequenceClip clip, int includeClipIndex, bool force = false) {
            var resolvedIncludeClipIndex = clip != null ? Mathf.Clamp(includeClipIndex, -1, clip.includeClips.Length - 1) : -1;
            var currentClip = ResolveCurrentClip(clip, resolvedIncludeClipIndex);
            if (!force && _model.CurrentClip == currentClip) {
                return;
            }

            _model.SetClipTargets(clip, resolvedIncludeClipIndex, currentClip);
            _repository.CleanBrokenReferences(_model.CurrentClip);
            _model.SetClipModel(_repository.Load(_model.CurrentClip));
            _timelineViewService.OnClipOpened();
            _selectionService.ClearSelection();

            RefreshPresentation();
        }

        /// <summary>
        /// 現在のクリップ状態を再読み込みする
        /// </summary>
        public void Reload() {
            if (_model.CurrentClip == null) {
                _model.SetClipModel(null);
                _selectionService.ClearSelection();
                RefreshPresentation();
                return;
            }

            var selectedTargets = _selectionService.SelectedTargets.ToArray();
            _repository.CleanBrokenReferences(_model.CurrentClip);
            _model.SetClipModel(_repository.Load(_model.CurrentClip));
            _timelineViewService.OnClipReloaded();
            _selectionService.RestoreSelection(selectedTargets);

            RefreshPresentation();
            _presenter?.FocusSelection();
        }

        /// <summary>
        /// Window の表示状態を取得
        /// </summary>
        /// <param name="rootClip">現在のルートクリップ</param>
        /// <param name="includeClipIndex">現在の includeClip index</param>
        /// <param name="title">Window タイトル</param>
        public void GetWindowState(out SequenceClip rootClip, out int includeClipIndex, out string title) {
            rootClip = _model.RootClip;
            includeClipIndex = _model.IncludeClipIndex;
            title = rootClip != null ? rootClip.name : ObjectNames.NicifyVariableName(nameof(SequenceEditorWindow));
        }

        /// <summary>
        /// 再描画が必要かどうかを取得
        /// </summary>
        /// <returns>再描画が必要な場合は true</returns>
        public bool IsRepaintRequired() {
            return _presenter?.RequiresRepaint == true;
        }

        /// <summary>
        /// OnGUI で必要な更新処理
        /// </summary>
        public void OnGui() {
            _presenter?.OnGui();
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        public void Dispose() {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            if (_presenter != null) {
                _presenter.OpenRequested -= Open;
                _presenter.InspectorReloadRequested -= ReloadFromInspector;
                _presenter.Dispose();
                _presenter = null;
            }

            _eventPresentationCoordinator?.Dispose();
            _eventPresentationCoordinator = null;
            _trackPresentationCoordinator?.Dispose();
            _trackPresentationCoordinator = null;
            _eventPresentationFactory = null;
            _trackPresentationFactory = null;
            _eventEditingService = null;
            _trackEditingService = null;
            _timelineViewService.Dispose();
            _view = null;
        }

        /// <summary>
        /// 現在の Model に合わせて Presentation を更新
        /// </summary>
        private void RefreshPresentation() {
            _presenter?.Refresh();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Inspector 変更後に Inspector を作り直さず再同期
        /// </summary>
        private void ReloadFromInspector() {
            if (_model.CurrentClip == null) {
                _model.SetClipModel(null);
                _selectionService.ClearSelection();
                _presenter?.RefreshWithoutInspector();
                StateChanged?.Invoke();
                return;
            }

            var selectedTargets = _selectionService.SelectedTargets.ToArray();
            _model.SetClipModel(_repository.Load(_model.CurrentClip));
            _selectionService.RestoreSelection(selectedTargets);
            _presenter?.RefreshWithoutInspector();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Undo / Redo 後に画面を再構築
        /// </summary>
        private void OnUndoRedoPerformed() {
            Reload();
        }

        /// <summary>
        /// includeClip 設定から現在編集中のクリップを解決
        /// </summary>
        /// <param name="clip">ルートの SequenceClip</param>
        /// <param name="includeClipIndex">選択する includeClip index</param>
        /// <returns>編集対象の SequenceClip</returns>
        private SequenceClip ResolveCurrentClip(SequenceClip clip, int includeClipIndex) {
            if (clip == null) {
                return null;
            }

            return includeClipIndex >= 0 && includeClipIndex < clip.includeClips.Length
                ? clip.includeClips[includeClipIndex]
                : clip;
        }
    }
}
