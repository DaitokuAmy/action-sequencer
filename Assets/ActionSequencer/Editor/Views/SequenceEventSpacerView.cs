using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceEvent用の空白View
    /// </summary>
    public class SequenceEventSpacerView : VisualElement
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceEventSpacerView()
        {
            focusable = false;
            AddToClassList("track__spacer");
        }
    }
}
