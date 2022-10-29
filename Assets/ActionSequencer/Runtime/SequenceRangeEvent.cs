using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// 範囲実行用イベント
    /// </summary>
    public abstract class SequenceRangeEvent : SequenceEvent
    {
        [Tooltip("開始時間")]
        public float enterTime;
        [Tooltip("終了時間")]
        public float exitTime;

        // トータル時間
        public float Duration => exitTime - enterTime;
    }
}