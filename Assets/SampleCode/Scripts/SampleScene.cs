using ActionSequencer;
using UnityEngine;

public class SampleScene : MonoBehaviour
{
    [SerializeField]
    private SequenceClip _clip;

    private SequenceController _controller;
    
    private void Start()
    {
        SequenceController.BindGlobalSignalEventHandler<LogSignalSequenceEvent, LogSignalSequenceEventHandler>();
        SequenceController.BindGlobalRangeEventHandler<TimerRangeSequenceEvent, TimerRangeSequenceEventHandler>();
        _controller = new SequenceController();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _controller.Play(_clip);
        }
        
        _controller.Update(Time.deltaTime);
    }
}
