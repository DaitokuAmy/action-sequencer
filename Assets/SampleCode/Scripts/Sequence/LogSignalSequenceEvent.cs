using System.Collections;
using System.Collections.Generic;
using ActionSequencer;
using UnityEngine;

public class LogSignalSequenceEvent : SignalSequenceEvent
{
    public readonly string Text = "";
}

public class LogSignalSequenceEventHandler : SignalSequenceEventHandler<LogSignalSequenceEvent>
{
    protected override void OnInvoke(LogSignalSequenceEvent signalSequenceEvent)
    {
        Debug.Log(signalSequenceEvent.Text);
    }
}
