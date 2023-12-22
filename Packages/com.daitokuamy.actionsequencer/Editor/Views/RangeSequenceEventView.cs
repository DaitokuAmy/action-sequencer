using UnityEngine.UIElements;

namespace ActionSequencer.Editor {
    /// <summary>
    /// RangeEvent用のView
    /// </summary>
    public class RangeSequenceEventView : SequenceEventView {
        public new class UxmlFactory : UxmlFactory<RangeSequenceEventView, UxmlTraits> {
        }

        /// <summary>左端位置</summary>
        public float LeftPosition {
            get => style.marginLeft.value.value;
            set => style.marginLeft = value;
        }

        /// <summary>右端位置</summary>
        public float RightPosition {
            get => LeftPosition + style.width.value.value;
            set => style.width = value - LeftPosition;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public RangeSequenceEventView()
            : base(true) {
            AddToClassList("range_event");
        }
    }
}