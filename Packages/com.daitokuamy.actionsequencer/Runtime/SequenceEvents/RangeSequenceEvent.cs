using UnityEngine;

namespace ActionSequencer {
    /// <summary>
    /// 範囲実行用イベント
    /// </summary>
    public abstract class RangeSequenceEvent : SequenceEvent {
        [Tooltip("開始時間"), FrameTime("enterFrame")]
        public float enterTime = 0.0f;

        [Tooltip("終了時間"), FrameTime("exitFrame")]
        public float exitTime = 0.5f;

        // トータル時間
        public float Duration => exitTime - enterTime;
    }
}