using UnityEngine.UIElements;

namespace ActionSequencer.Editor.VisualElements
{
    /// <summary>
    /// ToolbarToggle
    /// </summary>
    public class ToolbarToggle : UnityEditor.UIElements.ToolbarToggle
    {
        public new class UxmlFactory : UxmlFactory<ToolbarToggle, UxmlTraits> {}
    }
}
