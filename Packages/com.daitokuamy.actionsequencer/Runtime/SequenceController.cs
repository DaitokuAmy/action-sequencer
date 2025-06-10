using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace ActionSequencer {
    /// <summary>
    /// Sequence再生管理用ハンドル
    /// </summary>
    public readonly struct SequenceHandle : IEnumerator, IDisposable {
        private readonly SequenceController _controller;
        private readonly SequenceController.PlayingInfo _playingInfo;

        /// <summary>再生完了しているか</summary>
        public bool IsDone => _controller == null || _playingInfo == null || _playingInfo.IsDone;

        /// <summary>IEnumerator用</summary>
        object IEnumerator.Current => null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        internal SequenceHandle(SequenceController controller, SequenceController.PlayingInfo playingInfo) {
            _controller = controller;
            _playingInfo = playingInfo;
        }

        /// <summary>
        /// 廃棄時処理
        /// </summary>
        public void Dispose() {
            Stop();
        }

        /// <summary>
        /// IEnumerator用
        /// </summary>
        bool IEnumerator.MoveNext() => !IsDone;

        /// <summary>
        /// IEnumerator用
        /// </summary>
        void IEnumerator.Reset() {
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop() {
            if (_controller == null || _playingInfo == null || _playingInfo.IsDone) {
                return;
            }

            _controller.Stop(_playingInfo);
        }
    }

    /// <summary>
    /// Sequence再生用クラス
    /// </summary>
    public sealed class SequenceController : IReadOnlySequenceController, IDisposable {
        /// <summary>
        /// 再生中情報
        /// </summary>
        public class PlayingInfo {
            /// <summary>SignalEventのHandlerリスト</summary>
            public readonly Dictionary<SignalSequenceEvent, List<ISignalSequenceEventHandler>> SignalEventHandlers = new();
            /// <summary>RangeEventのHandlerリスト</summary>
            public readonly Dictionary<RangeSequenceEvent, List<IRangeSequenceEventHandler>> RangeEventHandlers = new();

            /// <summary>有効なSignalイベント</summary>
            public readonly List<SignalSequenceEvent> ActiveSignalEvents = new();
            /// <summary>有効なRangeイベント</summary>
            public readonly List<RangeSequenceEvent> ActiveRangeEvents = new();

            /// <summary>再生しているClip</summary>
            public SequenceClip Clip;
            /// <summary>現在の再生時間</summary>
            public float Time;

            /// <summary>再生完了しているか</summary>
            public bool IsDone => ActiveSignalEvents.Count <= 0 && ActiveRangeEvents.Count <= 0;
        }

        /// <summary>
        /// EventHandler情報
        /// </summary>
        private class EventHandlerInfo {
            public Type Type;
            public Action<object> InitAction;
            public Action<object> ReadyAction;
        }

        /// <summary>
        /// Dispose時アクション登録用
        /// </summary>
        private class DisposableAction : IDisposable {
            private Action _onDisposed;

            public DisposableAction(Action onDisposed) {
                _onDisposed = onDisposed;
            }

            void IDisposable.Dispose() {
                _onDisposed?.Invoke();
                _onDisposed = null;
            }
        }

        // Event > EventHandler情報の紐付け
        private static readonly Dictionary<Type, List<EventHandlerInfo>> GlobalSignalEventHandlerInfos = new();
        private static readonly Dictionary<Type, List<EventHandlerInfo>> GlobalRangeEventHandlerInfos = new();

        private readonly Dictionary<Type, List<EventHandlerInfo>> _signalEventHandlerInfos = new();
        private readonly Dictionary<Type, List<EventHandlerInfo>> _rangeEventHandlerInfos = new();

        // 再生中情報リスト
        private readonly List<PlayingInfo> _playingInfos = new();
        private readonly ObjectPool<PlayingInfo> _playingInfoPool;
        // 削除対象のPlayingInfoIndexリスト（高速化用にメンバー化）
        private readonly List<int> _removePlayingIndices = new();
        // ハンドラインスタンス用Pool
        private readonly Dictionary<Type, ObjectPool<ISignalSequenceEventHandler>> _signalHandlerPools = new();
        private readonly Dictionary<Type, ObjectPool<IRangeSequenceEventHandler>> _rangeHandlerPools = new();

        /// <summary>再生中のSequenceClipが存在するか</summary>
        public bool HasPlayingClip => _playingInfos.Count > 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequenceController() {
            _playingInfoPool = new ObjectPool<PlayingInfo>(() => new PlayingInfo());
        }

        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理(1回)</param>
        /// <param name="onReady">ハンドラ準備時の処理(再生毎)</param>
        public static IDisposable BindGlobalSignalEventHandler<TEvent, THandler>(Action<THandler> onInit = null, Action<THandler> onReady = null)
            where TEvent : SignalSequenceEvent
            where THandler : SignalSequenceEventHandler<TEvent> {
            var type = typeof(TEvent);
            if (!GlobalSignalEventHandlerInfos.TryGetValue(type, out var infos)) {
                infos = new List<EventHandlerInfo>();
                GlobalSignalEventHandlerInfos.Add(type, infos);
            }

            var info = new EventHandlerInfo {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null,
                ReadyAction = onReady != null ? obj => { onReady.Invoke(obj as THandler); } : null
            };
            infos.Add(info);

            return new DisposableAction(() => { infos.Remove(info); });
        }

        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInvoke">イベント発火時処理</param>
        public static IDisposable BindGlobalSignalEventHandler<TEvent>(Action<TEvent> onInvoke)
            where TEvent : SignalSequenceEvent {
            return BindGlobalSignalEventHandler<TEvent, ObserveSignalSequenceEventHandler<TEvent>>(handler => { handler.SetInvokeAction(onInvoke); });
        }

        /// <summary>
        /// 単体イベント用のハンドラの設定解除
        /// </summary>
        public static void ResetGlobalSignalEventHandler<TEvent>()
            where TEvent : SignalSequenceEvent {
            GlobalSignalEventHandlerInfos.Remove(typeof(TEvent));
        }

        /// <summary>
        /// 単体イベント用のハンドラを一括設定解除
        /// </summary>
        public static void ResetGlobalSignalEventHandlers() {
            GlobalSignalEventHandlerInfos.Clear();
        }

        /// <summary>
        /// 範囲イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理(1回)</param>
        /// <param name="onReady">ハンドラ準備時の処理(再生毎)</param>
        public static IDisposable BindGlobalRangeEventHandler<TEvent, THandler>(Action<THandler> onInit = null, Action<THandler> onReady = null)
            where TEvent : RangeSequenceEvent
            where THandler : RangeSequenceEventHandler<TEvent> {
            var type = typeof(TEvent);
            if (!GlobalRangeEventHandlerInfos.TryGetValue(type, out var infos)) {
                infos = new List<EventHandlerInfo>();
                GlobalRangeEventHandlerInfos.Add(type, infos);
            }

            var info = new EventHandlerInfo {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null,
                ReadyAction = onReady != null ? obj => { onReady.Invoke(obj as THandler); } : null
            };
            infos.Add(info);

            return new DisposableAction(() => { infos.Remove(info); });
        }

        /// <summary>
        /// 範囲イベント用のハンドラを設定
        /// </summary>
        /// <param name="onEnter">区間開始時処理</param>
        /// <param name="onExit">区間終了時処理</param>
        /// <param name="onUpdate">区間中更新処理</param>
        /// <param name="onCancel">区間キャンセル時処理</param>
        public static IDisposable BindGlobalRangeEventHandler<TEvent>(Action<TEvent> onEnter, Action<TEvent> onExit,
            Action<TEvent, float> onUpdate = null, Action<TEvent> onCancel = null)
            where TEvent : RangeSequenceEvent {
            return BindGlobalRangeEventHandler<TEvent, ObserveRangeSequenceEventHandler<TEvent>>(handler => {
                handler.SetEnterAction(onEnter);
                handler.SetExitAction(onExit);
                handler.SetUpdateAction(onUpdate);
                handler.SetCancelAction(onCancel);
            });
        }

        /// <summary>
        /// 範囲イベント用のハンドラの設定解除
        /// </summary>
        public static void ResetGlobalRangeEventHandler<TEvent>()
            where TEvent : RangeSequenceEvent {
            GlobalRangeEventHandlerInfos.Remove(typeof(TEvent));
        }

        /// <summary>
        /// 範囲イベント用のハンドラを一括設定解除
        /// </summary>
        public static void ResetGlobalRangeEventHandlers() {
            GlobalRangeEventHandlerInfos.Clear();
        }

        /// <summary>
        /// イベント用のハンドラを一括設定解除
        /// </summary>
        public static void ResetGlobalEventHandlers() {
            ResetGlobalSignalEventHandlers();
            ResetGlobalSignalEventHandlers();
        }

        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理(1回)</param>
        /// <param name="onReady">ハンドラ準備時の処理(再生毎)</param>
        public IDisposable BindSignalEventHandler<TEvent, THandler>(Action<THandler> onInit = null, Action<THandler> onReady = null)
            where TEvent : SignalSequenceEvent
            where THandler : SignalSequenceEventHandler<TEvent> {
            var type = typeof(TEvent);
            if (!_signalEventHandlerInfos.TryGetValue(type, out var infos)) {
                infos = new List<EventHandlerInfo>();
                _signalEventHandlerInfos.Add(type, infos);
            }

            var info = new EventHandlerInfo {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null,
                ReadyAction = onReady != null ? obj => { onReady.Invoke(obj as THandler); } : null
            };
            infos.Add(info);

            return new DisposableAction(() => { infos.Remove(info); });
        }

        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInvoke">イベント発火時処理</param>
        public IDisposable BindSignalEventHandler<TEvent>(Action<TEvent> onInvoke)
            where TEvent : SignalSequenceEvent {
            return BindSignalEventHandler<TEvent, ObserveSignalSequenceEventHandler<TEvent>>(handler => { handler.SetInvokeAction(onInvoke); });
        }

        /// <summary>
        /// 単体イベント用のハンドラの設定解除
        /// </summary>
        public void ResetSignalEventHandler<TEvent>()
            where TEvent : SignalSequenceEvent {
            _signalEventHandlerInfos.Remove(typeof(TEvent));
        }

        /// <summary>
        /// 単体イベント用のハンドラを一括設定解除
        /// </summary>
        public void ResetSignalEventHandlers() {
            _signalEventHandlerInfos.Clear();
        }

        /// <summary>
        /// 範囲イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理(1回)</param>
        /// <param name="onReady">ハンドラ準備時の処理(再生毎)</param>
        public IDisposable BindRangeEventHandler<TEvent, THandler>(Action<THandler> onInit = null, Action<THandler> onReady = null)
            where TEvent : RangeSequenceEvent
            where THandler : RangeSequenceEventHandler<TEvent> {
            var type = typeof(TEvent);
            if (!_rangeEventHandlerInfos.TryGetValue(type, out var infos)) {
                infos = new List<EventHandlerInfo>();
                _rangeEventHandlerInfos.Add(type, infos);
            }

            var info = new EventHandlerInfo {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null,
                ReadyAction = onReady != null ? obj => { onReady.Invoke(obj as THandler); } : null
            };
            infos.Add(info);

            return new DisposableAction(() => { infos.Remove(info); });
        }

        /// <summary>
        /// 範囲イベント用のハンドラを設定
        /// </summary>
        /// <param name="onEnter">区間開始時処理</param>
        /// <param name="onExit">区間終了時処理</param>
        /// <param name="onUpdate">区間中更新処理</param>
        /// <param name="onCancel">区間キャンセル時処理</param>
        public IDisposable BindRangeEventHandler<TEvent>(Action<TEvent> onEnter, Action<TEvent> onExit,
            Action<TEvent, float> onUpdate = null, Action<TEvent> onCancel = null)
            where TEvent : RangeSequenceEvent {
            return BindRangeEventHandler<TEvent, ObserveRangeSequenceEventHandler<TEvent>>(handler => {
                handler.SetEnterAction(onEnter);
                handler.SetExitAction(onExit);
                handler.SetUpdateAction(onUpdate);
                handler.SetCancelAction(onCancel);
            });
        }

        /// <summary>
        /// 範囲イベント用のハンドラの設定解除
        /// </summary>
        public void ResetRangeEventHandler<TEvent>()
            where TEvent : RangeSequenceEvent {
            _rangeEventHandlerInfos.Remove(typeof(TEvent));
        }

        /// <summary>
        /// 範囲イベント用のハンドラを一括設定解除
        /// </summary>
        public void ResetRangeEventHandlers() {
            _rangeEventHandlerInfos.Clear();
        }

        /// <summary>
        /// イベント用のハンドラを一括設定解除
        /// </summary>
        public void ResetEventHandlers() {
            ResetSignalEventHandlers();
            ResetRangeEventHandlers();
        }

        /// <summary>
        /// 廃棄処理
        /// </summary>
        public void Dispose() {
            StopAll();
            ResetEventHandlers();
            _signalHandlerPools.Clear();
            _rangeHandlerPools.Clear();
        }

        /// <summary>
        /// 再生処理
        /// </summary>
        /// <param name="clip">再生対象のClip</param>
        /// <param name="startOffset">開始時間オフセット</param>
        public SequenceHandle Play(SequenceClip clip, float startOffset = 0.0f) {
            // 再生用の情報を追加
            var playingInfo = CreatePlayingInfo(clip, startOffset);
            _playingInfos.Add(playingInfo);

            return new SequenceHandle(this, playingInfo);
        }

        /// <summary>
        /// 全クリップの強制停止
        /// </summary>
        public void StopAll() {
            foreach (var playingInfo in _playingInfos) {
                // 実行中の物を全部キャンセル
                for (var i = playingInfo.ActiveSignalEvents.Count - 1; i >= 0; i--) {
                    var signalEvent = playingInfo.ActiveSignalEvents[i];
                    if (playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var handlers)) {
                        foreach (var handler in handlers) {
                            ReleaseSignalEventHandler(handler);
                        }
                    }
                }

                for (var i = playingInfo.ActiveRangeEvents.Count - 1; i >= 0; i--) {
                    var rangeEvent = playingInfo.ActiveRangeEvents[i];
                    if (playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handlers)) {
                        foreach (var handler in handlers) {
                            if (handler.IsEntered) {
                                handler.Cancel(rangeEvent);
                            }

                            ReleaseRangeEventHandler(handler);
                        }
                    }
                }

                ReleasePlayingInfo(playingInfo);
            }

            _playingInfos.Clear();
        }

        /// <summary>
        /// 該当SequenceClipの再生時間（再生していなければ負の値)
        /// </summary>
        public float GetSequenceTime(SequenceClip clip) {
            if (clip == null) {
                return -1.0f;
            }

            var foundInfo = _playingInfos.FirstOrDefault(x => x.Clip == clip);
            if (foundInfo == null) {
                return -1.0f;
            }

            return foundInfo.Time;
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        /// <param name="deltaTime">経過時間</param>
        public void Update(float deltaTime) {
            for (var i = 0; i < _playingInfos.Count; i++) {
                var playingInfo = _playingInfos[i];

                // 時間の更新
                playingInfo.Time += deltaTime;

                // 単発イベントの更新
                for (var j = playingInfo.ActiveSignalEvents.Count - 1; j >= 0; j--) {
                    var signalEvent = playingInfo.ActiveSignalEvents[j];
                    if (signalEvent.time > playingInfo.Time) {
                        continue;
                    }

                    if (playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var handlers)) {
                        foreach (var handler in handlers) {
                            // 発火通知
                            handler.Invoke(signalEvent);
                            // 解放
                            ReleaseSignalEventHandler(handler);
                        }
                    }

                    // リストから除外
                    playingInfo.ActiveSignalEvents.RemoveAt(j);
                }

                // 範囲イベントの更新
                for (var j = playingInfo.ActiveRangeEvents.Count - 1; j >= 0; j--) {
                    var rangeEvent = playingInfo.ActiveRangeEvents[j];
                    if (rangeEvent.enterTime > playingInfo.Time) {
                        continue;
                    }

                    if (playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handlers)) {
                        var elapsedTime = Mathf.Min(playingInfo.Time - rangeEvent.enterTime, rangeEvent.Duration);

                        foreach (var handler in handlers) {
                            var enterFrame = false;

                            // EnterしてなければEnter実行
                            if (!handler.IsEntered) {
                                enterFrame = true;
                                handler.Enter(rangeEvent);
                            }

                            handler.Update(rangeEvent, elapsedTime);

                            // 終了していたらExit実行してリストから除外
                            if (rangeEvent.exitTime <= playingInfo.Time) {
                                // Enter/Exitが同時に呼ばれるのを回避する対応
                                if (!enterFrame || !rangeEvent.MustOneFrame) {
                                    handler.Exit(rangeEvent);
                                    // 解放
                                    ReleaseRangeEventHandler(handler);
                                    // リストから除外
                                    playingInfo.ActiveRangeEvents.RemoveAt(j);
                                }
                            }
                        }
                    }
                    else {
                        // リストから除外
                        playingInfo.ActiveRangeEvents.RemoveAt(j);
                    }
                }

                // 完了していたら除外
                if (playingInfo.IsDone) {
                    _removePlayingIndices.Add(i);
                }
            }

            // 再生終了した物を除外
            for (var i = _removePlayingIndices.Count - 1; i >= 0; i--) {
                var index = _removePlayingIndices[i];
                _playingInfos.RemoveAt(index);
            }

            _removePlayingIndices.Clear();
        }

        /// <summary>
        /// 停止処理
        /// </summary>
        internal void Stop(PlayingInfo playingInfo) {
            if (playingInfo == null) {
                return;
            }

            // リストから除外
            if (!_playingInfos.Remove(playingInfo)) {
                return;
            }

            // 実行中の物を全部キャンセル
            for (var i = playingInfo.ActiveSignalEvents.Count - 1; i >= 0; i--) {
                var signalEvent = playingInfo.ActiveSignalEvents[i];
                if (playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var handlers)) {
                    foreach (var handler in handlers) {
                        ReleaseSignalEventHandler(handler);
                    }
                }
            }

            for (var i = playingInfo.ActiveRangeEvents.Count - 1; i >= 0; i--) {
                var rangeEvent = playingInfo.ActiveRangeEvents[i];
                if (playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handlers)) {
                    foreach (var handler in handlers) {
                        if (handler.IsEntered) {
                            handler.Cancel(rangeEvent);
                        }

                        ReleaseRangeEventHandler(handler);
                    }
                }
            }

            // プールから解放
            ReleasePlayingInfo(playingInfo);
        }

        /// <summary>
        /// 再生用情報の作成
        /// </summary>
        private PlayingInfo CreatePlayingInfo(SequenceClip clip, float startOffset) {
            _playingInfoPool.Get(out var playingInfo);
            playingInfo.Clip = clip;
            playingInfo.Time = startOffset;

            bool TryGetHandlerInfo(Dictionary<Type, List<EventHandlerInfo>> localInfos,
                Dictionary<Type, List<EventHandlerInfo>> globalInfos, Type type, out List<EventHandlerInfo> handlerInfos) {
                if (localInfos.TryGetValue(type, out handlerInfos)) {
                    return true;
                }

                if (globalInfos.TryGetValue(type, out handlerInfos)) {
                    return true;
                }

                return false;
            }

            void AddEvents(SequenceClip seqClip) {
                foreach (var track in seqClip.tracks) {
                    foreach (var ev in track.sequenceEvents) {
                        // 無効状態のEventは処理しない
                        if (!ev.active) {
                            continue;
                        }

                        if (ev is RangeSequenceEvent rangeEvent) {
                            // Handlerの生成
                            if (TryGetHandlerInfo(_rangeEventHandlerInfos, GlobalRangeEventHandlerInfos, ev.GetType(),
                                    out var handlerInfos)) {
                                if (!playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handlers)) {
                                    handlers = new List<IRangeSequenceEventHandler>();
                                    playingInfo.RangeEventHandlers.Add(rangeEvent, handlers);
                                }

                                foreach (var handlerInfo in handlerInfos) {
                                    var handler = GetRangeEventHandler(handlerInfo);
                                    if (handler != null) {
                                        handlers.Add(handler);
                                        handlerInfo.ReadyAction?.Invoke(handler);
                                    }
                                }
                            }

                            // 待機リストへ登録
                            playingInfo.ActiveRangeEvents.Add(rangeEvent);
                        }
                        else if (ev is SignalSequenceEvent signalEvent) {
                            // Handlerの生成
                            if (TryGetHandlerInfo(_signalEventHandlerInfos, GlobalSignalEventHandlerInfos, ev.GetType(),
                                    out var handlerInfos)) {
                                if (!playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var handlers)) {
                                    handlers = new List<ISignalSequenceEventHandler>();
                                    playingInfo.SignalEventHandlers.Add(signalEvent, handlers);
                                }

                                foreach (var handlerInfo in handlerInfos) {
                                    var handler = GetSignalEventHandler(handlerInfo);
                                    if (handler != null) {
                                        handlers.Add(handler);
                                        handlerInfo.ReadyAction?.Invoke(handler);
                                    }
                                }
                            }

                            // 待機リストへ登録
                            playingInfo.ActiveSignalEvents.Add(signalEvent);
                        }
                    }
                }
            }

            AddEvents(clip);
            foreach (var includeClip in clip.includeClips) {
                AddEvents(includeClip);
            }

            // リストのソート(終了時間の降順)
            playingInfo.ActiveRangeEvents.Sort((a, b) => b.exitTime.CompareTo(a.exitTime));
            playingInfo.ActiveSignalEvents.Sort((a, b) => b.time.CompareTo(a.time));

            return playingInfo;
        }

        /// <summary>
        /// 再生用情報のリリース
        /// </summary>
        private void ReleasePlayingInfo(PlayingInfo playingInfo) {
            playingInfo.Clip = null;
            playingInfo.Time = 0.0f;
            playingInfo.ActiveSignalEvents.Clear();
            playingInfo.ActiveRangeEvents.Clear();
            foreach (var pair in playingInfo.SignalEventHandlers) {
                pair.Value.Clear();
            }

            foreach (var pair in playingInfo.RangeEventHandlers) {
                pair.Value.Clear();
            }

            _playingInfoPool.Release(playingInfo);
        }

        /// <summary>
        /// イベントハンドラの取得
        /// </summary>
        private ISignalSequenceEventHandler GetSignalEventHandler(EventHandlerInfo handlerInfo) {
            var type = handlerInfo.Type;
            if (!_signalHandlerPools.TryGetValue(type, out var pool)) {
                pool = new ObjectPool<ISignalSequenceEventHandler>(() => {
                    var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null) {
                        return null;
                    }

                    var handler = (ISignalSequenceEventHandler)constructorInfo.Invoke(Array.Empty<object>());
                    handlerInfo.InitAction?.Invoke(handler);
                    return handler;
                }, _ => { }, _ => { }, _ => { });
                _signalHandlerPools[type] = pool;
            }

            return pool.Get();
        }

        /// <summary>
        /// イベントハンドラの取得
        /// </summary>
        private IRangeSequenceEventHandler GetRangeEventHandler(EventHandlerInfo handlerInfo) {
            var type = handlerInfo.Type;
            if (!_rangeHandlerPools.TryGetValue(type, out var pool)) {
                pool = new ObjectPool<IRangeSequenceEventHandler>(() => {
                    var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null) {
                        return null;
                    }

                    var handler = (IRangeSequenceEventHandler)constructorInfo.Invoke(Array.Empty<object>());
                    handlerInfo.InitAction?.Invoke(handler);
                    return handler;
                }, _ => { }, _ => { }, _ => { });
                _rangeHandlerPools[type] = pool;
            }

            return pool.Get();
        }

        /// <summary>
        /// イベントハンドラの開放
        /// </summary>
        private void ReleaseSignalEventHandler(ISignalSequenceEventHandler handler) {
            var type = handler.GetType();
            if (_signalHandlerPools.TryGetValue(type, out var pool)) {
                pool.Release(handler);
            }
        }

        /// <summary>
        /// イベントハンドラの開放
        /// </summary>
        private void ReleaseRangeEventHandler(IRangeSequenceEventHandler handler) {
            var type = handler.GetType();
            if (_rangeHandlerPools.TryGetValue(type, out var pool)) {
                pool.Release(handler);
            }
        }
    }
}