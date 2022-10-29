namespace ActionSequencer.Editor
{
    /// <summary>
    /// SignalEvent用のView
    /// </summary>
    public class SequenceSignalEventView : SequenceEventView
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceSignalEventView()
            : base(false)
        {
            AddToClassList("signal_event");
        }
    }
}
