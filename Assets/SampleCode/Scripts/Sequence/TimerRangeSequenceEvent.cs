using System.Diagnostics;
using ActionSequencer;
using Debug = UnityEngine.Debug;

[SequenceEvent("時間計測", "#FF8888")]
public class TimerRangeSequenceEvent : RangeSequenceEvent
{
}

public class TimerRangeSequenceEventHandler : RangeSequenceEventHandler<TimerRangeSequenceEvent>
{
    private Stopwatch _stopwatch = new Stopwatch();
    
    protected override void OnEnter(TimerRangeSequenceEvent sequenceEvent)
    {
        _stopwatch.Restart();
    }

    protected override void OnExit(TimerRangeSequenceEvent sequenceEvent)
    {
        _stopwatch.Stop();
        Debug.Log($"Time:{_stopwatch.Elapsed.TotalSeconds:0.000}");
    }

    protected override void OnCancel(TimerRangeSequenceEvent sequenceEvent)
    {
        OnExit(sequenceEvent);
    }
}
