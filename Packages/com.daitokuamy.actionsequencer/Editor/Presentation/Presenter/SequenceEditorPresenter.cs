using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ToolbarToggle = UnityEditor.UIElements.ToolbarToggle;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEditor 全体の Presenter
    /// </summary>
    internal sealed class SequenceEditorPresenter : IDisposable {
        private readonly SequenceEditorView _view;
        private readonly SequenceEditorModel _model;
        private readonly SelectionService _selectionService;
        private readonly TimelineViewService _timelineViewService;
        private readonly PreviewService _previewService;
        private readonly TrackEditingService _trackEditingService;
        private readonly EventEditingService _eventEditingService;
        private readonly List<IDisposable> _disposables = new();

        private GameObject _playerProviderOwner;
        private ISequencePlayerProvider _playerProvider;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="view">対応する EditorView</param>
        /// <param name="model">編集中のモデル</param>
        /// <param name="selectionService">選択状態を扱うサービス</param>
        /// <param name="timelineViewService">タイムライン設定を扱うサービス</param>
        /// <param name="previewService">Preview 設定を扱うサービス</param>
        /// <param name="trackEditingService">Track 編集サービス</param>
        /// <param name="eventEditingService">Event 編集サービス</param>
        public SequenceEditorPresenter(
            SequenceEditorView view,
            SequenceEditorModel model,
            SelectionService selectionService,
            TimelineViewService timelineViewService,
            PreviewService previewService,
            TrackEditingService trackEditingService,
            EventEditingService eventEditingService) {
            _view = view;
            _model = model;
            _selectionService = selectionService;
            _timelineViewService = timelineViewService;
            _previewService = previewService;
            _trackEditingService = trackEditingService;
            _eventEditingService = eventEditingService;

            InitializeToolbar();
            InitializeInspector();
            InitializePreview();
            InitializeCommands();
        }

        /// <summary>再描画が必要な状態かどうか</summary>
        public bool RequiresRepaint => _playerProvider != null;

        /// <summary>
        /// クリップを開き直す要求時に発火する
        /// </summary>
        public event Action<SequenceClip, int, bool> OpenRequested;

        /// <summary>
        /// Inspector 変更後の軽量再読込要求時に発火する
        /// </summary>
        public event Action InspectorReloadRequested;

        /// <summary>
        /// View 状態を現在の Model に合わせて更新
        /// </summary>
        public void Refresh() {
            SequenceEditorGUI.TimeMode = _model.CurrentTimeMode;
            _view.TargetObjectField.value = _model.RootClip;
            _view.UpdateIncludeClipField(_model.RootClip, _model.IncludeClipIndex);
            UpdateToolbarState();
            UpdatePreview();
            _trackEditingService.RebuildPresentation();
            _view.UpdateInspector(_model.CurrentTimeMode, _selectionService.SelectedTargets);
        }

        /// <summary>
        /// Inspector を作り直さずに表示状態を更新
        /// </summary>
        public void RefreshWithoutInspector() {
            SequenceEditorGUI.TimeMode = _model.CurrentTimeMode;
            _view.TargetObjectField.value = _model.RootClip;
            _view.UpdateIncludeClipField(_model.RootClip, _model.IncludeClipIndex);
            UpdateToolbarState();
            UpdatePreview();
            _trackEditingService.RebuildPresentation();
            _view.InspectorView.TimeMode = _model.CurrentTimeMode;
        }

        /// <summary>
        /// OnGUI で必要な更新処理
        /// </summary>
        public void OnGui() {
            if (_model.ClipModel == null) {
                return;
            }

            UpdatePlayerProvider();
            UpdateSeekbar();
        }

        /// <summary>
        /// 現在の選択対象へフォーカスを移す
        /// </summary>
        internal void FocusSelection() {
            var target = _selectionService.SelectedTargets.LastOrDefault();
            if (target == null) {
                _view.Root.schedule.Execute(() => _view.Root.Focus());
                return;
            }

            FocusTarget(target);
        }

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
        /// ツールバーを初期化
        /// </summary>
        private void InitializeToolbar() {
            _view.CreateMenu.menu.AppendAction("Track", _ => {
                if (_model.ClipModel != null) {
                    _trackEditingService.CreateTrack();
                }
            });

            _view.PlayPauseToggle.RegisterValueChangedCallback(OnPlayPauseChanged);
            _view.PlayPauseToggle.style.display = DisplayStyle.None;

            _view.TargetObjectField.RegisterValueChangedCallback(evt => {
                OpenRequested?.Invoke(evt.newValue as SequenceClip, -1, true);
            });

            _view.IncludeClipField.style.display = DisplayStyle.None;
            _view.IncludeClipField.RegisterValueChangedCallback(_ => {
                OpenRequested?.Invoke(_model.RootClip, _view.IncludeClipField.index - 1, true);
            });

            _view.RulerModeField.choices = new List<string>(Enum.GetNames(typeof(SequenceEditorModel.TimeMode)));
            _view.RulerModeField.RegisterValueChangedCallback(_ => {
                _timelineViewService.SetTimeMode((SequenceEditorModel.TimeMode)_view.RulerModeField.index);
            });

            _view.TimeFitToggle.RegisterValueChangedCallback(evt => {
                _timelineViewService.SetTimeFit(evt.newValue);
            });

            _view.RefreshButton.clicked += () => OpenRequested?.Invoke(_model.RootClip, _model.IncludeClipIndex, true);

            _timelineViewService.SettingsChanged += UpdateToolbarState;
            _trackEditingService.TrackListChanged += UpdateToolbarState;
            _eventEditingService.EventListChanged += UpdateToolbarState;

            AddDisposable(new ActionDisposable(() => _timelineViewService.SettingsChanged -= UpdateToolbarState));
            AddDisposable(new ActionDisposable(() => _trackEditingService.TrackListChanged -= UpdateToolbarState));
            AddDisposable(new ActionDisposable(() => _eventEditingService.EventListChanged -= UpdateToolbarState));
            AddDisposable(new ActionDisposable(() => _view.PlayPauseToggle.UnregisterValueChangedCallback(OnPlayPauseChanged)));
        }

        /// <summary>
        /// Inspector 表示を初期化
        /// </summary>
        private void InitializeInspector() {
            _selectionService.SelectionChanged += UpdateInspectorSelection;
            _timelineViewService.SettingsChanged += UpdateInspectorTimeMode;
            _view.InspectorView.Changed += OnInspectorChanged;

            AddDisposable(new ActionDisposable(() => _selectionService.SelectionChanged -= UpdateInspectorSelection));
            AddDisposable(new ActionDisposable(() => _timelineViewService.SettingsChanged -= UpdateInspectorTimeMode));
            AddDisposable(new ActionDisposable(() => _view.InspectorView.Changed -= OnInspectorChanged));
        }

        /// <summary>
        /// Preview 表示を初期化
        /// </summary>
        private void InitializePreview() {
            _view.PreviewView.OnChangedClipEvent += OnChangedPreviewClip;
            _view.PreviewView.OnChangedOffsetTimeEvent += OnChangedPreviewOffsetTime;

            AddDisposable(new ActionDisposable(() => _view.PreviewView.OnChangedClipEvent -= OnChangedPreviewClip));
            AddDisposable(new ActionDisposable(() => _view.PreviewView.OnChangedOffsetTimeEvent -= OnChangedPreviewOffsetTime));
        }

        /// <summary>
        /// キー入力とエディタコマンドを初期化
        /// </summary>
        private void InitializeCommands() {
            _view.Root.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.F || evt.keyCode == KeyCode.A) {
                    if (_timelineViewService.SetBestTimeToSize(_view.TrackAreaView.layout.width - 20.0f)) {
                        _view.TrackScrollView.horizontalScroller.value = 0.0f;
                    }
                }
            });

            _view.Root.RegisterCallback<KeyDownEvent>(evt => {
                if (!evt.shiftKey || _model.ClipModel == null) {
                    return;
                }

                if (evt.keyCode == KeyCode.UpArrow) {
                    MoveSelectedEventsByKeyboard(ascending: true, offset: -1);
                    evt.StopPropagation();
                    evt.StopImmediatePropagation();
                }

                if (evt.keyCode == KeyCode.DownArrow) {
                    MoveSelectedEventsByKeyboard(ascending: false, offset: 1);
                    evt.StopPropagation();
                    evt.StopImmediatePropagation();
                }
            });

            _view.Root.RegisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            _view.Root.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);
            AddDisposable(new ActionDisposable(() => _view.Root.UnregisterCallback<ValidateCommandEvent>(OnValidateCommandEvent)));
            AddDisposable(new ActionDisposable(() => _view.Root.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent)));
        }

        /// <summary>
        /// Disposable を登録
        /// </summary>
        /// <param name="disposable">登録対象</param>
        private void AddDisposable(IDisposable disposable) {
            _disposables.Add(disposable);
        }

        /// <summary>
        /// ツールバー状態を更新
        /// </summary>
        private void UpdateToolbarState() {
            _view.UpdateToolbarState(_model.ClipModel != null, _model.CurrentTimeMode, _model.TimeFit);
        }

        /// <summary>
        /// Inspector の選択状態を更新
        /// </summary>
        private void UpdateInspectorSelection() {
            _view.InspectorView.SetTarget(_selectionService.SelectedTargets);
        }

        /// <summary>
        /// Inspector の時間モードを更新
        /// </summary>
        private void UpdateInspectorTimeMode() {
            _view.InspectorView.TimeMode = _model.CurrentTimeMode;
        }

        /// <summary>
        /// Inspector 変更を再読込要求へ変換
        /// </summary>
        private void OnInspectorChanged() {
            InspectorReloadRequested?.Invoke();
        }

        /// <summary>
        /// Preview 表示対象を更新
        /// </summary>
        private void UpdatePreview() {
            var (previewClip, offsetTime) = _previewService.LoadPreviewData(_model.CurrentClip, _model.RootClip);
            _view.UpdatePreview(previewClip, offsetTime);
        }

        /// <summary>
        /// プレイヤー提供元を更新
        /// </summary>
        private void UpdatePlayerProvider() {
            if (!Application.isPlaying) {
                _playerProviderOwner = null;
                _playerProvider = null;
                return;
            }

            var activeObject = UnityEditor.Selection.activeGameObject;
            if (activeObject == null || activeObject == _playerProviderOwner) {
                return;
            }

            var provider = activeObject.GetComponentInParent<ISequencePlayerProvider>();
            if (provider == null) {
                return;
            }

            _playerProviderOwner = activeObject;
            _playerProvider = provider;
        }

        /// <summary>
        /// シークバー位置を更新
        /// </summary>
        private void UpdateSeekbar() {
            if (_model.ClipModel?.Target == null) {
                return;
            }

            var seekTime = GetSeekTime(_model.ClipModel.Target);
            var visible = seekTime >= 0.0f;
            if (!visible) {
                _view.UpdateSeekbar(0.0f, false);
                return;
            }

            var left = seekTime * _model.TimeToSize - _view.TrackScrollView.horizontalScroller.value;
            _view.UpdateSeekbar(left, left >= 0.0f);
        }

        /// <summary>
        /// 現在の再生時間を取得
        /// </summary>
        /// <param name="clip">対象の SequenceClip</param>
        /// <returns>取得できた再生時間。取得できない場合は負値</returns>
        private float GetSeekTime(SequenceClip clip) {
            var sequencePlayer = _playerProvider?.SequencePlayer;
            var sequenceTime = sequencePlayer?.GetSequenceTime(clip) ?? -1.0f;
            if (sequenceTime >= 0.0f) {
                return sequenceTime;
            }

            return _view.PreviewView.IsValid ? _view.PreviewView.CurrentTime : -1.0f;
        }

        /// <summary>
        /// 再生ボタンの表示切り替えを反映
        /// </summary>
        /// <param name="evt">トグル変更イベント</param>
        private void OnPlayPauseChanged(ChangeEvent<bool> evt) {
            var playPauseToggle = (ToolbarToggle)evt.target;
            if (evt.newValue) {
                playPauseToggle.RemoveFromClassList("play_icon");
                playPauseToggle.AddToClassList("pause_icon");
            }
            else {
                playPauseToggle.RemoveFromClassList("pause_icon");
                playPauseToggle.AddToClassList("play_icon");
            }
        }

        /// <summary>
        /// エディタコマンドを受け付けるか検証
        /// </summary>
        /// <param name="evt">検証対象のコマンドイベント</param>
        private void OnValidateCommandEvent(ValidateCommandEvent evt) {
            if (_model.ClipModel == null) {
                return;
            }

            if (evt.commandName == "Duplicate" ||
                evt.commandName == "Delete" ||
                evt.commandName == "SoftDelete" ||
                evt.commandName == "Copy" ||
                evt.commandName == "Paste") {
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// エディタコマンドを現在の選択へ反映
        /// </summary>
        /// <param name="evt">実行対象のコマンドイベント</param>
        private void OnExecuteCommandEvent(ExecuteCommandEvent evt) {
            if (_model.ClipModel == null) {
                return;
            }

            if (evt.commandName == "Duplicate") {
                var duplicatedTargets = _eventEditingService.DuplicateEvents(GetSelectedEventModels())
                    .Select(x => x.Target)
                    .ToList();

                if (duplicatedTargets.Count > 0) {
                    _selectionService.RestoreSelection(duplicatedTargets);
                    FocusTarget(duplicatedTargets[^1]);
                }
            }
            else if (evt.commandName == "Delete" || evt.commandName == "SoftDelete") {
                _eventEditingService.DeleteEvents(GetSelectedEventModels());
            }
            else if (evt.commandName == "Copy") {
                _eventEditingService.CopySelectedEvents();
            }
            else if (evt.commandName == "Paste") {
                var trackModel = _model.ClipModel.TrackModels.LastOrDefault();
                if (trackModel != null) {
                    var pastedEventModels = _eventEditingService.PasteEvents(trackModel, EditorGUIUtility.systemCopyBuffer);
                    var pastedTargets = pastedEventModels
                        .Select(x => x.Target)
                        .ToArray();
                    if (pastedTargets.Length > 0) {
                        _selectionService.RestoreSelection(pastedTargets);
                        FocusTarget(pastedTargets[^1]);
                    }
                }
            }

            if (evt.commandName == "Duplicate" ||
                evt.commandName == "Delete" ||
                evt.commandName == "SoftDelete" ||
                evt.commandName == "Copy" ||
                evt.commandName == "Paste") {
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// プレビュー対象の変更を保存
        /// </summary>
        /// <param name="clip">選択された AnimationClip</param>
        private void OnChangedPreviewClip(AnimationClip clip) {
            var sequenceClip = _model.ClipModel?.Target;
            if (sequenceClip != null) {
                _previewService.SavePreviewData(sequenceClip, clip, _view.PreviewView.OffsetTime);
            }
        }

        /// <summary>
        /// プレビューのオフセット時間変更を保存
        /// </summary>
        /// <param name="offsetTime">変更後のオフセット時間</param>
        private void OnChangedPreviewOffsetTime(float offsetTime) {
            var sequenceClip = _model.ClipModel?.Target;
            if (sequenceClip != null) {
                _previewService.SavePreviewData(sequenceClip, _view.PreviewView.CurrentClip, offsetTime);
            }
        }

        /// <summary>
        /// 現在選択中の EventModel 一覧を取得
        /// </summary>
        /// <returns>現在有効な EventModel 一覧</returns>
        private SequenceEventModel[] GetSelectedEventModels() {
            var selectedTargets = _selectionService.SelectedTargets
                .OfType<SequenceEvent>()
                .ToArray();
            var selectedEventModels = new List<SequenceEventModel>(selectedTargets.Length);

            foreach (var selectedTarget in selectedTargets) {
                var selectedEventModel = _model.ClipModel?.FindEventModel(selectedTarget);
                if (selectedEventModel != null) {
                    selectedEventModels.Add(selectedEventModel);
                }
            }

            return selectedEventModels.ToArray();
        }

        /// <summary>
        /// 現在選択中の Event を並び順付きで取得
        /// </summary>
        /// <param name="ascending">昇順で取得する場合は true</param>
        /// <returns>並び順付きの選択 Event 一覧</returns>
        private SequenceEvent[] GetOrderedSelectedEventTargets(bool ascending) {
            var selectedTargets = _selectionService.SelectedTargets
                .OfType<SequenceEvent>()
                .Select(selectedTarget => new {
                    Target = selectedTarget,
                    Model = _model.ClipModel?.FindEventModel(selectedTarget),
                })
                .Where(x => x.Model != null);

            return ascending
                ? selectedTargets.OrderBy(x => x.Model.TrackModel.GetEventIndex(x.Model)).Select(x => x.Target).ToArray()
                : selectedTargets.OrderByDescending(x => x.Model.TrackModel.GetEventIndex(x.Model)).Select(x => x.Target).ToArray();
        }

        /// <summary>
        /// キーボード入力で選択中 Event を並び替える
        /// </summary>
        /// <param name="ascending">昇順で処理する場合は true</param>
        /// <param name="offset">移動量</param>
        private void MoveSelectedEventsByKeyboard(bool ascending, int offset) {
            var selectedTargets = GetOrderedSelectedEventTargets(ascending);
            if (selectedTargets.Length == 0) {
                return;
            }

            foreach (var selectedTarget in selectedTargets) {
                var selectedEvent = _model.ClipModel?.FindEventModel(selectedTarget);
                if (selectedEvent != null) {
                    _eventEditingService.MoveEvent(selectedEvent, selectedEvent.TrackModel.GetEventIndex(selectedEvent) + offset);
                }
            }

            _selectionService.RestoreSelection(selectedTargets);
            FocusTarget(selectedTargets[^1]);
        }

        /// <summary>
        /// 指定ターゲットに対応する View へフォーカスを移す
        /// </summary>
        /// <param name="target">フォーカス対象</param>
        private void FocusTarget(UnityEngine.Object target) {
            if (target == null) {
                return;
            }

            _view.Root.schedule.Execute(() => {
                var eventView = _view.Root.Query<SequenceEventView>()
                    .ToList()
                    .FirstOrDefault(x => Equals(x.userData, target));
                if (eventView != null) {
                    eventView.Focus();
                    return;
                }

                _view.Root.Focus();
            });
        }
    }
}
