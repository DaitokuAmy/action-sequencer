using System;
using ActionSequencer;
using Test;
using UnityEngine;

/// <summary>
/// サンプル用コード
/// </summary>
public class Sample : MonoBehaviour, ISequencePlayerProvider {
    // アクション情報
    [Serializable]
    private class ActionInfo {
        public string triggerName;
        public SequenceClip SequenceClip;
    }

    [SerializeField, Tooltip("制御対象Animator")]
    private Animator _animator;
    [SerializeField, Tooltip("アクション情報リスト")]
    private ActionInfo[] _actionInfos;

    public float startOffset;

    // SequenceClip再生用プレイヤー
    private SequencePlayer _sequencePlayer;
    // 以前再生したActionによるSequenceHandle
    private SequenceHandle _actionSequenceHandle;
    // 現在流すべきActionIndex
    private int _actionIndex;

    // Preview用
    SequencePlayer ISequencePlayerProvider.SequencePlayer => _sequencePlayer;

    /// <summary>
    /// 開始処理
    /// </summary>
    private void Start() {
        Application.targetFrameRate = 30;
        
        _sequencePlayer = new SequencePlayer();

        // 各種イベントと振る舞いを紐づける
        _sequencePlayer.BindSignalEventHandler<LogSignalSequenceEvent, LogSignalSequenceEventHandler>();
        _sequencePlayer.BindRangeEventHandler<TimerRangeSequenceEvent, TimerRangeSequenceEventHandler>();
        _sequencePlayer.BindSignalEventHandler<PlayEffectSignalSequenceEvent, PlayEffectSignalSequenceEventHandler>(
            handler => { handler.Setup(_animator.transform); });
    }

    /// <summary>
    /// 更新処理ｓ
    /// </summary>
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            _actionIndex = (_actionIndex + 1) % _actionInfos.Length;
            PlayAction(_actionIndex);
        }
        
        _sequencePlayer.Update(Time.deltaTime);
    }

    /// <summary>
    /// 廃棄時処理
    /// </summary>
    private void OnDestroy() {
        _sequencePlayer.ResetEventHandlers();
        _sequencePlayer?.Dispose();
    }

    /// <summary>
    /// アクションの再生
    /// </summary>
    private void PlayAction(int actionIndex) {
        if (actionIndex < 0 || actionIndex >= _actionInfos.Length) {
            return;
        }

        // 以前流した物があれば止める
        _actionSequenceHandle.Stop();

        // モーション再生と同時にシーケンスを流す
        var actionInfo = _actionInfos[actionIndex];
        _animator.SetTrigger(actionInfo.triggerName);
        _actionSequenceHandle = _sequencePlayer.Play(actionInfo.SequenceClip, startOffset);
    }
}
