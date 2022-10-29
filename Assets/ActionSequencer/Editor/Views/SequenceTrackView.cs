using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// Track用のView
    /// </summary>
    public class SequenceTrackView : VisualElement
    {
        private List<SequenceEventView> _eventViews = new List<SequenceEventView>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceTrackView() {
            AddToClassList("track_box");
        }
    }
}
