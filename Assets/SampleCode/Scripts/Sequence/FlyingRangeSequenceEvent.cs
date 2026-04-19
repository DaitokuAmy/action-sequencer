using ActionSequencer;

public sealed class FlyingRangeSequenceEvent : RangeSequenceEvent {
}

public sealed class FlyingRangeSequenceEventHandler : RangeSequenceEventHandler<FlyingRangeSequenceEvent> {
    /// <summary>
    /// 開始位置に到達した時の処理
    /// </summary>
    protected override void OnEnter(FlyingRangeSequenceEvent sequenceEvent) {
        // todo:空中状態開始
    }

    /// <summary>
    /// 終了位置に到達した時の処理
    /// </summary>
    protected override void OnExit(FlyingRangeSequenceEvent sequenceEvent) {
        // todo:空中状態終了
    }

    /// <summary>
    /// 終了する前にキャンセルされた時の処理
    /// </summary>
    protected override void OnCancel(FlyingRangeSequenceEvent sequenceEvent) {
        OnExit(sequenceEvent);
    }
}