using UnityEngine.UIElements;

namespace ActionSequencer.Editor.VisualElements {
    /// <summary>
    /// SplitView
    /// </summary>
    public class SplitView : TwoPaneSplitView {
        public new class UxmlFactory : UxmlFactory<SplitView, UxmlTraits> {
        }
    }
}