using System;
using ActionSequencer;
using UnityEngine;

public class SampleScene : MonoBehaviour, ISequenceControllerProvider
{
    [SerializeField]
    private SequenceClip _clip;

    private SequenceController _sequenceController;

    SequenceController ISequenceControllerProvider.SequenceController => _sequenceController;
    
    private void Start()
    {
        _sequenceController = new SequenceController();
        
        _sequenceController.BindSignalEventHandler<LogSignalSequenceEvent, LogSignalSequenceEventHandler>();
        _sequenceController.BindRangeEventHandler<TimerRangeSequenceEvent, TimerRangeSequenceEventHandler>();
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _sequenceController.Play(_clip);
        }
        
        _sequenceController.Update(Time.deltaTime);
    }

    private void OnDestroy() {
        _sequenceController?.Dispose();
    }
}
