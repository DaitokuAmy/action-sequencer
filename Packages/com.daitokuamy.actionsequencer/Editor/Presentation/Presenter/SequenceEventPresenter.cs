using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEvent 用 Presenter 基底
    /// </summary>
    internal abstract class SequenceEventPresenter : IDisposable {
        private enum DragMode {
            Timeline,
            Pending,
            Reorder
        }

        private const float MiddleDragDecisionThreshold = 6.0f;
        private const float VerticalReorderThreshold = 12.0f;
        private const float TimelineTextMargin = 4.0f;

        private bool _suppressSingleSelectionOnClick;
        private DragMode _dragMode = DragMode.Timeline;
        private float? _sharedDragMinStartTime;
        private SequenceEventManipulator.DragType _currentDragType;
        private SequenceEventManipulator.DragInfo _lastDragInfo;
        private readonly SequenceEditorModel _editorModel;
        private readonly SequenceTrackView _trackView;
        private readonly SelectionService _selectionService;
        private readonly TimelineViewService _timelineService;
        private readonly EventEditingService _eventEditingService;
        private readonly List<IDisposable> _disposables = new();
        private readonly SequenceTrackLabelElementView _labelElementView;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="model">対応する EventModel</param>
        /// <param name="view">対応する EventView</param>
        /// <param name="trackView">トラック要素 View</param>
        /// <param name="labelElementView">ラベル要素の View</param>
        /// <param name="editorModel">編集中のモデル</param>
        /// <param name="selectionService">選択状態を扱うサービス</param>
        /// <param name="timelineService">タイムライン設定を扱うサービス</param>
        /// <param name="eventEditingService">Event 編集サービス</param>
        protected SequenceEventPresenter(
            SequenceEventModel model,
            SequenceEventView view,
            SequenceTrackView trackView,
            SequenceTrackLabelElementView labelElementView,
            SequenceEditorModel editorModel,
            SelectionService selectionService,
            TimelineViewService timelineService,
            EventEditingService eventEditingService) {
            Model = model;
            View = view;
            _trackView = trackView;
            _editorModel = editorModel;
            _selectionService = selectionService;
            _timelineService = timelineService;
            _eventEditingService = eventEditingService;
            _labelElementView = labelElementView;

            _labelElementView.LabelColor = model.ThemeColor;
            _labelElementView.LabelChanged += OnChangedViewLabel;
            _labelElementView.OptionClicked += OnOptionClicked;
            View.ContextMenuOpening += OnContextMenuOpening;
            View.Manipulator.OnDragStart += OnDragStartInternal;
            View.Manipulator.OnDragging += OnDraggingInternal;
            View.Manipulator.OnDragExit += OnDragExitInternal;

            AddDisposable(new ActionDisposable(() => _labelElementView.LabelChanged -= OnChangedViewLabel));
            AddDisposable(new ActionDisposable(() => _labelElementView.OptionClicked -= OnOptionClicked));
            AddDisposable(new ActionDisposable(() => View.ContextMenuOpening -= OnContextMenuOpening));
            AddDisposable(new ActionDisposable(() => View.Manipulator.OnDragStart -= OnDragStartInternal));
            AddDisposable(new ActionDisposable(() => View.Manipulator.OnDragging -= OnDraggingInternal));
            AddDisposable(new ActionDisposable(() => View.Manipulator.OnDragExit -= OnDragExitInternal));

            AddChangedCallback<MouseDownEvent>(View, OnMouseDownEvent);
            AddChangedCallback<ClickEvent>(View, OnClickEvent);

            _selectionService.SelectionChanged += OnSelectionChanged;
            _eventEditingService.EventChanged += OnEventChanged;
            _timelineService.SettingsChanged += OnTimelineSettingsChanged;
            _eventEditingService.DragStarted += OnExternalDragStarted;
            _eventEditingService.Dragging += OnExternalDragging;

            AddDisposable(new ActionDisposable(() => _selectionService.SelectionChanged -= OnSelectionChanged));
            AddDisposable(new ActionDisposable(() => _eventEditingService.EventChanged -= OnEventChanged));
            AddDisposable(new ActionDisposable(() => _timelineService.SettingsChanged -= OnTimelineSettingsChanged));
            AddDisposable(new ActionDisposable(() => _eventEditingService.DragStarted -= OnExternalDragStarted));
            AddDisposable(new ActionDisposable(() => _eventEditingService.Dragging -= OnExternalDragging));

            Refresh();
        }

        /// <summary>対応する EventModel</summary>
        protected SequenceEventModel Model { get; }
        /// <summary>対応する EventView</summary>
        protected SequenceEventView View { get; }
        /// <summary>編集中のモデル</summary>
        protected SequenceEditorModel EditorModel => _editorModel;
        /// <summary>対応する TrackView</summary>
        protected SequenceTrackView TrackView => _trackView;
        /// <summary>選択状態を扱うサービス</summary>
        protected SelectionService SelectionService => _selectionService;
        /// <summary>タイムライン設定を扱うサービス</summary>
        protected TimelineViewService TimelineService => _timelineService;
        /// <summary>Event 編集サービス</summary>
        protected EventEditingService EventEditingService => _eventEditingService;

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
        protected void AddDisposable(IDisposable disposable) {
            _disposables.Add(disposable);
        }

        /// <summary>
        /// VisualElement の callback を登録
        /// </summary>
        /// <typeparam name="TEvent">監視するイベント型</typeparam>
        /// <param name="element">監視対象</param>
        /// <param name="callback">受信時の処理</param>
        protected void AddChangedCallback<TEvent>(VisualElement element, EventCallback<TEvent> callback)
            where TEvent : EventBase<TEvent>, new() {
            element.RegisterCallback(callback);
            AddDisposable(new ActionDisposable(() => element.UnregisterCallback(callback)));
        }

        /// <summary>
        /// ドラッグ開始時の状態を反映
        /// </summary>
        /// <param name="dragType">ドラッグ種別</param>
        /// <param name="otherEvent">他の選択 Event からの同期か</param>
        protected abstract void OnDragStart(SequenceEventManipulator.DragType dragType, bool otherEvent);
        /// <summary>
        /// ドラッグ中の状態を反映
        /// </summary>
        /// <param name="dragInfo">ドラッグ情報</param>
        /// <param name="otherEvent">他の選択 Event からの同期か</param>
        protected abstract void OnDragging(SequenceEventManipulator.DragInfo dragInfo, bool otherEvent);
        /// <summary>
        /// View の位置とサイズを更新
        /// </summary>
        protected abstract void RefreshGeometry();

        /// <summary>
        /// Timeline 補助テキストの表示開始 offset を返す
        /// </summary>
        /// <returns>表示開始 offset</returns>
        protected virtual float GetTimelineTextOffset() {
            return TimelineTextMargin;
        }

        /// <summary>
        /// Timeline 補助テキストの表示色を返す
        /// </summary>
        /// <returns>表示色</returns>
        protected virtual Color GetTimelineTextColor() {
            var color = Model.ThemeColor;
            var luminance = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            return luminance > 0.6f ? Color.black : Color.white;
        }

        /// <summary>
        /// 表示幅を時間へ変換
        /// </summary>
        /// <param name="position">表示幅</param>
        /// <returns>変換後の時間</returns>
        protected float SizeToTime(float position) {
            return position / _editorModel.TimeToSize;
        }

        /// <summary>
        /// 時間を表示幅へ変換
        /// </summary>
        /// <param name="time">時間</param>
        /// <returns>変換後の表示幅</returns>
        protected float TimeToSize(float time) {
            return time * _editorModel.TimeToSize;
        }

        /// <summary>
        /// View の表示を更新
        /// </summary>
        protected virtual void Refresh() {
            _labelElementView.SetLabel(Model.Label, Model.UsesDefaultLabel);
            View.Selected = _selectionService.SelectedTargets.Contains(Model.Target);
            View.style.backgroundColor = Model.Active ? Model.ThemeColor : Color.gray;
            RefreshGeometry();
            View.TimelineText = Model.TimelineText;
            View.TimelineTextOffset = GetTimelineTextOffset();
            View.TimelineTextColor = GetTimelineTextColor();
        }

        /// <summary>
        /// 選択状態変更時に View の選択表示を更新
        /// </summary>
        private void OnSelectionChanged() {
            View.Selected = _selectionService.SelectedTargets.Contains(Model.Target);
        }

        /// <summary>
        /// Event 情報変更時に表示全体を更新
        /// </summary>
        private void OnEventChanged() {
            Refresh();
        }

        /// <summary>
        /// タイムライン設定変更時にジオメトリのみ更新
        /// </summary>
        private void OnTimelineSettingsChanged() {
            RefreshGeometry();
        }

        /// <summary>
        /// ラベル変更をサービスへ反映
        /// </summary>
        /// <param name="label">変更後のラベル</param>
        private void OnChangedViewLabel(string label) {
            _eventEditingService.RenameEvent(Model, label);
        }

        /// <summary>
        /// Event オプションメニューを表示
        /// </summary>
        private void OnOptionClicked() {
            var menu = new GenericMenu();
            var currentIndex = Model.TrackModel.GetEventIndex(Model);

            menu.AddItem(new GUIContent("Up"), false, () => { _eventEditingService.MoveEvent(Model, currentIndex - 1); });
            menu.AddItem(new GUIContent("Down"), false, () => { _eventEditingService.MoveEvent(Model, currentIndex + 1); });
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Reset Label"), false, () => _eventEditingService.ResetLabel(Model));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Select Event"), false, () => _selectionService.SetSelectedTarget(Model.Target));
            menu.AddItem(new GUIContent("Duplicate Event"), false, () => { _eventEditingService.DuplicateEvent(Model); });
            menu.AddItem(new GUIContent("Delete Event"), false, () => { _eventEditingService.DeleteEvent(Model); });
            menu.AddItem(new GUIContent(Model.Active ? "Deactivate" : "Activate"), false,
                () => _eventEditingService.SetActive(Model, !Model.Active));

            foreach (var trackModel in _editorModel.ClipModel.TrackModels.Where(x => x.OwnerClip == Model.TrackModel.OwnerClip)) {
                var content = new GUIContent($"Move Event/{trackModel.OwnerTrackIndex}:{trackModel.Label}");
                if (trackModel == Model.TrackModel) {
                    menu.AddDisabledItem(content);
                    continue;
                }

                var targetTrack = trackModel;
                menu.AddItem(content, false, () => { _eventEditingService.MoveEvent(Model, targetTrack, targetTrack.EventModels.Count); });
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// クリック内容に応じて選択状態を更新
        /// </summary>
        /// <param name="evt">マウスイベント</param>
        private void OnMouseDownEvent(MouseDownEvent evt) {
            if (evt.button != 0 && evt.button != 1) {
                return;
            }

            if (View.Selected) {
                return;
            }

            if (evt.commandKey || evt.ctrlKey) {
                _selectionService.AddSelectedTarget(Model.Target);
            }
            else if (evt.shiftKey) {
                _selectionService.AddRangeSelectedTarget(Model.Target);
            }
            else {
                _selectionService.SetSelectedTarget(Model.Target);
            }
        }

        /// <summary>
        /// クリック時に必要なら単体選択へ戻す
        /// </summary>
        /// <param name="evt">クリックイベント</param>
        private void OnClickEvent(ClickEvent evt) {
            if (_suppressSingleSelectionOnClick) {
                _suppressSingleSelectionOnClick = false;
                return;
            }

            if (evt.button != 0 ||
                !View.Selected ||
                _selectionService.SelectedTargets.Count <= 1 ||
                evt.commandKey ||
                evt.ctrlKey ||
                evt.shiftKey) {
                return;
            }

            _selectionService.SetSelectedTarget(Model.Target);
        }

        /// <summary>
        /// コンテキストメニュー項目を構築
        /// </summary>
        /// <param name="evt">メニュー生成イベント</param>
        private void OnContextMenuOpening(ContextualMenuPopulateEvent evt) {
            evt.menu.AppendAction("Duplicate", _ => DuplicateSelectedEvents());
            evt.menu.AppendAction("Delete", _ => DeleteSelectedEvents());
            evt.menu.AppendAction("Copy", _ => _eventEditingService.CopySelectedEvents());
            evt.menu.AppendAction("Paste", _ => PasteEventsToTrack());
            evt.menu.AppendAction(Model.Active ? "Deactivate" : "Activate",
                _ => SetActiveSelectedEvents(!Model.Active));
        }

        /// <summary>
        /// ローカルドラッグ開始をサービスへ通知
        /// </summary>
        /// <param name="dragType">ドラッグ種別</param>
        private void OnDragStartInternal(SequenceEventManipulator.DragType dragType) {
            _currentDragType = dragType;
            _dragMode = ShouldDeferMiddleDrag(dragType) ? DragMode.Pending : DragMode.Timeline;
            _sharedDragMinStartTime = GetSharedDragMinStartTime(dragType);
            TrackView.HideReorderIndicator();
            OnDragStart(dragType, false);

            if (_dragMode == DragMode.Timeline) {
                _eventEditingService.NotifyDragStarted(Model, dragType);
            }
        }

        /// <summary>
        /// ローカルドラッグ更新をサービスへ通知
        /// </summary>
        /// <param name="dragInfo">ドラッグ情報</param>
        private void OnDraggingInternal(SequenceEventManipulator.DragInfo dragInfo) {
            _suppressSingleSelectionOnClick = true;

            var adjustedDragInfo = AdjustDragInfoForSelectionBounds(dragInfo);
            _lastDragInfo = adjustedDragInfo;

            if (_dragMode == DragMode.Pending) {
                if (ShouldStartVerticalReorder(adjustedDragInfo)) {
                    _dragMode = DragMode.Reorder;
                }
                else if (ShouldStartTimelineDrag(adjustedDragInfo)) {
                    _dragMode = DragMode.Timeline;
                    _eventEditingService.NotifyDragStarted(Model, _currentDragType);
                }
                else {
                    return;
                }
            }

            if (_dragMode == DragMode.Reorder) {
                UpdateReorderIndicator(adjustedDragInfo);
                return;
            }

            TrackView.HideReorderIndicator();
            OnDragging(adjustedDragInfo, false);
            _eventEditingService.NotifyDragging(Model, adjustedDragInfo);
        }

        /// <summary>
        /// ローカルドラッグ終了時に必要なら並び替えを確定
        /// </summary>
        /// <param name="dragType">ドラッグ種別</param>
        private void OnDragExitInternal(SequenceEventManipulator.DragType dragType) {
            if (_dragMode == DragMode.Reorder) {
                ReorderByVerticalDrag(_lastDragInfo);
                FocusEventTarget(Model.Target);
            }

            TrackView.HideReorderIndicator();

            _dragMode = DragMode.Timeline;
            _sharedDragMinStartTime = null;
            _currentDragType = dragType;
            _lastDragInfo = default;
        }

        /// <summary>
        /// 他の選択 Event からのドラッグ開始を同期
        /// </summary>
        /// <param name="sourceModel">ドラッグ元の EventModel</param>
        /// <param name="dragType">ドラッグ種別</param>
        private void OnExternalDragStarted(SequenceEventModel sourceModel, SequenceEventManipulator.DragType dragType) {
            if (sourceModel == Model || !View.Selected) {
                return;
            }

            _sharedDragMinStartTime = GetSharedDragMinStartTime(dragType);
            OnDragStart(dragType, true);
        }

        /// <summary>
        /// 他の選択 Event からのドラッグ更新を同期
        /// </summary>
        /// <param name="sourceModel">ドラッグ元の EventModel</param>
        /// <param name="dragInfo">ドラッグ情報</param>
        private void OnExternalDragging(SequenceEventModel sourceModel, SequenceEventManipulator.DragInfo dragInfo) {
            if (sourceModel == Model || !View.Selected) {
                return;
            }

            OnDragging(AdjustDragInfoForSelectionBounds(dragInfo), true);
        }

        /// <summary>
        /// 複数選択時の左端制約を考慮してドラッグ量を補正
        /// </summary>
        /// <param name="dragInfo">補正前のドラッグ情報</param>
        /// <returns>補正後のドラッグ情報</returns>
        private SequenceEventManipulator.DragInfo AdjustDragInfoForSelectionBounds(SequenceEventManipulator.DragInfo dragInfo) {
            if (dragInfo.Type != SequenceEventManipulator.DragType.Middle ||
                _selectionService.SelectedTargets.Count <= 1 ||
                !_sharedDragMinStartTime.HasValue) {
                return dragInfo;
            }

            var minStartTime = _sharedDragMinStartTime.Value;
            if (minStartTime <= 0.0f) {
                var clampedCurrentX = Mathf.Max(dragInfo.Current, dragInfo.Start);
                return Mathf.Approximately(clampedCurrentX, dragInfo.Current)
                    ? dragInfo
                    : new SequenceEventManipulator.DragInfo(
                        dragInfo.Type,
                        dragInfo.StartPosition,
                        new Vector2(clampedCurrentX, dragInfo.CurrentPosition.y));
            }

            var minDeltaX = -TimeToSize(minStartTime);
            var deltaX = dragInfo.Current - dragInfo.Start;
            if (deltaX >= minDeltaX) {
                return dragInfo;
            }

            return new SequenceEventManipulator.DragInfo(
                dragInfo.Type,
                dragInfo.StartPosition,
                new Vector2(dragInfo.Start + minDeltaX, dragInfo.CurrentPosition.y));
        }

        /// <summary>
        /// 複数選択 middle drag 用の共通 deltaTime を取得
        /// </summary>
        /// <param name="dragInfo">ドラッグ情報</param>
        /// <param name="deltaTime">共通 deltaTime</param>
        /// <returns>共通 deltaTime を使う場合は true</returns>
        protected bool TryGetSharedMiddleDragDeltaTime(SequenceEventManipulator.DragInfo dragInfo, out float deltaTime) {
            deltaTime = 0.0f;
            if (dragInfo.Type != SequenceEventManipulator.DragType.Middle || _selectionService.SelectedTargets.Count <= 1) {
                return false;
            }

            var clipModel = _editorModel.ClipModel;
            if (clipModel == null) {
                return false;
            }

            if (!_sharedDragMinStartTime.HasValue) {
                return false;
            }

            var minStartTime = _sharedDragMinStartTime.Value;
            var rawDeltaTime = SizeToTime(dragInfo.Current - dragInfo.Start);
            var edgeEpsilon = 1.0f / Mathf.Max(_editorModel.TimeToSize, 1.0f);
            if (rawDeltaTime <= -minStartTime + edgeEpsilon) {
                deltaTime = -minStartTime;
                return true;
            }

            var clampedDeltaTime = Mathf.Max(rawDeltaTime, -minStartTime);
            deltaTime = TimelineService.GetAbsorptionTime(minStartTime + clampedDeltaTime) - minStartTime;
            return true;
        }

        /// <summary>
        /// ドラッグ制約に使う開始時間を取得
        /// </summary>
        /// <param name="eventModel">対象の EventModel</param>
        /// <returns>開始時間</returns>
        private float GetDragStartTime(SequenceEventModel eventModel) {
            return eventModel switch {
                RangeSequenceEventModel rangeEventModel => rangeEventModel.EnterTime,
                SignalSequenceEventModel signalEventModel => signalEventModel.Time,
                _ => eventModel.GetStartTime(),
            };
        }

        /// <summary>
        /// 複数選択 middle drag 用の最小開始時間を取得
        /// </summary>
        /// <param name="dragType">ドラッグ種別</param>
        /// <returns>最小開始時間。不要な場合は null</returns>
        private float? GetSharedDragMinStartTime(SequenceEventManipulator.DragType dragType) {
            if (dragType != SequenceEventManipulator.DragType.Middle || _selectionService.SelectedTargets.Count <= 1) {
                return null;
            }

            var clipModel = _editorModel.ClipModel;
            if (clipModel == null) {
                return null;
            }

            var selectedEventModels = _selectionService.SelectedTargets
                .OfType<SequenceEvent>()
                .Select(clipModel.FindEventModel)
                .Where(x => x != null)
                .ToArray();
            if (selectedEventModels.Length <= 1) {
                return null;
            }

            return selectedEventModels.Min(GetDragStartTime);
        }

        /// <summary>
        /// 選択中 Event を複製
        /// </summary>
        private void DuplicateSelectedEvents() {
            var duplicatedTargets = _eventEditingService.DuplicateEvents(GetSelectedEventModels())
                .Select(x => x.Target)
                .ToList();

            if (duplicatedTargets.Count > 0) {
                _selectionService.RestoreSelection(duplicatedTargets);
                FocusEventTarget(duplicatedTargets[^1]);
            }
        }

        /// <summary>
        /// 現在の Track へ Event 群を貼り付け
        /// </summary>
        private void PasteEventsToTrack() {
            var pastedEventModels = _eventEditingService.PasteEvents(Model.TrackModel, EditorGUIUtility.systemCopyBuffer);
            var pastedTargets = pastedEventModels
                .Select(x => x.Target)
                .ToArray();

            if (pastedTargets.Length > 0) {
                _selectionService.RestoreSelection(pastedTargets);
                FocusEventTarget(pastedTargets[^1]);
            }
        }

        /// <summary>
        /// 選択中 Event を削除
        /// </summary>
        private void DeleteSelectedEvents() {
            _eventEditingService.DeleteEvents(GetSelectedEventModels());
        }

        /// <summary>
        /// 選択中 Event の active 状態をまとめて更新
        /// </summary>
        /// <param name="active">設定する active 状態</param>
        private void SetActiveSelectedEvents(bool active) {
            ApplyToSelectedEventModels(eventModel => _eventEditingService.SetActive(eventModel, active));
        }

        /// <summary>
        /// 選択中 Event へ処理を適用
        /// </summary>
        /// <param name="action">適用する処理</param>
        private void ApplyToSelectedEventModels(Action<SequenceEventModel> action) {
            foreach (var eventModel in GetSelectedEventModels()) {
                action(eventModel);
            }
        }

        /// <summary>
        /// 現在有効な選択中 EventModel 一覧を取得
        /// </summary>
        /// <returns>現在有効な EventModel 一覧</returns>
        private SequenceEventModel[] GetSelectedEventModels() {
            var selectedTargets = _selectionService.SelectedTargets
                .OfType<SequenceEvent>()
                .ToArray();
            var eventModels = new List<SequenceEventModel>(selectedTargets.Length);

            foreach (var selectedTarget in selectedTargets) {
                var eventModel = _editorModel.ClipModel?.FindEventModel(selectedTarget);
                if (eventModel == null) {
                    continue;
                }

                eventModels.Add(eventModel);
            }

            return eventModels.ToArray();
        }

        /// <summary>
        /// 指定 Event に対応する View へフォーカスを移す
        /// </summary>
        /// <param name="target">フォーカス対象の Event</param>
        private void FocusEventTarget(SequenceEvent target) {
            if (target == null) {
                return;
            }

            View.schedule.Execute(() => {
                var root = View.panel?.visualTree;
                var eventView = root?.Query<SequenceEventView>()
                    .ToList()
                    .FirstOrDefault(x => Equals(x.userData, target));
                eventView?.Focus();
            });
        }

        /// <summary>
        /// 中央ドラッグを保留判定にするか返す
        /// </summary>
        /// <param name="dragType">ドラッグ種別</param>
        /// <returns>保留判定にする場合は true</returns>
        private bool ShouldDeferMiddleDrag(SequenceEventManipulator.DragType dragType) {
            return dragType == SequenceEventManipulator.DragType.Middle &&
                   _selectionService.SelectedTargets.Count <= 1;
        }

        /// <summary>
        /// 縦ドラッグによる並び替えへ切り替えるか判定
        /// </summary>
        /// <param name="dragInfo">ドラッグ情報</param>
        /// <returns>並び替えを開始する場合は true</returns>
        private bool ShouldStartVerticalReorder(SequenceEventManipulator.DragInfo dragInfo) {
            var delta = dragInfo.CurrentPosition - dragInfo.StartPosition;
            var absX = Mathf.Abs(delta.x);
            var absY = Mathf.Abs(delta.y);
            return absY >= VerticalReorderThreshold && absY > absX * 1.25f;
        }

        /// <summary>
        /// 時間移動ドラッグへ切り替えるか判定
        /// </summary>
        /// <param name="dragInfo">ドラッグ情報</param>
        /// <returns>時間移動を開始する場合は true</returns>
        private bool ShouldStartTimelineDrag(SequenceEventManipulator.DragInfo dragInfo) {
            var delta = dragInfo.CurrentPosition - dragInfo.StartPosition;
            var absX = Mathf.Abs(delta.x);
            var absY = Mathf.Abs(delta.y);
            return absX >= MiddleDragDecisionThreshold && absX >= absY;
        }

        /// <summary>
        /// 縦ドラッグ結果を使って同一 Track 内の並び順を更新
        /// </summary>
        /// <param name="dragInfo">ドラッグ情報</param>
        private void ReorderByVerticalDrag(SequenceEventManipulator.DragInfo dragInfo) {
            var currentIndex = Model.TrackModel.GetEventIndex(Model);
            if (currentIndex < 0) {
                return;
            }

            var targetIndex = CalculateReorderTargetIndex(dragInfo);

            if (targetIndex == currentIndex) {
                return;
            }

            _eventEditingService.MoveEvent(Model, targetIndex);
            _selectionService.SetSelectedTarget(Model.Target);
        }

        /// <summary>
        /// 現在のポインタ位置からインジケータ表示用 index を計算
        /// </summary>
        /// <param name="dragInfo">ドラッグ情報</param>
        /// <returns>インジケータ表示用 index</returns>
        private int CalculateReorderIndicatorIndex(SequenceEventManipulator.DragInfo dragInfo) {
            var targetIndex = CalculateReorderTargetIndex(dragInfo);
            var currentIndex = Model.TrackModel.GetEventIndex(Model);
            if (currentIndex >= 0 && targetIndex > currentIndex) {
                return targetIndex + 1;
            }

            return targetIndex;
        }

        /// <summary>
        /// 現在のドラッグ位置に応じて並び替えインジケータを更新
        /// </summary>
        /// <param name="dragInfo">ドラッグ情報</param>
        private void UpdateReorderIndicator(SequenceEventManipulator.DragInfo dragInfo) {
            var currentIndex = Model.TrackModel.GetEventIndex(Model);
            var targetIndex = CalculateReorderTargetIndex(dragInfo);
            if (currentIndex == targetIndex) {
                TrackView.HideReorderIndicator();
                return;
            }

            TrackView.ShowReorderIndicator(CalculateReorderIndicatorIndex(dragInfo));
        }

        /// <summary>
        /// 現在のポインタ位置から差し込み予定 index を計算
        /// </summary>
        /// <param name="dragInfo">ドラッグ情報</param>
        /// <returns>差し込み予定 index</returns>
        private int CalculateReorderTargetIndex(SequenceEventManipulator.DragInfo dragInfo) {
            var parent = View.parent;
            if (parent == null) {
                return Model.TrackModel.GetEventIndex(Model);
            }

            var pointerY = dragInfo.CurrentPosition.y;
            var targetIndex = 0;
            for (var childIndex = 0; childIndex < parent.childCount; childIndex++) {
                if (parent[childIndex] is not SequenceEventView eventView || eventView == View) {
                    continue;
                }

                if (pointerY > eventView.worldBound.center.y) {
                    targetIndex++;
                }
            }

            return targetIndex;
        }
    }
}
