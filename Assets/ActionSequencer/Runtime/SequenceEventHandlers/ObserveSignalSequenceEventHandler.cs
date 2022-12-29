using System;

namespace ActionSequencer {
    /// <summary>
    /// 監視するだけのSequenceEventHandler
    /// </summary>
    public class ObserveSignalSequenceEventHandler<TEvent> : SignalSequenceEventHandler<TEvent>
        where TEvent : SignalSequenceEvent {
        private Action<TEvent> _invokeAction;

        /// <summary>
        /// InvokeActionの設定
        /// </summary>
        public void SetInvokeAction(Action<TEvent> invokeAction) {
            _invokeAction = invokeAction;
        }

        /// <summary>
        /// 発火処理
        /// </summary>
        protected override void OnInvoke(TEvent sequenceEvent) {
            _invokeAction?.Invoke(sequenceEvent);
        }
    }
}