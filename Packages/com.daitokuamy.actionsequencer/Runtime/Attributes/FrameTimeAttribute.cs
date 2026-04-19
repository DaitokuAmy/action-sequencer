using UnityEngine;

namespace ActionSequencer {
    /// <summary>
    /// Frame表記可能な時間指定用Attribute
    /// </summary>
    public sealed class FrameTimeAttribute : PropertyAttribute {
        // Frame表示状態の名前
        public string FrameLabel { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FrameTimeAttribute(string frameLabel = "Frame") {
            FrameLabel = frameLabel;
        }
    }
}