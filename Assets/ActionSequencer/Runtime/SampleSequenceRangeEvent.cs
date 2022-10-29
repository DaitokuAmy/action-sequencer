using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// 範囲実行用イベント
    /// </summary>
    [SequenceEvent("サンプル範囲イベント")]
    public class SampleSequenceRangeEvent : SequenceRangeEvent
    {
        public string sampleStartText;
        public string sampleEndText;
    }
    
    public class SampleSequenceRangeEventHandler : SequenceRangeEventHandler<SampleSequenceRangeEvent>
    {
        protected override void OnEnter(SampleSequenceRangeEvent sequenceEvent)
        {
            Debug.Log(sequenceEvent.sampleStartText);
        }

        protected override void OnExit(SampleSequenceRangeEvent sequenceEvent)
        {
            Debug.Log(sequenceEvent.sampleEndText);
        }
    }
}