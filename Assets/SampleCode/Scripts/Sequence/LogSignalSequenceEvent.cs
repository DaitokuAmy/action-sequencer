using ActionSequencer;
using UnityEngine;

public class LogSignalSequenceEvent : SignalSequenceEvent
{
    [Tooltip("出力用のログ")]
    public string text = "";
}

public class LogSignalSequenceEventHandler : SignalSequenceEventHandler<LogSignalSequenceEvent>
{
    /// <summary>
    /// タイミング発火時の処理
    /// </summary>
    /// <param name="signalSequenceEvent"></param>
    protected override void OnInvoke(LogSignalSequenceEvent signalSequenceEvent)
    {
        Debug.Log(signalSequenceEvent.text);
    }
}
