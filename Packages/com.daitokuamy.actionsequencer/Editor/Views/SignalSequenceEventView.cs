using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// SignalEvent用のView
    /// </summary>
    [UxmlElement]
    public sealed partial class SignalSequenceEventView : SequenceEventView {
        /// <summary>位置</summary>
        public float Position {
            get => style.marginLeft.value.value;
            set => style.marginLeft = value;
        }

        /// <summary>幅の指定</summary>
        public float Width {
            get => style.paddingRight.value.value;
            set => style.paddingRight = value;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SignalSequenceEventView()
            : base(false) {
            AddToClassList("signal_event");
        }
    }
}
