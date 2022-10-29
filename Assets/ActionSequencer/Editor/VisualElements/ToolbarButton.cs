using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor.VisualElements
{
    /// <summary>
    /// ToolbarButton
    /// </summary>
    public class ToolbarButton : UnityEditor.UIElements.ToolbarButton
    {
        public new class UxmlFactory : UxmlFactory<ToolbarButton, UxmlTraits> {}
    }
}
