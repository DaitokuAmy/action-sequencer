using UnityEngine;

namespace ActionSequencer {
    /// <summary>
    /// 単発実行用イベント
    /// </summary>
    public abstract class SignalSequenceEvent : SequenceEvent {
        /// <summary>GUIの見た目に反映させる時間</summary>
        public virtual float ViewDuration => 0.0f;
        
        [Tooltip("イベント発火時間"), FrameTime("frame")]
        public float time;
    }
}