using System;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceTrack 用 Presenter
    /// </summary>
    internal sealed class SequenceTrackPresenter : IDisposable {
        private SequenceTrackModel _model;
        private readonly SequenceEditorModel _editorModel;
        private readonly SelectionService _selectionService;
        private readonly TimelineViewService _timelineService;
        private readonly TrackEditingService _trackEditingService;
        private readonly EventEditingService _eventEditingService;
        private readonly System.Collections.Generic.List<IDisposable> _disposables = new();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="model">対応する TrackModel</param>
        /// <param name="view">対応する TrackLabelView</param>
        /// <param name="trackView">対応する TrackView</param>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="selectionService">選択状態を扱うサービス</param>
        /// <param name="timelineService">タイムライン設定を扱うサービス</param>
        /// <param name="trackEditingService">Track 編集サービス</param>
        /// <param name="eventEditingService">Event 編集サービス</param>
        public SequenceTrackPresenter(
            SequenceTrackModel model,
            SequenceTrackLabelView view,
            SequenceTrackView trackView,
            SequenceEditorModel editorModel,
            SelectionService selectionService,
            TimelineViewService timelineService,
            TrackEditingService trackEditingService,
            EventEditingService eventEditingService) {
            _model = model;
            View = view;
            TrackView = trackView;
            _editorModel = editorModel;
            _selectionService = selectionService;
            _timelineService = timelineService;
            _trackEditingService = trackEditingService;
            _eventEditingService = eventEditingService;

            View.LabelChanged += OnLabelChanged;
            View.OptionClicked += OnOptionClicked;
            View.FoldoutChanged += OnFoldoutChanged;
            TrackView.SpacerClicked += OnSpacerClicked;

            _trackEditingService.TrackChanged += OnTrackChanged;
            _eventEditingService.EventChanged += OnEventChanged;
            _timelineService.SettingsChanged += OnTimelineSettingsChanged;

            AddDisposable(new ActionDisposable(() => View.LabelChanged -= OnLabelChanged));
            AddDisposable(new ActionDisposable(() => View.OptionClicked -= OnOptionClicked));
            AddDisposable(new ActionDisposable(() => View.FoldoutChanged -= OnFoldoutChanged));
            AddDisposable(new ActionDisposable(() => TrackView.SpacerClicked -= OnSpacerClicked));
            AddDisposable(new ActionDisposable(() => _trackEditingService.TrackChanged -= OnTrackChanged));
            AddDisposable(new ActionDisposable(() => _eventEditingService.EventChanged -= OnEventChanged));
            AddDisposable(new ActionDisposable(() => _timelineService.SettingsChanged -= OnTimelineSettingsChanged));

            Refresh();
        }

        /// <summary>対応する TrackModel</summary>
        internal SequenceTrackModel Model => _model;
        /// <summary>対応する TrackLabelView</summary>
        internal SequenceTrackLabelView View { get; }
        /// <summary>対応する TrackView</summary>
        internal SequenceTrackView TrackView { get; }

        /// <summary>
        /// 終了処理
        /// </summary>
        public void Dispose() {
            foreach (var disposable in _disposables) {
                disposable.Dispose();
            }

            _disposables.Clear();
        }

        /// <summary>
        /// Disposable を登録
        /// </summary>
        /// <param name="disposable">登録対象</param>
        private void AddDisposable(IDisposable disposable) {
            _disposables.Add(disposable);
        }

        /// <summary>
        /// Track 情報変更時に表示全体を更新
        /// </summary>
        private void OnTrackChanged() {
            Refresh();
        }

        /// <summary>
        /// Event 情報変更時に Track 領域を更新
        /// </summary>
        private void OnEventChanged() {
            RefreshTrackArea();
        }

        /// <summary>
        /// タイムライン設定変更時に Track 領域を更新
        /// </summary>
        private void OnTimelineSettingsChanged() {
            RefreshTrackArea();
        }

        /// <summary>
        /// Track ラベル変更をサービスへ反映
        /// </summary>
        /// <param name="label">変更後のラベル</param>
        private void OnLabelChanged(string label) {
            _trackEditingService.RenameTrack(Model, label);
        }

        /// <summary>
        /// Foldout 状態変更をサービスへ反映
        /// </summary>
        /// <param name="foldout">変更後の foldout 状態</param>
        private void OnFoldoutChanged(bool foldout) {
            _trackEditingService.SetFoldout(Model, foldout);
            TrackView.SetFoldout(foldout);
        }

        /// <summary>
        /// スペーサー押下時に Foldout を切り替え
        /// </summary>
        private void OnSpacerClicked() {
            View.Foldout = !View.Foldout;
        }

        /// <summary>
        /// Track オプションメニューを表示
        /// </summary>
        private void OnOptionClicked() {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Up"), false, () => {
                var currentIndex = _editorModel.ClipModel.GetTrackIndex(Model);
                _trackEditingService.MoveTrack(Model, currentIndex - 1);
            });
            menu.AddItem(new GUIContent("Down"), false, () => {
                var currentIndex = _editorModel.ClipModel.GetTrackIndex(Model);
                _trackEditingService.MoveTrack(Model, currentIndex + 1);
            });

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Select All"), false, () => {
                _selectionService.ClearSelection();
                foreach (var eventModel in Model.EventModels) {
                    _selectionService.AddSelectedTarget(eventModel.Target);
                }
            });

            var signalTypes = TypeCache.GetTypesDerivedFrom<SignalSequenceEvent>()
                .Where(x => !x.IsAbstract && !x.IsGenericType);
            var rangeTypes = TypeCache.GetTypesDerivedFrom<RangeSequenceEvent>()
                .Where(x => !x.IsAbstract && !x.IsGenericType);

            foreach (var signalType in signalTypes) {
                var displayName = SequenceEditorUtility.GetDisplayName(signalType);
                if (!_editorModel.ClipModel.FilterNamespace(signalType) || !_editorModel.ClipModel.FilterPath(displayName)) {
                    continue;
                }

                var currentType = signalType;
                menu.AddItem(new GUIContent($"Create Event/Signal/{displayName}"), false, () => {
                    _eventEditingService.CreateEvent(Model, currentType);
                });
            }

            foreach (var rangeType in rangeTypes) {
                var displayName = SequenceEditorUtility.GetDisplayName(rangeType);
                if (!_editorModel.ClipModel.FilterNamespace(rangeType) || !_editorModel.ClipModel.FilterPath(displayName)) {
                    continue;
                }

                var currentType = rangeType;
                menu.AddItem(new GUIContent($"Create Event/Range/{displayName}"), false, () => {
                    _eventEditingService.CreateEvent(Model, currentType);
                });
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Delete Track"), false, () => { _trackEditingService.DeleteTrack(Model); });
            menu.ShowAsContext();
        }

        /// <summary>
        /// Track ラベルと表示状態を更新
        /// </summary>
        private void Refresh() {
            View.Label = Model.Label;
            View.Foldout = Model.Foldout;
            TrackView.SetFoldout(Model.Foldout);
            RefreshTrackArea();
        }

        /// <summary>
        /// 対応する TrackModel を差し替えて表示を更新
        /// </summary>
        /// <param name="model">差し替え後の TrackModel</param>
        internal void UpdateModel(SequenceTrackModel model) {
            _model = model;
            Refresh();
        }

        /// <summary>
        /// Track 全体の表示範囲を現在の Event 配置から再計算
        /// </summary>
        internal void RefreshTrackArea() {
            if (Model.EventModels.Count == 0) {
                TrackView.SetTrackArea(0.0f, 0.0f);
                return;
            }

            var minTime = Model.EventModels.Min(x => x.GetStartTime());
            var maxTime = Model.EventModels.Max(x => x.GetEndTime());
            if (minTime > maxTime) {
                minTime = maxTime;
            }

            var min = minTime * _editorModel.TimeToSize;
            var max = maxTime * _editorModel.TimeToSize;
            TrackView.SetTrackArea(min, max);
        }

        /// <summary>
        /// TrackLabelElementView を追加
        /// </summary>
        /// <returns>追加したラベル要素</returns>
        internal SequenceTrackLabelElementView AddLabelElement() {
            return View.AddElement();
        }

        /// <summary>
        /// 現在の表示順に合わせて tab index を更新
        /// </summary>
        /// <param name="offset">開始 offset</param>
        /// <returns>更新後の offset</returns>
        internal int SetTabIndices(int offset) {
            return View.SetTabIndices(offset);
        }

        /// <summary>
        /// 管理している View を階層から外す
        /// </summary>
        internal void RemoveFromHierarchy() {
            View.RemoveFromHierarchy();
            TrackView.RemoveFromHierarchy();
        }
    }
}
