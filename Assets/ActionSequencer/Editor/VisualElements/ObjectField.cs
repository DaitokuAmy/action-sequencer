using UnityEngine.UIElements;

namespace ActionSequencer.Editor.VisualElements
{
    /// <summary>
    /// ObjectField
    /// </summary>
    public class ObjectField : UnityEditor.UIElements.ObjectField
    {
        public new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits> {}
    }
}
