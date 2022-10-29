using System.Collections;
using System.Collections.Generic;
using ActionSequencer;
using UnityEngine;

public class LogSequenceEvent : SequenceSignalEvent
{
    public readonly string Text = "";
}

public class LogSequenceEventHandler : SequenceSignalEventHandler<LogSequenceEvent>
{
    protected override void OnInvoke(LogSequenceEvent sequenceEvent)
    {
        Debug.Log(sequenceEvent.Text);
    }
}
