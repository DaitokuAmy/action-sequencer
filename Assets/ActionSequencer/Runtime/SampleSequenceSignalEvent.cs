using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// 単発実行用イベント
    /// </summary>
    [SequenceEvent("サンプル単発イベント")]
    public class SampleSequenceSignalEvent : SequenceSignalEvent
    {
        public string sampleText;
    }
    
    public class SampleSequenceSignalEventHandler : SequenceSignalEventHandler<SampleSequenceSignalEvent>
    {
        protected override void OnInvoke(SampleSequenceSignalEvent sequenceEvent)
        {
            Debug.Log(sequenceEvent.sampleText);
        }
    }
}