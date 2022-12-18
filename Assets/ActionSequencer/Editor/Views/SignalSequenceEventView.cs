using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SignalEvent用のView
    /// </summary>
    public class SignalSequenceEventView : SequenceEventView
    {
        public new class UxmlFactory : UxmlFactory<SignalSequenceEventView, UxmlTraits> {
        }
        
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
