using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor.VisualElements {
    /// <summary>
    /// ToolbarMenu
    /// </summary>
    public class ToolbarMenu : UnityEditor.UIElements.ToolbarMenu {
        public new class UxmlFactory : UxmlFactory<ToolbarMenu, UxmlTraits> {
        }
    }
}