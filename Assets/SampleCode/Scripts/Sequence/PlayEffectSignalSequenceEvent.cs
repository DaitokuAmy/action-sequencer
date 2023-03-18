using ActionSequencer;
using UnityEngine;

/// <summary>
/// エフェクト再生用イベント
/// </summary>
public class PlayEffectSignalSequenceEvent : SignalSequenceEvent {
    public GameObject prefab;
    public Vector3 offsetPosition;
    public Vector3 offsetAngles;
}

/// <summary>
/// エフェクト再生用イベントのハンドラ
/// </summary>
public class PlayEffectSignalSequenceEventHandler : SignalSequenceEventHandler<PlayEffectSignalSequenceEvent> {
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
    protected override void OnInvoke(PlayEffectSignalSequenceEvent sequenceEvent) {
        if (_owenr == null) {
            return;
        }

        if (sequenceEvent.prefab == null) {
            return;
        }

        var position = _owenr.TransformPoint(sequenceEvent.offsetPosition);
        var rotation = Quaternion.Euler(sequenceEvent.offsetAngles) * _owenr.rotation;
        var instance = Object.Instantiate(sequenceEvent.prefab);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        var particleSystem = instance.GetComponent<ParticleSystem>();
        if (particleSystem != null) {
            // ※削除はPrefabの指定任せ
            particleSystem.Play();
        }
    }
}
