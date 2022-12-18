namespace ActionSequencer {
    /// <summary>
    /// SignalEventの処理を記述するためのInterface
    /// </summary>
    public interface ISignalSequenceEventHandler {
        /// <summary>
        /// イベント実行時処理
        /// </summary>
        void Invoke(SignalSequenceEvent signalSequenceEvent);
    }

    /// <summary>
    /// SignalEventの処理を記述するためのInterface
    /// </summary>
    public abstract class SignalSequenceEventHandler<TEvent> : ISignalSequenceEventHandler
        where TEvent : SignalSequenceEvent {
        /// <summary>
        /// イベント実行時処理
        /// </summary>
        void ISignalSequenceEventHandler.Invoke(SignalSequenceEvent signalSequenceEvent) {
            OnInvoke((TEvent)signalSequenceEvent);
        }

        /// <summary>
        /// イベント実行時処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        protected abstract void OnInvoke(TEvent sequenceEvent);
    }
}