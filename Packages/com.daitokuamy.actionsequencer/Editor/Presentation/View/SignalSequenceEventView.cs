using UnityEngine;
using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SignalEvent用のView
    /// </summary>
    [UxmlElement]
    public sealed partial class SignalSequenceEventView : SequenceEventView {
        private const float StartDiameter = 14.0f;
        private const float BarLeft = 10.0f;
        private const float BarTop = 3.0f;
        private const float BarHeight = 8.0f;

        private readonly VisualElement _startView;
        private readonly VisualElement _barView;

        private float _tailWidth;

        /// <summary>位置</summary>
        public float Position {
            get => style.marginLeft.value.value;
            set => style.marginLeft = value;
        }

        /// <summary>幅の指定</summary>
        public float Width {
            get => _tailWidth;
            set {
                _tailWidth = Mathf.Max(0.0f, value);
                UpdateTailVisual();
            }
        }

        /// <summary>Signal の表示色</summary>
        public Color SignalColor {
            set {
                var barColor = new Color(value.r, value.g, value.b, value.a * 0.45f);

                _startView.style.backgroundColor = value;
                _barView.style.backgroundColor = barColor;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SignalSequenceEventView()
            : base(false) {
            AddToClassList("signal_event");

            _startView = CreatePart("signal_event__start");
            _barView = CreatePart("signal_event__bar");

            Add(_startView);
            Add(_barView);
            BringTimelineTextToFront();

            UpdateTailVisual();
        }

        /// <summary>
        /// Tail 表示を更新
        /// </summary>
        private void UpdateTailVisual() {
            style.width = StartDiameter + _tailWidth;

            var hasTail = _tailWidth > 0.0f;
            _barView.style.display = hasTail ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasTail) {
                return;
            }

            _barView.style.left = BarLeft;
            _barView.style.top = BarTop;
            _barView.style.width = _tailWidth;
            _barView.style.height = BarHeight;
        }

        /// <summary>
        /// Signal の構成要素を生成
        /// </summary>
        /// <param name="className">付与する class 名</param>
        /// <returns>生成した VisualElement</returns>
        private VisualElement CreatePart(string className) {
            var element = new VisualElement();
            element.AddToClassList(className);
            element.pickingMode = PickingMode.Ignore;
            return element;
        }
    }
}
