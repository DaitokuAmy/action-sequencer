using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// 単発実行用イベント
    /// </summary>
    [SequenceEvent("サンプル単発イベント")]
    public class SampleSignalSequenceEvent : SignalSequenceEvent
    {
        public string sampleText;
    }
    
    public class SampleSignalSequenceEventHandler : SignalSequenceEventHandler<SampleSignalSequenceEvent>
    {
        protected override void OnInvoke(SampleSignalSequenceEvent signalSequenceEvent)
        {
            Debug.Log(signalSequenceEvent.sampleText);
        }
    }
}