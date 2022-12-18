using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// RangeEvent用のView
    /// </summary>
    public class RangeSequenceEventView : SequenceEventView
    {
        public new class UxmlFactory : UxmlFactory<RangeSequenceEventView, UxmlTraits> {
        }
        
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
