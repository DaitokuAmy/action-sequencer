using System;
using ActionSequencer.Editor.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor
{
    /// <summary>
    /// SequenceEvent用のView基底
    /// </summary>
    public abstract class SequenceEventView : VisualElement
    {
        private bool _selected;
        private ContextualMenuManipulator _contextualMenuManipulator;

        public Subject<ContextualMenuPopulateEvent> OpenContextMenuSubject { get; } = new Subject<ContextualMenuPopulateEvent>();

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
            _contextualMenuManipulator = new ContextualMenuManipulator(OnOpenContextMenuInternal);
            _contextualMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Command});
            _contextualMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Control});
            this.AddManipulator(Manipulator);
            this.AddManipulator(_contextualMenuManipulator);
        }

        /// <summary>
        /// 右クリックメニュー開いた時の処理
        /// </summary>
        private void OnOpenContextMenuInternal(ContextualMenuPopulateEvent evt)
        {
            OpenContextMenuSubject.Invoke(evt);
        }
    }
}
