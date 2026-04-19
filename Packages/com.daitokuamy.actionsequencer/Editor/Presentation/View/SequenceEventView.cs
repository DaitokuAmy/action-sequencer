using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SequenceEvent用のView基底
    /// </summary>
    public abstract class SequenceEventView : VisualElement {
        private bool _selected;
        private readonly Label _timelineTextLabel;
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

        /// <summary>Timeline 補助テキスト</summary>
        public string TimelineText {
            get => _timelineTextLabel.text;
            set {
                _timelineTextLabel.text = value;
                _timelineTextLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// <summary>Timeline 補助テキストの開始 offset</summary>
        public float TimelineTextOffset {
            set => _timelineTextLabel.style.left = value;
        }

        /// <summary>Timeline 補助テキストの表示色</summary>
        public Color TimelineTextColor {
            set => _timelineTextLabel.style.color = value;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="resizable">左右リサイズを許可する場合は true</param>
        public SequenceEventView(bool resizable) {
            focusable = true;
            style.overflow = Overflow.Visible;
            Manipulator = new SequenceEventManipulator(resizable);
            _contextualMenuManipulator = new ContextualMenuManipulator(OnOpenContextMenuInternal);
            _contextualMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Command });
            _contextualMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Control });

            _timelineTextLabel = new Label();
            _timelineTextLabel.AddToClassList("event__timeline_text");
            _timelineTextLabel.pickingMode = PickingMode.Ignore;
            _timelineTextLabel.style.display = DisplayStyle.None;
            Add(_timelineTextLabel);

            this.AddManipulator(Manipulator);
            this.AddManipulator(_contextualMenuManipulator);
        }

        /// <summary>
        /// Timeline 補助テキストを最前面へ移動
        /// </summary>
        protected void BringTimelineTextToFront() {
            _timelineTextLabel.BringToFront();
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
