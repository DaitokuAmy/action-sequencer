using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceTrack用Model
    /// </summary>
    public class SequenceTrackModel : SerializedObjectModel
    {
        private SerializedProperty _label;
        private SerializedProperty _sequenceEvents;
        private List<SignalSequenceEventModel> _signalEventModels = new List<SignalSequenceEventModel>();
        private List<RangeSequenceEventModel> _rangeEventModels = new List<RangeSequenceEventModel>();
        private string _defaultLabel;

        public event Action<string> OnChangedLabel;
        public event Action<SignalSequenceEventModel> OnAddedSignalEventModel;
        public event Action<RangeSequenceEventModel> OnAddedRangeEventModel;
        public event Action<SignalSequenceEventModel> OnRemoveSignalEventModel;
        public event Action<RangeSequenceEventModel> OnRemoveRangeEventModel;
        
        public string Label
        {
            get => string.IsNullOrEmpty(_label.stringValue) ? _defaultLabel : _label.stringValue;
            set
            {
                SerializedObject.Update();
                _label.stringValue = value ?? "";
                SerializedObject.ApplyModifiedProperties();
                OnChangedLabel?.Invoke(Label);
                SetDirty();
            }
        }

        public int EventCount => _signalEventModels.Count + _rangeEventModels.Count;

        public IReadOnlyList<SignalSequenceEventModel> SignalEventModels => _signalEventModels;
        public IReadOnlyList<RangeSequenceEventModel> RangeEventModels => _rangeEventModels;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackModel(SequenceTrack target)
            : base(target)
        {
            _label = SerializedObject.FindProperty("label");
            _sequenceEvents = SerializedObject.FindProperty("sequenceEvents");
            
            // モデルの生成
            RefreshEvents();
            
            // Defaultのラベル名更新
            RefreshDefaultLabel();
            
            // 色を取得
            
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            ClearEventModels();
        }

        /// <summary>
        /// SequenceEventsの情報を再構築
        /// </summary>
        public void RefreshEvents()
        {
            ClearEventModels();
            
            for (var i = 0; i < _sequenceEvents.arraySize; i++)
            {
                var sequenceEvent = _sequenceEvents.GetArrayElementAtIndex(i).objectReferenceValue as SequenceEvent;
                if (sequenceEvent is SignalSequenceEvent signalEvent)
                {
                    var model = new SignalSequenceEventModel(signalEvent, this);
                    _signalEventModels.Add(model);
                    OnAddedSignalEventModel?.Invoke(model);
                }
                else if (sequenceEvent is RangeSequenceEvent rangeEvent)
                {
                    var model = new RangeSequenceEventModel(rangeEvent, this);
                    _rangeEventModels.Add(model);
                    OnAddedRangeEventModel?.Invoke(model);
                }
            }
        }

        /// <summary>
        /// Eventの追加
        /// </summary>
        public SequenceEventModel AddEvent(Type eventType)
        {
            if (eventType.IsSubclassOf(typeof(SignalSequenceEvent)))
            {
                return AddSignalEvent(eventType);
            }
            if (eventType.IsSubclassOf(typeof(RangeSequenceEvent)))
            {
                return AddRangeEvent(eventType);
            }

            return null;
        }

        /// <summary>
        /// Eventの削除
        /// </summary>
        public void RemoveEvent(SequenceEvent sequenceEvent)
        {
            if (sequenceEvent is RangeSequenceEvent rangeSequenceEvent)
            {
                RemoveRangeEvent(rangeSequenceEvent);
            }
            else if (sequenceEvent is SignalSequenceEvent signalSequenceEvent)
            {
                RemoveSignalEvent(signalSequenceEvent);
            }
        }

        /// <summary>
        /// Eventの複製
        /// </summary>
        public void DuplicateEvent(SequenceEvent sequenceEvent)
        {
            
        }

        /// <summary>
        /// SignalEventの追加
        /// </summary>
        private SignalSequenceEventModel AddSignalEvent(Type eventType)
        {
            if (!eventType.IsSubclassOf(typeof(SignalSequenceEvent)))
            {
                return null;
            }
            
            // 要素の追加
            var evt = CreateEventAsset<SignalSequenceEvent>(eventType);

            // Modelの生成
            var model = new SignalSequenceEventModel(evt, this);
            _signalEventModels.Add(model);
            OnAddedSignalEventModel?.Invoke(model);

            // 一つ目の要素だった場合、Trackのラベルを初期化
            if (_sequenceEvents.arraySize == 1)
            {
                RefreshDefaultLabel();
            }
            
            return model;
        }

        /// <summary>
        /// SignalEventの削除
        /// </summary>
        private void RemoveSignalEvent(SignalSequenceEvent sequenceEvent)
        {
            var model = _signalEventModels.FirstOrDefault(x => x.Target == sequenceEvent);
            if (model == null)
            {
                return;
            }
            
            // Modelの削除
            OnRemoveSignalEventModel?.Invoke(model);
            _signalEventModels.Remove(model);
            model.Dispose();

            // 要素削除
            DeleteEventAsset(sequenceEvent);
        }

        /// <summary>
        /// RangeEventの追加
        /// </summary>
        private RangeSequenceEventModel AddRangeEvent(Type eventType)
        {
            // 要素の追加
            var evt = CreateEventAsset<RangeSequenceEvent>(eventType);

            // Modelの生成
            var model = new RangeSequenceEventModel(evt, this);
            _rangeEventModels.Add(model);
            OnAddedRangeEventModel?.Invoke(model);

            // 一つ目の要素だった場合、Trackのラベルを初期化
            if (_sequenceEvents.arraySize == 1)
            {
                RefreshDefaultLabel();
            }

            return model;
        }

        /// <summary>
        /// RangeEventの削除
        /// </summary>
        private void RemoveRangeEvent(RangeSequenceEvent sequenceEvent)
        {
            var model = _rangeEventModels.FirstOrDefault(x => x.Target == sequenceEvent);
            if (model == null)
            {
                return;
            }
            
            // Modelの削除
            OnRemoveRangeEventModel?.Invoke(model);
            _rangeEventModels.Remove(model);
            model.Dispose();

            // 要素削除
            DeleteEventAsset(sequenceEvent);
        }

        /// <summary>
        /// 保持しているEventを全部削除
        /// </summary>
        public void RemoveEvents()
        {
            var signalSequenceEvents = _signalEventModels.Select(x => (SignalSequenceEvent)x.Target).ToArray();
            foreach (var sequenceEvent in signalSequenceEvents)
            {
                RemoveSignalEvent(sequenceEvent);
            }
            var rangeSequenceEvents = _rangeEventModels.Select(x => (RangeSequenceEvent)x.Target).ToArray();
            foreach (var sequenceEvent in rangeSequenceEvents)
            {
                RemoveRangeEvent(sequenceEvent);
            }
        }

        /// <summary>
        /// SequenceEvent用のModelを削除する(SerializedObjectからは除外しない)
        /// </summary>
        private void ClearEventModels()
        {
            foreach (var model in _signalEventModels)
            {
                OnRemoveSignalEventModel?.Invoke(model);
                model.Dispose();
            }

            foreach (var model in _rangeEventModels)
            {
                OnRemoveRangeEventModel?.Invoke(model);
                model.Dispose();
            }
            
            _signalEventModels.Clear();
            _rangeEventModels.Clear();
        }

        /// <summary>
        /// SequenceEventアセットの生成
        /// </summary>
        private TEvent CreateEventAsset<TEvent>(Type eventType)
            where TEvent : SequenceEvent
        {
            if (!eventType.IsSubclassOf(typeof(TEvent)))
            {
                return null;
            }
            
            // Assetの生成
            var evt = ScriptableObject.CreateInstance(eventType) as TEvent;
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
        /// SequenceEventアセットの削除
        /// </summary>
        private void DeleteEventAsset(SequenceEvent sequenceEvent)
        {
            // 要素から除外
            SerializedObject.Update();
            for (var i = _sequenceEvents.arraySize - 1; i >= 0; i--)
            {
                var element = _sequenceEvents.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != sequenceEvent)
                {
                    continue;
                }
                
                _sequenceEvents.DeleteArrayElementAtIndex(i);
            }
            SerializedObject.ApplyModifiedProperties();
            
            // Asset削除
            Undo.DestroyObjectImmediate(sequenceEvent);
        }

        /// <summary>
        /// Defaultのラベル名更新
        /// </summary>
        private void RefreshDefaultLabel()
        {
            var firstEvent = _signalEventModels.Select(x => x.Target as SequenceEvent).FirstOrDefault() ??
                             _rangeEventModels.Select(x => x.Target as SequenceEvent).FirstOrDefault();
            if (firstEvent != null)
            {
                var eventType = firstEvent.GetType();
                var sequenceEventAttr = eventType.GetCustomAttribute(typeof(SequenceEventAttribute)) as SequenceEventAttribute;
                _defaultLabel = sequenceEventAttr != null ? sequenceEventAttr.DisplayName : eventType.Name;
            }
            else
            {
                _defaultLabel = "Empty";
            }
            
            OnChangedLabel?.Invoke(Label);
        }

        /// <summary>
        /// 色の初期化
        /// </summary>
        private void SetupColor()
        {
            var firstEvent = _signalEventModels.Select(x => x.Target as SequenceEvent).FirstOrDefault() ??
                             _rangeEventModels.Select(x => x.Target as SequenceEvent).FirstOrDefault();
            if (firstEvent != null)
            {
                var eventType = firstEvent.GetType();
                var sequenceEventAttr = eventType.GetCustomAttribute(typeof(SequenceEventAttribute)) as SequenceEventAttribute;
                _defaultLabel = sequenceEventAttr != null ? sequenceEventAttr.DisplayName : eventType.Name;
            }
            else
            {
                _defaultLabel = "Empty";
            }
            
            OnChangedLabel?.Invoke(Label);
        }
    }
}