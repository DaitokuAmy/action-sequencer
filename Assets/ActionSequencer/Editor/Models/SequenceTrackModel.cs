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
                    var model = new SignalSequenceEventModel(signalEvent);
                    _signalEventModels.Add(model);
                    OnAddedSignalEventModel?.Invoke(model);
                }
                else if (sequenceEvent is RangeSequenceEvent rangeEvent)
                {
                    var model = new RangeSequenceEventModel(rangeEvent);
                    _rangeEventModels.Add(model);
                    OnAddedRangeEventModel?.Invoke(model);
                }
            }
        }

        /// <summary>
        /// SignalEventの追加
        /// </summary>
        public SignalSequenceEventModel AddSignalEvent(SignalSequenceEvent @event)
        {
            SerializedObject.Update();
            _sequenceEvents.arraySize++;
            _sequenceEvents.GetArrayElementAtIndex(_sequenceEvents.arraySize - 1).objectReferenceValue = @event;
            SerializedObject.ApplyModifiedProperties();
            var model = new SignalSequenceEventModel(@event);
            _signalEventModels.Add(model);
            OnAddedSignalEventModel?.Invoke(model);

            if (_sequenceEvents.arraySize == 1)
            {
                RefreshDefaultLabel();
            }
            
            return model;
        }

        /// <summary>
        /// SignalEventの削除
        /// </summary>
        public void RemoveSignalEvent(SignalSequenceEvent @event)
        {
            var model = _signalEventModels.FirstOrDefault(x => x.Target == @event);
            if (model == null)
            {
                return;
            }
            
            SerializedObject.Update();
            for (var i = _sequenceEvents.arraySize - 1; i >= 0; i--)
            {
                var element = _sequenceEvents.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != model.Target)
                {
                    continue;
                }
                
                element.DeleteArrayElementAtIndex(i);
            }
            SerializedObject.ApplyModifiedProperties();
            
            OnRemoveSignalEventModel?.Invoke(model);
            _signalEventModels.Remove(model);
        }

        /// <summary>
        /// RangeEventの追加
        /// </summary>
        public RangeSequenceEventModel AddRangeEvent(RangeSequenceEvent @event)
        {
            SerializedObject.Update();
            _sequenceEvents.arraySize++;
            _sequenceEvents.GetArrayElementAtIndex(_sequenceEvents.arraySize - 1).objectReferenceValue = @event;
            SerializedObject.ApplyModifiedProperties();
            var model = new RangeSequenceEventModel(@event);
            _rangeEventModels.Add(model);
            OnAddedRangeEventModel?.Invoke(model);

            if (_sequenceEvents.arraySize == 1)
            {
                RefreshDefaultLabel();
            }
            
            return model;
        }

        /// <summary>
        /// RangeEventの削除
        /// </summary>
        public void RemoveRangeEvent(RangeSequenceEvent @event)
        {
            var model = _rangeEventModels.FirstOrDefault(x => x.Target == @event);
            if (model == null)
            {
                return;
            }
            
            SerializedObject.Update();
            for (var i = _sequenceEvents.arraySize - 1; i >= 0; i--)
            {
                var element = _sequenceEvents.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != model.Target)
                {
                    continue;
                }
                
                element.DeleteArrayElementAtIndex(i);
            }
            SerializedObject.ApplyModifiedProperties();
            
            OnRemoveRangeEventModel?.Invoke(model);
            _rangeEventModels.Remove(model);
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