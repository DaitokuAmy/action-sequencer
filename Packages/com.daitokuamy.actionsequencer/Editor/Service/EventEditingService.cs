using System;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor {
    /// <summary>
    /// Event 編集サービス
    /// </summary>
    internal sealed class EventEditingService {
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
        public EventEditingService(
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

        /// <summary>Event 一覧が変化したときに発火する</summary>
        public event Action EventListChanged;
        /// <summary>Event の表示内容が変化したときに発火する</summary>
        public event Action EventChanged;
        /// <summary>ドラッグ開始時に発火する</summary>
        public event Action<SequenceEventModel, SequenceEventManipulator.DragType> DragStarted;
        /// <summary>ドラッグ中の情報更新時に発火する</summary>
        public event Action<SequenceEventModel, SequenceEventManipulator.DragInfo> Dragging;

        /// <summary>
        /// Event を作成
        /// </summary>
        /// <param name="trackModel">作成先の TrackModel</param>
        /// <param name="eventType">作成する Event 型</param>
        /// <returns>作成後の EventModel</returns>
        public SequenceEventModel CreateEvent(SequenceTrackModel trackModel, Type eventType) {
            if (trackModel == null) {
                return null;
            }

            var sequenceEvent = _repository.CreateEvent(trackModel.Target, eventType);
            ReloadClipModel();
            RebuildPresentation();
            EventListChanged?.Invoke();
            return _model.ClipModel?.FindEventModel(sequenceEvent);
        }

        /// <summary>
        /// Event を削除
        /// </summary>
        /// <param name="eventModel">削除対象</param>
        public void DeleteEvent(SequenceEventModel eventModel) {
            DeleteEvents(new[] { eventModel });
        }

        /// <summary>
        /// Event 群を削除
        /// </summary>
        /// <param name="eventModels">削除対象の EventModel 一覧</param>
        public void DeleteEvents(SequenceEventModel[] eventModels) {
            if (eventModels == null || eventModels.Length == 0) {
                return;
            }

            var targetEvents = eventModels
                .Where(x => x != null)
                .Select(x => x.Target)
                .ToArray();
            if (targetEvents.Length == 0) {
                return;
            }

            _repository.DeleteEvents(targetEvents);
            ReloadClipModel();
            RebuildPresentation();
            EventListChanged?.Invoke();
        }

        /// <summary>
        /// Event を複製
        /// </summary>
        /// <param name="eventModel">複製元</param>
        /// <returns>複製後の EventModel</returns>
        public SequenceEventModel DuplicateEvent(SequenceEventModel eventModel) {
            return DuplicateEvents(new[] { eventModel }).FirstOrDefault();
        }

        /// <summary>
        /// Event 群を複製
        /// </summary>
        /// <param name="eventModels">複製元の EventModel 一覧</param>
        /// <returns>複製後の EventModel 一覧</returns>
        public SequenceEventModel[] DuplicateEvents(SequenceEventModel[] eventModels) {
            if (eventModels == null || eventModels.Length == 0) {
                return Array.Empty<SequenceEventModel>();
            }

            var duplicatedEvents = eventModels
                .Where(x => x != null)
                .GroupBy(x => x.TrackModel.Target)
                .OrderBy(x => _model.ClipModel.GetTrackIndex(x.First().TrackModel))
                .SelectMany(group => {
                    var orderedEventModels = group
                        .OrderBy(x => x.TrackModel.GetEventIndex(x))
                        .Select(x => x.Target)
                        .ToArray();
                    return _repository.DuplicateEvents(group.Key, orderedEventModels);
                })
                .ToArray();

            ReloadClipModel();
            RebuildPresentation();
            EventListChanged?.Invoke();
            return duplicatedEvents
                .Select(x => _model.ClipModel?.FindEventModel(x))
                .Where(x => x != null)
                .ToArray();
        }

        /// <summary>
        /// Event 群を貼り付け
        /// </summary>
        /// <param name="trackModel">貼り付け先の TrackModel</param>
        /// <param name="json">コピー済み JSON</param>
        /// <returns>貼り付け後の EventModel 一覧</returns>
        public SequenceEventModel[] PasteEvents(SequenceTrackModel trackModel, string json) {
            if (trackModel == null) {
                return Array.Empty<SequenceEventModel>();
            }

            var pastedEvents = _repository.PasteEvents(trackModel.Target, json);
            ReloadClipModel();
            RebuildPresentation();
            EventListChanged?.Invoke();
            return pastedEvents
                .Select(x => _model.ClipModel?.FindEventModel(x))
                .Where(x => x != null)
                .ToArray();
        }

        /// <summary>
        /// Event を別 Track へ移動
        /// </summary>
        /// <param name="eventModel">移動対象</param>
        /// <param name="targetTrackModel">移動先の TrackModel</param>
        /// <param name="targetIndex">移動先 index</param>
        public void MoveEvent(SequenceEventModel eventModel, SequenceTrackModel targetTrackModel, int targetIndex) {
            if (eventModel == null || targetTrackModel == null) {
                return;
            }

            _repository.MoveEvent(eventModel.TrackModel.Target, targetTrackModel.Target, eventModel.Target, targetIndex);
            ReloadClipModel();
            RebuildPresentation();
            EventListChanged?.Invoke();
        }

        /// <summary>
        /// Event を同一 Track 内で移動
        /// </summary>
        /// <param name="eventModel">移動対象</param>
        /// <param name="targetIndex">移動先 index</param>
        public void MoveEvent(SequenceEventModel eventModel, int targetIndex) {
            if (eventModel == null) {
                return;
            }

            _repository.MoveEvent(eventModel.TrackModel.Target, eventModel.Target, targetIndex);
            ReloadClipModel();
            RebuildPresentation();
            EventListChanged?.Invoke();
        }

        /// <summary>
        /// Event ラベルを変更
        /// </summary>
        /// <param name="eventModel">変更対象</param>
        /// <param name="label">変更後のラベル</param>
        public void RenameEvent(SequenceEventModel eventModel, string label) {
            if (eventModel == null || eventModel.Label == label) {
                return;
            }

            _repository.RenameEvent(eventModel.Target, label);
            if (eventModel.SetLabel(label)) {
                EventChanged?.Invoke();
            }
        }

        /// <summary>
        /// Event ラベルを既定値へ戻す
        /// </summary>
        /// <param name="eventModel">変更対象</param>
        public void ResetLabel(SequenceEventModel eventModel) {
            if (eventModel == null) {
                return;
            }

            var label = SequenceEditorUtility.GetDisplayName(eventModel.Target.GetType());
            RenameEvent(eventModel, label);
        }

        /// <summary>
        /// Event の有効状態を変更
        /// </summary>
        /// <param name="eventModel">変更対象</param>
        /// <param name="active">変更後の状態</param>
        public void SetActive(SequenceEventModel eventModel, bool active) {
            if (eventModel == null || eventModel.Active == active) {
                return;
            }

            _repository.SetEventActive(eventModel.Target, active);
            if (eventModel.SetActive(active)) {
                EventChanged?.Invoke();
            }
        }

        /// <summary>
        /// SignalEvent の時間を変更
        /// </summary>
        /// <param name="eventModel">変更対象</param>
        /// <param name="time">変更後の時間</param>
        public void SetSignalTime(SignalSequenceEventModel eventModel, float time) {
            if (eventModel == null) {
                return;
            }

            var nextTime = Mathf.Max(0.0f, time);
            if (Mathf.Approximately(eventModel.Time, nextTime)) {
                return;
            }

            _repository.SetSignalTime(eventModel.Target, time);
            if (eventModel.SetTime(time)) {
                EventChanged?.Invoke();
            }
        }

        /// <summary>
        /// RangeEvent の時間範囲を変更
        /// </summary>
        /// <param name="eventModel">変更対象</param>
        /// <param name="enterTime">開始時間</param>
        /// <param name="exitTime">終了時間</param>
        public void SetRangeTimes(RangeSequenceEventModel eventModel, float enterTime, float exitTime) {
            if (eventModel == null) {
                return;
            }

            var nextEnterTime = Mathf.Clamp(enterTime, 0.0f, exitTime);
            var nextExitTime = Mathf.Max(exitTime, nextEnterTime);
            if (Mathf.Approximately(eventModel.EnterTime, nextEnterTime) &&
                Mathf.Approximately(eventModel.ExitTime, nextExitTime)) {
                return;
            }

            _repository.SetRangeTimes(eventModel.Target, enterTime, exitTime);
            var changed = eventModel.SetEnterTime(enterTime);
            changed |= eventModel.SetExitTime(exitTime);
            if (changed) {
                EventChanged?.Invoke();
            }
        }

        /// <summary>
        /// Duration を維持したまま RangeEvent を移動
        /// </summary>
        /// <param name="eventModel">変更対象</param>
        /// <param name="enterTime">開始時間</param>
        /// <param name="exitTime">終了時間</param>
        public void MoveRangeKeepingDuration(RangeSequenceEventModel eventModel, float enterTime, float exitTime) {
            if (eventModel == null) {
                return;
            }

            var nextEnterTime = Mathf.Max(0.0f, enterTime);
            var nextExitTime = Mathf.Max(exitTime, nextEnterTime);
            if (Mathf.Approximately(eventModel.EnterTime, nextEnterTime) &&
                Mathf.Approximately(eventModel.ExitTime, nextExitTime)) {
                return;
            }

            _repository.SetRangeTimes(eventModel.Target, nextEnterTime, nextExitTime);
            if (eventModel.MoveEnterTime(nextEnterTime, nextExitTime)) {
                EventChanged?.Invoke();
            }
        }

        /// <summary>
        /// 現在選択中の Event をコピー
        /// </summary>
        public void CopySelectedEvents() {
            var events = _selectionService.SelectedTargets
                .OfType<SequenceEvent>()
                .ToArray();
            var copyData = new CopyData(events);
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(copyData);
        }

        /// <summary>
        /// ドラッグ開始を通知
        /// </summary>
        /// <param name="eventModel">ドラッグ元の EventModel</param>
        /// <param name="dragType">ドラッグ種別</param>
        public void NotifyDragStarted(SequenceEventModel eventModel, SequenceEventManipulator.DragType dragType) {
            DragStarted?.Invoke(eventModel, dragType);
        }

        /// <summary>
        /// ドラッグ中の更新を通知
        /// </summary>
        /// <param name="eventModel">ドラッグ元の EventModel</param>
        /// <param name="dragInfo">ドラッグ情報</param>
        public void NotifyDragging(SequenceEventModel eventModel, SequenceEventManipulator.DragInfo dragInfo) {
            Dragging?.Invoke(eventModel, dragInfo);
        }

        /// <summary>
        /// 現在の Model に合わせて Event Presentation を再構築
        /// </summary>
        internal void RebuildPresentation() {
            _eventPresentationCoordinator?.Clear();
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
