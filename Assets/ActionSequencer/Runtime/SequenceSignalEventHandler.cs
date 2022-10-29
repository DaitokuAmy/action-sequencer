namespace ActionSequencer
{
    /// <summary>
    /// SignalEventの処理を記述するためのInterface
    /// </summary>
    public interface ISequenceSignalEventHandler
    {
        /// <summary>
        /// イベント実行時処理
        /// </summary>
        void Invoke(SequenceSignalEvent sequenceEvent);
    }
    
    /// <summary>
    /// SignalEventの処理を記述するためのInterface
    /// </summary>
    public abstract class SequenceSignalEventHandler<TEvent> : ISequenceSignalEventHandler
        where TEvent : SequenceSignalEvent
    {
        /// <summary>
        /// イベント実行時処理
        /// </summary>
        void ISequenceSignalEventHandler.Invoke(SequenceSignalEvent sequenceEvent)
        {
            OnInvoke((TEvent)sequenceEvent);
        }

        /// <summary>
        /// イベント実行時処理
        /// </summary>
        /// <param name="sequenceEvent">対象のイベント</param>
        protected abstract void OnInvoke(TEvent sequenceEvent);
    }
}