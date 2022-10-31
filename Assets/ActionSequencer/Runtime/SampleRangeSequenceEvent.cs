using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// 範囲実行用イベント
    /// </summary>
    [SequenceEvent("サンプル範囲イベント")]
    public class SampleRangeSequenceEvent : RangeSequenceEvent
    {
        public string sampleStartText;
        public string sampleEndText;
    }
    
    public class SampleRangeSequenceEventHandler : RangeSequenceEventHandler<SampleRangeSequenceEvent>
    {
        protected override void OnEnter(SampleRangeSequenceEvent rangeSequenceEvent)
        {
            Debug.Log(rangeSequenceEvent.sampleStartText);
        }

        protected override void OnExit(SampleRangeSequenceEvent rangeSequenceEvent)
        {
            Debug.Log(rangeSequenceEvent.sampleEndText);
        }
    }
}