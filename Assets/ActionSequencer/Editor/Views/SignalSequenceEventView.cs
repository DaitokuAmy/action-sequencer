namespace ActionSequencer.Editor
{
    /// <summary>
    /// SignalEvent用のView
    /// </summary>
    public class SignalSequenceEventView : SequenceEventView
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SignalSequenceEventView()
            : base(false)
        {
            AddToClassList("signal_event");
        }
    }
}
