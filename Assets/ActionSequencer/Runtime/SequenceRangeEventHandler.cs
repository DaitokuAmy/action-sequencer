using System;
using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// RangeEventの処理を記述するためのInterface
    /// </summary>
    public interface ISequenceRangeEventHandler
    {
        /// <summary>
        /// 開始済か
        /// </summary>
        bool IsEntered { get; }
        
        /// <summary>
        /// イベント開始時処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        void Enter(SequenceRangeEvent sequenceEvent);
        
        /// <summary>
        /// イベント終了時処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        void Exit(SequenceRangeEvent sequenceEvent);

        /// <summary>
        /// イベント中更新処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        /// <param name="elapsedTime">開始からの経過時間</param>
        void Update(SequenceRangeEvent sequenceEvent, float elapsedTime);
        
        /// <summary>
        /// イベント途中キャンセル時処理(Enterした後、外的要因で止められた場合)
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        void Cancel(SequenceRangeEvent sequenceEvent);
    }
    
    /// <summary>
    /// RangeEventの処理を記述するためのInterface
    /// </summary>
    public abstract class SequenceRangeEventHandler<TEvent> : ISequenceRangeEventHandler
        where TEvent : SequenceRangeEvent
    {
        private bool _isEntered = false;
        bool ISequenceRangeEventHandler.IsEntered => _isEntered;
        
        /// <summary>
        /// イベント開始時処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        void ISequenceRangeEventHandler.Enter(SequenceRangeEvent sequenceEvent)
        {
            _isEntered = true;
            OnEnter((TEvent)sequenceEvent);
        }

        /// <summary>
        /// イベント終了時処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        void ISequenceRangeEventHandler.Exit(SequenceRangeEvent sequenceEvent)
        {
            OnExit((TEvent)sequenceEvent);
            _isEntered = false;
        }

        /// <summary>
        /// イベント中更新処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        /// <param name="elapsedTime">開始からの経過時間</param>
        void ISequenceRangeEventHandler.Update(SequenceRangeEvent sequenceEvent, float elapsedTime)
        {
            OnUpdate((TEvent)sequenceEvent, elapsedTime);
        }

        /// <summary>
        /// イベント途中キャンセル時処理(Enterした後、外的要因で止められた場合)
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        void ISequenceRangeEventHandler.Cancel(SequenceRangeEvent sequenceEvent)
        {
            OnCancel((TEvent)sequenceEvent);
            _isEntered = false;
        }

        /// <summary>
        /// イベント開始時処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        protected virtual void OnEnter(TEvent sequenceEvent) {}

        /// <summary>
        /// イベント終了時処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        protected virtual void OnExit(TEvent sequenceEvent) {}

        /// <summary>
        /// イベント中更新処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        /// <param name="elapsedTime">開始からの経過時間</param>
        protected virtual void OnUpdate(TEvent sequenceEvent, float elapsedTime) {}

        /// <summary>
        /// イベント途中キャンセル時処理(Enterした後、外的要因で止められた場合)
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        protected virtual void OnCancel(TEvent sequenceEvent) {}
    }
}