using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// RangeEvent用のView
    /// </summary>
    public class SequenceRangeEventView : SequenceEventView
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceRangeEventView()
            : base(true)
        {
            AddToClassList("range_event");
        }
    }
}
