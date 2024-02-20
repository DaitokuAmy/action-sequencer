using System;
using System.Collections.Generic;
using System.Linq;
using ActionSequencer.Editor.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceTrack用Model
    /// </summary>
    internal class SequenceTrackModel : SerializedObjectModel {
        private SerializedProperty _label;
        private SerializedProperty _sequenceEvents;
        private List<SequenceEventModel> _eventModels = new List<SequenceEventModel>();

        public Subject<string> ChangedLabelSubject { get; } = new Subject<string>();
        public Subject<SequenceEventModel> AddedEventModelSubject { get; } =
            new Subject<SequenceEventModel>();
        public Subject<SequenceEventModel> RemovedEventModelSubject { get; } =
            new Subject<SequenceEventModel>();
        public Subject MovedEventModelSubject { get; } =
            new Subject();
        public Subject ChangedEventTimeSubject { get; } = new Subject();

        public string Label {
            get => _label.stringValue;
            set {
                SerializedObject.Update();
                _label.stringValue = value;
                SerializedObject.ApplyModifiedProperties();
                ChangedLabelSubject.Invoke(Label);
                SetDirty();
            }
        }

        public IReadOnlyList<SequenceEventModel> EventModels => _eventModels;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackModel(SequenceTrack target)
            : base(target) {
            _label = SerializedObject.FindProperty("label");
            _sequenceEvents = SerializedObject.FindProperty("sequenceEvents");

            // 時間系の変化監視
            AddDisposable(AddedEventModelSubject
                .Subscribe(x => {
                    if (x is SignalSequenceEventModel signalEventModel) {
                        AddDisposable(
                            signalEventModel.ChangedTimeSubject.Subscribe(_ => ChangedEventTimeSubject.Invoke()));
                    }
                    else if (x is RangeSequenceEventModel rangeEventModel) {
                        AddDisposable(
                            rangeEventModel.ChangedEnterTimeSubject.Subscribe(_ => ChangedEventTimeSubject.Invoke()));
                        AddDisposable(
                            rangeEventModel.ChangedExitTimeSubject.Subscribe(_ => ChangedEventTimeSubject.Invoke()));
                    }
                }));

            // モデルの生成
            RefreshEvents();
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose() {
            base.Dispose();
            ClearEventModels();
        }

        /// <summary>
        /// SequenceEventsの情報を再構築
        /// </summary>
        public void RefreshEvents() {
            ClearEventModels();

            for (var i = 0; i < _sequenceEvents.arraySize; i++) {
                var sequenceEvent = _sequenceEvents.GetArrayElementAtIndex(i).objectReferenceValue as SequenceEvent;
                if (sequenceEvent is SignalSequenceEvent signalEvent) {
                    var model = new SignalSequenceEventModel(signalEvent, this);
                    _eventModels.Add(model);
                    AddedEventModelSubject.Invoke(model);
                }
                else if (sequenceEvent is RangeSequenceEvent rangeEvent) {
                    var model = new RangeSequenceEventModel(rangeEvent, this);
                    _eventModels.Add(model);
                    AddedEventModelSubject.Invoke(model);
                }
            }
        }

        /// <summary>
        /// イベントを移動
        /// </summary>
        public void MoveEvent(SequenceEventModel eventModel, int index) {
            var currentIndex = GetEventIndex(eventModel);
            if (currentIndex < 0 || index < 0 || index >= _eventModels.Count) {
                return;
            }

            _eventModels.RemoveAt(currentIndex);
            _eventModels.Insert(index, eventModel);

            // 状態を保存
            SerializedObject.Update();

            for (var i = 0; i < _eventModels.Count; i++) {
                _sequenceEvents.GetArrayElementAtIndex(i).objectReferenceValue = _eventModels[i].Target;
            }

            SerializedObject.ApplyModifiedProperties();

            // 通知
            MovedEventModelSubject.Invoke();
        }

        /// <summary>
        /// Eventを一つ上に移動
        /// </summary>
        public void MovePrevEvent(SequenceEventModel eventModel) {
            var currentIndex = GetEventIndex(eventModel);
            if (currentIndex <= 0) {
                return;
            }

            MoveEvent(eventModel, currentIndex - 1);
        }

        /// <summary>
        /// Eventを一つ下に移動
        /// </summary>
        public void MoveNextEvent(SequenceEventModel eventModel) {
            var currentIndex = GetEventIndex(eventModel);
            if (currentIndex < 0 || currentIndex >= _eventModels.Count - 1) {
                return;
            }

            MoveEvent(eventModel, currentIndex + 1);
        }

        /// <summary>
        /// EventのIndexを取得
        /// </summary>
        public int GetEventIndex(SequenceEventModel eventModel) {
            return _eventModels.IndexOf(eventModel);
        }

        /// <summary>
        /// EventModelの検索
        /// </summary>
        public SequenceEventModel FindEventModel(SequenceEvent sequenceEvent) {
            return _eventModels.FirstOrDefault(x => x.Target == sequenceEvent);
        }

        /// <summary>
        /// Eventの追加
        /// </summary>
        public SequenceEventModel AddEvent(Type eventType) {
            if (eventType.IsSubclassOf(typeof(SignalSequenceEvent))) {
                return AddSignalEvent(eventType);
            }

            if (eventType.IsSubclassOf(typeof(RangeSequenceEvent))) {
                return AddRangeEvent(eventType);
            }

            return null;
        }

        /// <summary>
        /// Eventの削除
        /// </summary>
        public bool RemoveEvent(SequenceEvent sequenceEvent) {
            var model = FindEventModel(sequenceEvent);
            if (model == null) {
                return false;
            }

            // Modelの削除
            _eventModels.Remove(model);

            // 要素削除
            DeleteEventAsset(sequenceEvent);

            // 通知
            RemovedEventModelSubject.Invoke(model);
            model.Dispose();
            return true;
        }

        /// <summary>
        /// 保持しているEventを全部削除
        /// </summary>
        public void RemoveEvents() {
            var models = _eventModels.ToArray();
            foreach (var model in models) {
                RemoveEvent(model.Target as SequenceEvent);
            }
        }

        /// <summary>
        /// Eventの複製
        /// </summary>
        public bool DuplicateEvent(SequenceEvent sequenceEvent) {
            var model = FindEventModel(sequenceEvent);
            if (model == null) {
                return false;
            }
            
            // 要素の追加
            var evt = DuplicateEventAsset(sequenceEvent);

            // Modelの生成
            var newModel = default(SequenceEventModel);
            if (evt is SignalSequenceEvent signalSequenceEvent) {
                newModel = new SignalSequenceEventModel(signalSequenceEvent, this);
            }
            else if (evt is RangeSequenceEvent rangeSequenceEvent) {
                newModel = new RangeSequenceEventModel(rangeSequenceEvent, this);
            }

            _eventModels.Add(newModel);
            AddedEventModelSubject.Invoke(newModel);
            return true;
        }

        /// <summary>
        /// Eventの貼り付け(CopyDataのjson)
        /// </summary>
        public void PasteEvents(string json) {
            var copyData = new CopyData();
            JsonUtility.FromJsonOverwrite(json, copyData);

            if (copyData.sequenceEvents == null || copyData.sequenceEvents.Length <= 0) {
                return;
            }

            foreach (var sequenceEvent in copyData.sequenceEvents) {
                if (sequenceEvent == null) {
                    continue;
                }
            
                // 要素の追加
                var evt = PasteEventAsset(sequenceEvent);

                // Modelの生成
                var model = default(SequenceEventModel);
                if (evt is SignalSequenceEvent signalSequenceEvent) {
                    model = new SignalSequenceEventModel(signalSequenceEvent, this);
                }
                else if (evt is RangeSequenceEvent rangeSequenceEvent) {
                    model = new RangeSequenceEventModel(rangeSequenceEvent, this);
                }

                _eventModels.Add(model);
                AddedEventModelSubject.Invoke(model);
            }
        }

        /// <summary>
        /// 含まれているEventをターゲットのTrackに移動する
        /// </summary>
        public void TransportEvent(SequenceEventModel eventModel, SequenceTrackModel target) {
            if (!_eventModels.Contains(eventModel)) {
                return;
            }

            // 参照場所を書き換える
            var sequenceEvent = eventModel.Target as SequenceEvent;
            RemoveEventAsset(sequenceEvent);
            _eventModels.Remove(eventModel);
            target.AddEventAsset(sequenceEvent);
            target._eventModels.Add(eventModel);

            // 通知
            RemovedEventModelSubject.Invoke(eventModel);
            target.AddedEventModelSubject.Invoke(eventModel);
        }

        /// <summary>
        /// SignalEventの追加
        /// </summary>
        private SignalSequenceEventModel AddSignalEvent(Type eventType) {
            if (!eventType.IsSubclassOf(typeof(SignalSequenceEvent))) {
                return null;
            }

            // 要素の追加
            var evt = CreateEventAsset<SignalSequenceEvent>(eventType);

            // Modelの生成
            var model = new SignalSequenceEventModel(evt, this);
            model.ResetLabel();
            _eventModels.Add(model);
            AddedEventModelSubject.Invoke(model);

            return model;
        }

        /// <summary>
        /// RangeEventの追加
        /// </summary>
        private RangeSequenceEventModel AddRangeEvent(Type eventType) {
            // 要素の追加
            var evt = CreateEventAsset<RangeSequenceEvent>(eventType);

            // Modelの生成
            var model = new RangeSequenceEventModel(evt, this);
            model.ResetLabel();
            _eventModels.Add(model);
            AddedEventModelSubject.Invoke(model);

            return model;
        }

        /// <summary>
        /// SequenceEvent用のModelを削除する(SerializedObjectからは除外しない)
        /// </summary>
        private void ClearEventModels() {
            foreach (var model in _eventModels) {
                RemovedEventModelSubject.Invoke(model);
                model.Dispose();
            }

            _eventModels.Clear();
        }

        /// <summary>
        /// SequenceEventアセットの生成
        /// </summary>
        private TEvent CreateEventAsset<TEvent>(Type eventType)
            where TEvent : SequenceEvent {
            if (!eventType.IsSubclassOf(typeof(TEvent))) {
                return null;
            }

            // Assetの生成
            var evt = ScriptableObject.CreateInstance(eventType) as TEvent;
            evt.name = eventType.Name;
            AssetDatabase.AddObjectToAsset(evt, Target);
            Undo.RegisterCreatedObjectUndo(evt, "Created Event");

            // 要素の追加
            SerializedObject.Update();
            _sequenceEvents.arraySize++;
            _sequenceEvents.GetArrayElementAtIndex(_sequenceEvents.arraySize - 1).objectReferenceValue = evt;
            SerializedObject.ApplyModifiedProperties();

            return evt;
        }

        /// <summary>
        /// SequenceEventアセットのコピー
        /// </summary>
        private TEvent DuplicateEventAsset<TEvent>(TEvent sourceEvent)
            where TEvent : SequenceEvent {
            // Index検索
            var index = -1;
            for (var i = 0; i < _sequenceEvents.arraySize; i++) {
                if (_sequenceEvents.GetArrayElementAtIndex(i).objectReferenceValue == sourceEvent) {
                    index = i;
                    break;
                }
            }

            if (index < 0) {
                return null;
            }
            
            // 同時に複数削除した際にReferenceが復帰しない不具合があったため、Undoを個別に登録
            var groupId = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();

            // Assetの生成
            var evt = Object.Instantiate(sourceEvent);
            evt.name = sourceEvent.name;
            AssetDatabase.AddObjectToAsset(evt, Target);
            Undo.RegisterCreatedObjectUndo(evt, "Instantiate Event");

            // 要素の追加
            SerializedObject.Update();
            _sequenceEvents.InsertArrayElementAtIndex(index + 1);
            _sequenceEvents.GetArrayElementAtIndex(index + 1).objectReferenceValue = evt;
            SerializedObject.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(groupId);

            return evt;
        }

        /// <summary>
        /// SequenceEventアセットの貼り付け
        /// </summary>
        private SequenceEvent PasteEventAsset(SequenceEvent sourceEvent) {
            // 末尾に挿入
            var index = _sequenceEvents.arraySize;
            
            // 同時に複数削除した際にReferenceが復帰しない不具合があったため、Undoを個別に登録
            var groupId = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();

            // Assetの生成
            var evt = Object.Instantiate(sourceEvent);
            evt.name = sourceEvent.name;
            AssetDatabase.AddObjectToAsset(evt, Target);
            Undo.RegisterCreatedObjectUndo(evt, "Paste Event");

            // 要素の追加
            SerializedObject.Update();
            _sequenceEvents.arraySize++;
            _sequenceEvents.GetArrayElementAtIndex(index).objectReferenceValue = evt;
            SerializedObject.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(groupId);

            return evt;
        }

        /// <summary>
        /// SequenceEventアセットの削除
        /// </summary>
        private void DeleteEventAsset(SequenceEvent sequenceEvent) {
            // 同時に複数削除した際にReferenceが復帰しない不具合があったため、Undoを個別に登録
            var groupId = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();

            // 要素から除外
            SerializedObject.Update();
            for (var i = _sequenceEvents.arraySize - 1; i >= 0; i--) {
                var element = _sequenceEvents.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != sequenceEvent) {
                    continue;
                }

                _sequenceEvents.DeleteArrayElementAtIndex(i);
            }
            
            // Eventを削除
            Undo.DestroyObjectImmediate(sequenceEvent);

            SerializedObject.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(groupId);
        }

        /// <summary>
        /// SequenceEventアセットの追加
        /// </summary>
        private void AddEventAsset(SequenceEvent sequenceEvent) {
            // 要素の追加
            SerializedObject.Update();
            _sequenceEvents.arraySize++;
            _sequenceEvents.GetArrayElementAtIndex(_sequenceEvents.arraySize - 1).objectReferenceValue = sequenceEvent;
            SerializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// SequenceEventアセットの除外（消しはしない）
        /// </summary>
        private void RemoveEventAsset(SequenceEvent sequenceEvent) {
            // 要素から除外
            SerializedObject.Update();
            for (var i = _sequenceEvents.arraySize - 1; i >= 0; i--) {
                var element = _sequenceEvents.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != sequenceEvent) {
                    continue;
                }

                _sequenceEvents.DeleteArrayElementAtIndex(i);
            }

            SerializedObject.ApplyModifiedProperties();
        }
    }
}