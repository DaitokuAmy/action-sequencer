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

        /// <summary>同フレーム終了でも1フレーム維持するか</summary>
        public virtual bool MustOneFrame => false;
        
        /// <summary>継続時間</summary>
        public float Duration => exitTime - enterTime;
    }
}