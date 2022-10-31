using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// RangeEvent用のView
    /// </summary>
    public class RangeSequenceEventView : SequenceEventView
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RangeSequenceEventView()
            : base(true)
        {
            AddToClassList("range_event");
        }
    }
}
