using System;

namespace ActionSequencer {
    /// <summary>
    /// 監視するだけのSequenceEventHandler
    /// </summary>
    public class ObserveRangeSequenceEventHandler<TEvent> : RangeSequenceEventHandler<TEvent>
        where TEvent : RangeSequenceEvent {
        private Action<TEvent> _enterAction;
        private Action<TEvent, float> _updateAction;
        private Action<TEvent> _exitAction;
        private Action<TEvent> _cancelAction;

        /// <summary>
        /// EnterActionの設定
        /// </summary>
        public void SetEnterAction(Action<TEvent> enterAction) {
            _enterAction = enterAction;
        }

        /// <summary>
        /// UpdateActionの設定
        /// </summary>
        public void SetUpdateAction(Action<TEvent, float> updateAction) {
            _updateAction = updateAction;
        }

        /// <summary>
        /// ExitActionの設定
        /// </summary>
        public void SetExitAction(Action<TEvent> exitAction) {
            _exitAction = exitAction;
        }

        /// <summary>
        /// CancelActionの設定
        /// </summary>
        public void SetCancelAction(Action<TEvent> cancelAction) {
            _cancelAction = cancelAction;
        }

        /// <summary>
        /// Event開始時処理
        /// </summary>
        protected override void OnEnter(TEvent sequenceEvent) {
            _enterAction?.Invoke(sequenceEvent);
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        protected override void OnUpdate(TEvent sequenceEvent, float elapsedTime) {
            _updateAction?.Invoke(sequenceEvent, elapsedTime);
        }

        /// <summary>
        /// Event終了時処理
        /// </summary>
        protected override void OnExit(TEvent sequenceEvent) {
            _exitAction?.Invoke(sequenceEvent);
        }

        /// <summary>
        /// Eventキャンセル時処理
        /// </summary>
        protected override void OnCancel(TEvent sequenceEvent) {
            _cancelAction?.Invoke(sequenceEvent);
        }
    }
}