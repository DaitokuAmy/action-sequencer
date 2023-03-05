using System;
using UnityEngine;

namespace ActionSequencer {
    /// <summary>
    /// RangeEventの処理を記述するためのInterface
    /// </summary>
    public interface IRangeSequenceEventHandler {
        /// <summary>
        /// 開始済か
        /// </summary>
        bool IsEntered { get; }

        /// <summary>
        /// イベント開始時処理
        /// </summary>
        /// <param name="rangeSequenceEvent">対象のイベント</param>
        void Enter(RangeSequenceEvent rangeSequenceEvent);

        /// <summary>
        /// イベント終了時処理
        /// </summary>
        /// <param name="rangeSequenceEvent">対象のイベント</param>
        void Exit(RangeSequenceEvent rangeSequenceEvent);

        /// <summary>
        /// イベント中更新処理
        /// </summary>
        /// <param name="rangeSequenceEvent">対象のイベント</param>
        /// <param name="elapsedTime">開始からの経過時間</param>
        void Update(RangeSequenceEvent rangeSequenceEvent, float elapsedTime);

        /// <summary>
        /// イベント途中キャンセル時処理(Enterした後、外的要因で止められた場合)
        /// </summary>
        /// <param name="rangeSequenceEvent">対象のイベント</param>
        void Cancel(RangeSequenceEvent rangeSequenceEvent);
    }

    /// <summary>
    /// RangeEventの処理を記述するためのInterface
    /// </summary>
    public abstract class RangeSequenceEventHandler<TEvent> : IRangeSequenceEventHandler
        where TEvent : RangeSequenceEvent {
        private bool _isEntered = false;
        bool IRangeSequenceEventHandler.IsEntered => _isEntered;

        /// <summary>
        /// イベント開始時処理
        /// </summary>
        /// <param name="rangeSequenceEvent">対象のイベント</param>
        void IRangeSequenceEventHandler.Enter(RangeSequenceEvent rangeSequenceEvent) {
            _isEntered = true;
            OnEnter((TEvent)rangeSequenceEvent);
        }

        /// <summary>
        /// イベント終了時処理
        /// </summary>
        /// <param name="rangeSequenceEvent">対象のイベント</param>
        void IRangeSequenceEventHandler.Exit(RangeSequenceEvent rangeSequenceEvent) {
            OnExit((TEvent)rangeSequenceEvent);
            _isEntered = false;
        }

        /// <summary>
        /// イベント中更新処理
        /// </summary>
        /// <param name="rangeSequenceEvent">対象のイベント</param>
        /// <param name="elapsedTime">開始からの経過時間</param>
        void IRangeSequenceEventHandler.Update(RangeSequenceEvent rangeSequenceEvent, float elapsedTime) {
            OnUpdate((TEvent)rangeSequenceEvent, elapsedTime);
        }

        /// <summary>
        /// イベント途中キャンセル時処理(Enterした後、外的要因で止められた場合)
        /// </summary>
        /// <param name="rangeSequenceEvent">対象のイベント</param>
        void IRangeSequenceEventHandler.Cancel(RangeSequenceEvent rangeSequenceEvent) {
            OnCancel((TEvent)rangeSequenceEvent);
            _isEntered = false;
        }

        /// <summary>
        /// イベント開始時処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        protected virtual void OnEnter(TEvent sequenceEvent) {
        }

        /// <summary>
        /// イベント終了時処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        protected virtual void OnExit(TEvent sequenceEvent) {
        }

        /// <summary>
        /// イベント中更新処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        /// <param name="elapsedTime">開始からの経過時間</param>
        protected virtual void OnUpdate(TEvent sequenceEvent, float elapsedTime) {
        }

        /// <summary>
        /// イベント途中キャンセル時処理(Enterした後、外的要因で止められた場合)
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        protected virtual void OnCancel(TEvent sequenceEvent) {
        }
    }
}