using System.Diagnostics;
using ActionSequencer;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TimerRangeSequenceEvent : RangeSequenceEvent
{
    [Tooltip("出力用のフォーマット")]
    public string format = "Time:{0.000}";
}

public class TimerRangeSequenceEventHandler : RangeSequenceEventHandler<TimerRangeSequenceEvent>
{
    private Stopwatch _stopwatch = new Stopwatch();
    
    /// <summary>
    /// 開始位置に到達した時の処理
    /// </summary>
    protected override void OnEnter(TimerRangeSequenceEvent sequenceEvent)
    {
        _stopwatch.Restart();
    }

    /// <summary>
    /// 終了位置に到達した時の処理
    /// </summary>
    protected override void OnExit(TimerRangeSequenceEvent sequenceEvent)
    {
        _stopwatch.Stop();
        Debug.Log(string.Format(sequenceEvent.format, _stopwatch.Elapsed.TotalSeconds));
    }

    /// <summary>
    /// 終了する前にキャンセルされた時の処理
    /// </summary>
    protected override void OnCancel(TimerRangeSequenceEvent sequenceEvent)
    {
        OnExit(sequenceEvent);
    }
}
