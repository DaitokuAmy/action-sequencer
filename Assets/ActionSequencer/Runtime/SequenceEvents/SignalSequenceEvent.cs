using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// 単発実行用イベント
    /// </summary>
    public abstract class SignalSequenceEvent : SequenceEvent
    {
        [Tooltip("イベント発火時間"), FrameTime("frame")]
        public float time;
    }
}