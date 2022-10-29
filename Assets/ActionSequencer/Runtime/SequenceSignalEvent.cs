using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// 単発実行用イベント
    /// </summary>
    public abstract class SequenceSignalEvent : SequenceEvent
    {
        [Tooltip("イベント発火時間")]
        public float time;
    }
}