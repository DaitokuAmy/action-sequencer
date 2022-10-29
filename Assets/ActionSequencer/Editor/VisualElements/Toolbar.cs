using UnityEngine.UIElements;

namespace ActionSequencer.Editor.VisualElements
{
    /// <summary>
    /// Toolbar
    /// </summary>
    public class Toolbar : UnityEditor.UIElements.Toolbar
    {
        public new class UxmlFactory : UxmlFactory<Toolbar, UxmlTraits> {}
    }
}
