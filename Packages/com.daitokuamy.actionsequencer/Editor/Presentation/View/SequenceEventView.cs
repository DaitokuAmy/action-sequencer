using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEvent用のView基底
    /// </summary>
    public abstract class SequenceEventView : VisualElement {
        private bool _selected;
        private ContextualMenuManipulator _contextualMenuManipulator;

        /// <summary>コンテキストメニュー生成時に発火する</summary>
        public event Action<ContextualMenuPopulateEvent> ContextMenuOpening;

        /// <summary>操作用の Manipulator</summary>
        public SequenceEventManipulator Manipulator { get; private set; }

        /// <summary>選択状態</summary>
        public bool Selected {
            get => _selected;
            set {
                if (value == _selected) {
                    return;
                }

                _selected = value;

                if (_selected) {
                    AddToClassList("event--selected");
                }
                else {
                    RemoveFromClassList("event--selected");
                }
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="resizable">左右リサイズを許可する場合は true</param>
        public SequenceEventView(bool resizable) {
            focusable = true;
            Manipulator = new SequenceEventManipulator(resizable);
            _contextualMenuManipulator = new ContextualMenuManipulator(OnOpenContextMenuInternal);
            _contextualMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Command });
            _contextualMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Control });
            this.AddManipulator(Manipulator);
            this.AddManipulator(_contextualMenuManipulator);
        }

        /// <summary>
        /// 右クリックメニュー開いた時の処理
        /// </summary>
        /// <param name="evt">メニュー生成イベント</param>
        private void OnOpenContextMenuInternal(ContextualMenuPopulateEvent evt) {
            ContextMenuOpening?.Invoke(evt);
        }
    }
}