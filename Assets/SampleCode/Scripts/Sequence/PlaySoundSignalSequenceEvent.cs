using ActionSequencer;
using UnityEngine;

/// <summary>
/// サウンド再生用イベント
/// </summary>
public class PlaySoundSignalSequenceEvent : SignalSequenceEvent {
    public string soundLabel;
    public Vector3 offsetPosition;
    public Vector3 offsetAngles;

    /// <inheritdoc/>
    public override string TimelineText => string.IsNullOrEmpty(soundLabel) ? "[None]" : soundLabel;
}

/// <summary>
/// サウンド再生用イベントのハンドラ
/// </summary>
public class PlaySoundEffectSignalSequenceEventHandler : SignalSequenceEventHandler<PlaySoundSignalSequenceEvent> {
    private Transform _owenr;
    
    /// <summary>
    /// 初期化処理
    /// </summary>
    /// <param name="owner">発生主</param>
    public void Setup(Transform owner) {
        _owenr = owner;
    }
    
    /// <summary>
    /// イベント発火時処理
    /// </summary>
    protected override void OnInvoke(PlaySoundSignalSequenceEvent sequenceEvent) {
        if (_owenr == null) {
            return;
        }
        
        // todo:サウンド再生
    }
}
