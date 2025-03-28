using System;

namespace ActionSequencer {
    /// <summary>
    /// 外部公開用のSequenceController
    /// </summary>
    public interface IReadOnlySequenceController {
        /// <summary>再生中のSequenceClipが存在するか</summary>
        bool HasPlayingClip { get; }
        
        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理(1回)</param>
        /// <param name="onReady">ハンドラ準備時の処理(再生毎)</param>
        IDisposable BindSignalEventHandler<TEvent, THandler>(Action<THandler> onInit = null, Action<THandler> onReady = null)
            where TEvent : SignalSequenceEvent
            where THandler : SignalSequenceEventHandler<TEvent>;

        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInvoke">イベント発火時処理</param>
        IDisposable BindSignalEventHandler<TEvent>(Action<TEvent> onInvoke)
            where TEvent : SignalSequenceEvent;

        /// <summary>
        /// 単体イベント用のハンドラの設定解除
        /// </summary>
        void ResetSignalEventHandler<TEvent>()
            where TEvent : SignalSequenceEvent;

        /// <summary>
        /// 単体イベント用のハンドラを一括設定解除
        /// </summary>
        void ResetSignalEventHandlers();

        /// <summary>
        /// 範囲イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理(1回)</param>
        /// <param name="onReady">ハンドラ準備時の処理(再生毎)</param>
        IDisposable BindRangeEventHandler<TEvent, THandler>(Action<THandler> onInit = null, Action<THandler> onReady = null)
            where TEvent : RangeSequenceEvent
            where THandler : RangeSequenceEventHandler<TEvent>;

        /// <summary>
        /// 範囲イベント用のハンドラを設定
        /// </summary>
        /// <param name="onEnter">区間開始時処理</param>
        /// <param name="onExit">区間終了時処理</param>
        /// <param name="onUpdate">区間中更新処理</param>
        /// <param name="onCancel">区間キャンセル時処理</param>
        IDisposable BindRangeEventHandler<TEvent>(Action<TEvent> onEnter, Action<TEvent> onExit,
            Action<TEvent, float> onUpdate = null, Action<TEvent> onCancel = null)
            where TEvent : RangeSequenceEvent;

        /// <summary>
        /// 範囲イベント用のハンドラの設定解除
        /// </summary>
        void ResetRangeEventHandler<TEvent>()
            where TEvent : RangeSequenceEvent;
        
        /// <summary>
        /// 範囲イベント用のハンドラを一括設定解除
        /// </summary>
        void ResetRangeEventHandlers();

        /// <summary>
        /// イベント用のハンドラを一括設定解除
        /// </summary>
        void ResetEventHandlers();
    }
}