using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceEvent用のView基底
    /// </summary>
    public abstract class SequenceEventView : VisualElement
    {
        private bool _selected;

        public SequenceEventManipulator Manipulator { get; private set; }

        // 選択状態
        public bool Selected
        {
            get => _selected;
            set
            {
                if (value == _selected)
                {
                    return;
                }

                _selected = value;

                if (_selected)
                {
                    AddToClassList("event--selected");
                }
                else
                {
                    RemoveFromClassList("event--selected");
                }
            }
        }
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceEventView(bool resizable)
        {
            focusable = true;
            Manipulator = new SequenceEventManipulator(resizable);
            this.AddManipulator(Manipulator);
        }
    }
}
