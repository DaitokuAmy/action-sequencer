using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ActionSequencer {
    /// <summary>
    /// Sequence再生用プレイヤー
    /// </summary>
    public sealed class SequencePlayer : IReadOnlySequencePlayer, IDisposable {
        /// <summary>
        /// SignalEventのハンドラ実体情報
        /// </summary>
        private sealed class SignalEventHandlerEntry {
            /// <summary>紐づくハンドラ情報</summary>
            public EventHandlerInfo Info { get; set; }
            /// <summary>ハンドラ実体</summary>
            public ISignalSequenceEventHandler Handler { get; set; }
        }

        /// <summary>
        /// RangeEventのハンドラ実体情報
        /// </summary>
        private sealed class RangeEventHandlerEntry {
            /// <summary>紐づくハンドラ情報</summary>
            public EventHandlerInfo Info { get; set; }
            /// <summary>ハンドラ実体</summary>
            public IRangeSequenceEventHandler Handler { get; set; }
        }

        /// <summary>
        /// 再生状態
        /// </summary>
        private enum PlayingStatus {
            PendingPlay,
            Playing,
            PendingStop
        }

        /// <summary>
        /// 再生中情報
        /// </summary>
        private sealed class PlayingInfo {
            /// <summary>SignalEventのHandlerリスト</summary>
            public Dictionary<SignalSequenceEvent, List<SignalEventHandlerEntry>> SignalEventHandlers { get; } = new();
            /// <summary>RangeEventのHandlerリスト</summary>
            public Dictionary<RangeSequenceEvent, List<RangeEventHandlerEntry>> RangeEventHandlers { get; } = new();

            /// <summary>有効なSignalイベント</summary>
            public List<SignalSequenceEvent> ActiveSignalEvents { get; } = new();
            /// <summary>有効なRangeイベント</summary>
            public List<RangeSequenceEvent> ActiveRangeEvents { get; } = new();
            /// <summary>Enter済みのRangeイベント</summary>
            public HashSet<RangeSequenceEvent> EnteredRangeEvents { get; } = new();

            /// <summary>再生しているClip</summary>
            public SequenceClip Clip { get; set; }
            /// <summary>再生ID</summary>
            public int Id { get; set; }
            /// <summary>現在の再生時間</summary>
            public float Time { get; set; }
            /// <summary>再生状態</summary>
            public PlayingStatus Status { get; set; }

            /// <summary>再生完了しているか</summary>
            public bool IsDone => ActiveSignalEvents.Count <= 0 && ActiveRangeEvents.Count <= 0;
        }

        /// <summary>
        /// EventHandler情報
        /// </summary>
        private sealed class EventHandlerInfo {
            /// <summary>ハンドラ型</summary>
            public Type Type { get; set; }
            /// <summary>ハンドラ生成時の初期化処理</summary>
            public Action<object> InitAction { get; set; }
            /// <summary>再生準備時の処理</summary>
            public Action<object> ReadyAction { get; set; }
            /// <summary>SignalEvent用ハンドラプール</summary>
            public ObjectPool<ISignalSequenceEventHandler> SignalHandlerPool { get; set; }
            /// <summary>RangeEvent用ハンドラプール</summary>
            public ObjectPool<IRangeSequenceEventHandler> RangeHandlerPool { get; set; }
        }

        /// <summary>
        /// Dispose時アクション登録用
        /// </summary>
        private sealed class DisposableAction : IDisposable {
            private Action _onDisposed;

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="onDisposed">廃棄時処理</param>
            public DisposableAction(Action onDisposed) {
                _onDisposed = onDisposed;
            }

            /// <inheritdoc />
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
        private readonly Dictionary<int, PlayingInfo> _playingInfoMap = new();
        private readonly ObjectPool<PlayingInfo> _playingInfoPool;
        private readonly ObjectPool<HashSet<SignalSequenceEvent>> _activeSignalEventSetPool;
        private readonly ObjectPool<HashSet<RangeSequenceEvent>> _activeRangeEventSetPool;
        private readonly ObjectPool<SignalEventHandlerEntry> _signalEventHandlerEntryPool;
        private readonly ObjectPool<RangeEventHandlerEntry> _rangeEventHandlerEntryPool;
        private readonly ObjectPool<List<SignalEventHandlerEntry>> _signalEventHandlerEntryListPool;
        private readonly ObjectPool<List<RangeEventHandlerEntry>> _rangeEventHandlerEntryListPool;
        private bool _isUpdating;
        private int _nextPlayingId = 1;

        /// <summary>再生中のSequenceClipが存在するか</summary>
        public bool HasPlayingClip => _playingInfoMap.Count > 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequencePlayer() {
            _playingInfoPool = new ObjectPool<PlayingInfo>(() => new PlayingInfo());
            _activeSignalEventSetPool = new ObjectPool<HashSet<SignalSequenceEvent>>(() => new HashSet<SignalSequenceEvent>(),
                null, set => { set.Clear(); });
            _activeRangeEventSetPool = new ObjectPool<HashSet<RangeSequenceEvent>>(() => new HashSet<RangeSequenceEvent>(),
                null, set => { set.Clear(); });
            _signalEventHandlerEntryPool = new ObjectPool<SignalEventHandlerEntry>(() => new SignalEventHandlerEntry());
            _rangeEventHandlerEntryPool = new ObjectPool<RangeEventHandlerEntry>(() => new RangeEventHandlerEntry());
            _signalEventHandlerEntryListPool = new ObjectPool<List<SignalEventHandlerEntry>>(() => new List<SignalEventHandlerEntry>(),
                null, list => { list.Clear(); });
            _rangeEventHandlerEntryListPool = new ObjectPool<List<RangeEventHandlerEntry>>(() => new List<RangeEventHandlerEntry>(),
                null, list => { list.Clear(); });
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
            ResetGlobalRangeEventHandlers();
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
        }

        /// <summary>
        /// 再生処理
        /// </summary>
        /// <param name="clip">再生対象のClip</param>
        /// <param name="startOffset">開始時間オフセット</param>
        public SequenceHandle Play(SequenceClip clip, float startOffset = 0.0f) {
            if (clip == null) {
                return default;
            }

            // 再生用の情報を追加
            var playingInfo = CreatePlayingInfo(clip, startOffset);
            playingInfo.Status = _isUpdating ? PlayingStatus.PendingPlay : PlayingStatus.Playing;
            _playingInfos.Add(playingInfo);
            _playingInfoMap.Add(playingInfo.Id, playingInfo);

            return new SequenceHandle(this, playingInfo.Id);
        }

        /// <summary>
        /// 全クリップの強制停止
        /// </summary>
        public void StopAll() {
            foreach (var playingInfo in _playingInfos) {
                QueueStop(playingInfo);
            }

            if (!_isUpdating) {
                CleanupStoppedPlayingInfos();
            }
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        /// <param name="deltaTime">経過時間</param>
        public void Update(float deltaTime) {
            PromotePendingPlays();
            _isUpdating = true;
            try {
                for (var i = 0; i < _playingInfos.Count; i++) {
                    var playingInfo = _playingInfos[i];
                    if (playingInfo.Status != PlayingStatus.Playing) {
                        continue;
                    }

                    // 時間の更新
                    playingInfo.Time += deltaTime;

                    var continueCurrentPlaying = true;

                    // 単発イベントの更新
                    for (var j = playingInfo.ActiveSignalEvents.Count - 1; j >= 0; j--) {
                        var signalEvent = playingInfo.ActiveSignalEvents[j];
                        if (signalEvent.time > playingInfo.Time) {
                            continue;
                        }

                        if (playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var handlers)) {
                            foreach (var handler in handlers) {
                                // 発火通知
                                handler.Handler.Invoke(signalEvent);
                                if (playingInfo.Status != PlayingStatus.Playing) {
                                    continueCurrentPlaying = false;
                                    break;
                                }

                                // 解放
                                ReleaseSignalEventHandler(handler);
                            }

                            if (!continueCurrentPlaying) {
                                break;
                            }
                        }

                        playingInfo.SignalEventHandlers.Remove(signalEvent);
                        ReleaseSignalEventHandlerEntries(handlers);

                        // リストから除外
                        playingInfo.ActiveSignalEvents.RemoveAt(j);
                    }

                    if (!continueCurrentPlaying || playingInfo.Status != PlayingStatus.Playing) {
                        continue;
                    }

                    // 範囲イベントの更新
                    for (var j = playingInfo.ActiveRangeEvents.Count - 1; j >= 0; j--) {
                        var rangeEvent = playingInfo.ActiveRangeEvents[j];
                        if (rangeEvent.enterTime > playingInfo.Time) {
                            continue;
                        }

                        var enteredThisFrame = playingInfo.EnteredRangeEvents.Add(rangeEvent);
                        if (playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handlers)) {
                            var elapsedTime = Mathf.Min(playingInfo.Time - rangeEvent.enterTime, rangeEvent.Duration);
                            var shouldRemove = false;

                            foreach (var handler in handlers) {
                                // EnterしてなければEnter実行
                                if (!handler.Handler.IsEntered) {
                                    handler.Handler.Enter(rangeEvent);
                                    if (playingInfo.Status != PlayingStatus.Playing) {
                                        continueCurrentPlaying = false;
                                        break;
                                    }
                                }

                                handler.Handler.Update(rangeEvent, elapsedTime);
                                if (playingInfo.Status != PlayingStatus.Playing) {
                                    continueCurrentPlaying = false;
                                    break;
                                }

                                // 終了していたらExit実行
                                if (rangeEvent.exitTime <= playingInfo.Time) {
                                    // Enter/Exitが同時に呼ばれるのを回避する対応
                                    if (!enteredThisFrame || !rangeEvent.MustOneFrame) {
                                        handler.Handler.Exit(rangeEvent);
                                        if (playingInfo.Status != PlayingStatus.Playing) {
                                            continueCurrentPlaying = false;
                                            break;
                                        }

                                        // 解放
                                        ReleaseRangeEventHandler(handler);
                                        shouldRemove = true;
                                    }
                                }
                            }

                            if (!continueCurrentPlaying) {
                                break;
                            }

                            if (shouldRemove) {
                                playingInfo.RangeEventHandlers.Remove(rangeEvent);
                                playingInfo.EnteredRangeEvents.Remove(rangeEvent);
                                ReleaseRangeEventHandlerEntries(handlers, rangeEvent, false);
                                // リストから除外
                                playingInfo.ActiveRangeEvents.RemoveAt(j);
                            }
                        }
                        else if (rangeEvent.exitTime <= playingInfo.Time && (!enteredThisFrame || !rangeEvent.MustOneFrame)) {
                            playingInfo.EnteredRangeEvents.Remove(rangeEvent);
                            // リストから除外
                            playingInfo.ActiveRangeEvents.RemoveAt(j);
                        }
                    }

                    if (!continueCurrentPlaying || playingInfo.Status != PlayingStatus.Playing) {
                        continue;
                    }

                    // 完了していたら除外
                    if (playingInfo.IsDone) {
                        QueueStop(playingInfo);
                    }
                }
            }
            finally {
                _isUpdating = false;
            }

            CleanupStoppedPlayingInfos();
        }

        /// <summary>
        /// 該当SequenceClipの再生時間（再生していなければ負の値)
        /// </summary>
        public float GetSequenceTime(SequenceClip clip) {
            if (clip == null) {
                return -1.0f;
            }

            for (var i = 0; i < _playingInfos.Count; i++) {
                var playingInfo = _playingInfos[i];
                if (playingInfo.Clip == clip && playingInfo.Status != PlayingStatus.PendingStop) {
                    return playingInfo.Time;
                }
            }

            return -1.0f;
        }

        /// <summary>
        /// 指定した再生IDが有効か
        /// </summary>
        /// <param name="playingId">再生ID</param>
        internal bool IsPlaying(int playingId) {
            if (playingId <= 0) {
                return false;
            }

            return _playingInfoMap.ContainsKey(playingId);
        }

        /// <summary>
        /// 停止処理
        /// </summary>
        internal void Stop(int playingId) {
            if (!_playingInfoMap.TryGetValue(playingId, out var playingInfo)) {
                return;
            }

            Stop(playingInfo);
        }

        /// <summary>
        /// 停止処理
        /// </summary>
        private void Stop(PlayingInfo playingInfo) {
            if (playingInfo == null) {
                return;
            }

            QueueStop(playingInfo);
            if (!_isUpdating) {
                CleanupStoppedPlayingInfos();
            }
        }

        /// <summary>
        /// 再生用情報の作成
        /// </summary>
        private PlayingInfo CreatePlayingInfo(SequenceClip clip, float startOffset) {
            _playingInfoPool.Get(out var playingInfo);
            playingInfo.Clip = clip;
            playingInfo.Id = GetNextPlayingId();
            playingInfo.Time = startOffset;
            playingInfo.Status = PlayingStatus.PendingPlay;
            var activeSignalEvents = _activeSignalEventSetPool.Get();
            var activeRangeEvents = _activeRangeEventSetPool.Get();

            try {
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
                    if (seqClip == null || seqClip.tracks == null) {
                        return;
                    }

                    foreach (var track in seqClip.tracks) {
                        if (track == null || track.sequenceEvents == null) {
                            continue;
                        }

                        foreach (var ev in track.sequenceEvents) {
                            if (ev == null) {
                                continue;
                            }

                            // 無効状態のEventは処理しない
                            if (!ev.active) {
                                continue;
                            }

                            if (ev is RangeSequenceEvent rangeEvent) {
                                if (!activeRangeEvents.Add(rangeEvent)) {
                                    continue;
                                }

                                // Handlerの生成
                                if (TryGetHandlerInfo(_rangeEventHandlerInfos, GlobalRangeEventHandlerInfos, ev.GetType(),
                                        out var handlerInfos)) {
                                    foreach (var handlerInfo in handlerInfos) {
                                        var handler = GetRangeEventHandler(handlerInfo);
                                        if (handler != null) {
                                            if (!playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handlers)) {
                                                handlers = GetRangeEventHandlerEntryList();
                                                playingInfo.RangeEventHandlers.Add(rangeEvent, handlers);
                                            }

                                            handlers.Add(GetRangeEventHandlerEntry(handlerInfo, handler));
                                            handlerInfo.ReadyAction?.Invoke(handler);
                                        }
                                    }
                                }

                                // 待機リストへ登録
                                playingInfo.ActiveRangeEvents.Add(rangeEvent);
                            }
                            else if (ev is SignalSequenceEvent signalEvent) {
                                if (!activeSignalEvents.Add(signalEvent)) {
                                    continue;
                                }

                                // Handlerの生成
                                if (TryGetHandlerInfo(_signalEventHandlerInfos, GlobalSignalEventHandlerInfos, ev.GetType(),
                                        out var handlerInfos)) {
                                    foreach (var handlerInfo in handlerInfos) {
                                        var handler = GetSignalEventHandler(handlerInfo);
                                        if (handler != null) {
                                            if (!playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var handlers)) {
                                                handlers = GetSignalEventHandlerEntryList();
                                                playingInfo.SignalEventHandlers.Add(signalEvent, handlers);
                                            }

                                            handlers.Add(GetSignalEventHandlerEntry(handlerInfo, handler));
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
                if (clip.includeClips != null) {
                    foreach (var includeClip in clip.includeClips) {
                        AddEvents(includeClip);
                    }
                }

                // リストのソート(終了時間の降順)
                playingInfo.ActiveRangeEvents.Sort((a, b) => b.exitTime.CompareTo(a.exitTime));
                playingInfo.ActiveSignalEvents.Sort((a, b) => b.time.CompareTo(a.time));

                return playingInfo;
            }
            finally {
                _activeSignalEventSetPool.Release(activeSignalEvents);
                _activeRangeEventSetPool.Release(activeRangeEvents);
            }
        }

        /// <summary>
        /// 再生用情報のリリース
        /// </summary>
        private void ReleasePlayingInfo(PlayingInfo playingInfo) {
            playingInfo.Clip = null;
            playingInfo.Id = 0;
            playingInfo.Time = 0.0f;
            playingInfo.Status = PlayingStatus.PendingPlay;
            playingInfo.ActiveSignalEvents.Clear();
            playingInfo.ActiveRangeEvents.Clear();
            playingInfo.EnteredRangeEvents.Clear();
            playingInfo.SignalEventHandlers.Clear();
            playingInfo.RangeEventHandlers.Clear();
            _playingInfoPool.Release(playingInfo);
        }

        /// <summary>
        /// 次の再生IDを取得
        /// </summary>
        private int GetNextPlayingId() {
            var playingId = _nextPlayingId;
            while (playingId <= 0 || _playingInfoMap.ContainsKey(playingId)) {
                playingId++;
            }

            _nextPlayingId = playingId + 1;
            return playingId;
        }

        /// <summary>
        /// 再生開始待ちを有効化
        /// </summary>
        private void PromotePendingPlays() {
            foreach (var playingInfo in _playingInfos) {
                if (playingInfo.Status == PlayingStatus.PendingPlay) {
                    playingInfo.Status = PlayingStatus.Playing;
                }
            }
        }

        /// <summary>
        /// 停止待ちに設定
        /// </summary>
        private void QueueStop(PlayingInfo playingInfo) {
            if (playingInfo == null || playingInfo.Status == PlayingStatus.PendingStop) {
                return;
            }

            _playingInfoMap.Remove(playingInfo.Id);
            playingInfo.Status = PlayingStatus.PendingStop;
        }

        /// <summary>
        /// 停止待ちの再生を解放
        /// </summary>
        private void CleanupStoppedPlayingInfos() {
            for (var i = _playingInfos.Count - 1; i >= 0; i--) {
                var playingInfo = _playingInfos[i];
                if (playingInfo.Status != PlayingStatus.PendingStop) {
                    continue;
                }

                ReleasePlayingHandlers(playingInfo);
                _playingInfos.RemoveAt(i);
                ReleasePlayingInfo(playingInfo);
            }
        }

        /// <summary>
        /// 再生中ハンドラの解放
        /// </summary>
        private void ReleasePlayingHandlers(PlayingInfo playingInfo) {
            for (var i = playingInfo.ActiveSignalEvents.Count - 1; i >= 0; i--) {
                var signalEvent = playingInfo.ActiveSignalEvents[i];
                if (playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var signalHandlers)) {
                    playingInfo.SignalEventHandlers.Remove(signalEvent);
                    ReleaseSignalEventHandlerEntries(signalHandlers);
                }
            }

            for (var i = playingInfo.ActiveRangeEvents.Count - 1; i >= 0; i--) {
                var rangeEvent = playingInfo.ActiveRangeEvents[i];
                if (playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var rangeHandlers)) {
                    playingInfo.RangeEventHandlers.Remove(rangeEvent);
                    ReleaseRangeEventHandlerEntries(rangeHandlers, rangeEvent, true);
                }
            }
        }

        /// <summary>
        /// SignalEventのハンドラエントリを取得
        /// </summary>
        private SignalEventHandlerEntry GetSignalEventHandlerEntry(EventHandlerInfo info, ISignalSequenceEventHandler handler) {
            var entry = _signalEventHandlerEntryPool.Get();
            entry.Info = info;
            entry.Handler = handler;
            return entry;
        }

        /// <summary>
        /// RangeEventのハンドラエントリを取得
        /// </summary>
        private RangeEventHandlerEntry GetRangeEventHandlerEntry(EventHandlerInfo info, IRangeSequenceEventHandler handler) {
            var entry = _rangeEventHandlerEntryPool.Get();
            entry.Info = info;
            entry.Handler = handler;
            return entry;
        }

        /// <summary>
        /// SignalEventのハンドラリストを取得
        /// </summary>
        private List<SignalEventHandlerEntry> GetSignalEventHandlerEntryList() {
            return _signalEventHandlerEntryListPool.Get();
        }

        /// <summary>
        /// RangeEventのハンドラリストを取得
        /// </summary>
        private List<RangeEventHandlerEntry> GetRangeEventHandlerEntryList() {
            return _rangeEventHandlerEntryListPool.Get();
        }

        /// <summary>
        /// イベントハンドラの取得
        /// </summary>
        private ISignalSequenceEventHandler GetSignalEventHandler(EventHandlerInfo handlerInfo) {
            if (handlerInfo.SignalHandlerPool == null) {
                var type = handlerInfo.Type;
                handlerInfo.SignalHandlerPool = new ObjectPool<ISignalSequenceEventHandler>(() => {
                    var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null) {
                        return null;
                    }

                    var handler = (ISignalSequenceEventHandler)constructorInfo.Invoke(Array.Empty<object>());
                    handlerInfo.InitAction?.Invoke(handler);
                    return handler;
                }, _ => { }, _ => { }, _ => { });
            }

            return handlerInfo.SignalHandlerPool.Get();
        }

        /// <summary>
        /// イベントハンドラの取得
        /// </summary>
        private IRangeSequenceEventHandler GetRangeEventHandler(EventHandlerInfo handlerInfo) {
            if (handlerInfo.RangeHandlerPool == null) {
                var type = handlerInfo.Type;
                handlerInfo.RangeHandlerPool = new ObjectPool<IRangeSequenceEventHandler>(() => {
                    var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null) {
                        return null;
                    }

                    var handler = (IRangeSequenceEventHandler)constructorInfo.Invoke(Array.Empty<object>());
                    handlerInfo.InitAction?.Invoke(handler);
                    return handler;
                }, _ => { }, _ => { }, _ => { });
            }

            return handlerInfo.RangeHandlerPool.Get();
        }

        /// <summary>
        /// イベントハンドラの開放
        /// </summary>
        private void ReleaseSignalEventHandler(SignalEventHandlerEntry handlerEntry) {
            if (handlerEntry?.Info?.SignalHandlerPool != null && handlerEntry.Handler != null) {
                handlerEntry.Info.SignalHandlerPool.Release(handlerEntry.Handler);
                handlerEntry.Handler = null;
            }
        }

        /// <summary>
        /// SignalEventのハンドラエントリリストを解放
        /// </summary>
        private void ReleaseSignalEventHandlerEntries(List<SignalEventHandlerEntry> handlers) {
            if (handlers == null) {
                return;
            }

            foreach (var handlerEntry in handlers) {
                ReleaseSignalEventHandler(handlerEntry);
                handlerEntry.Info = null;
                _signalEventHandlerEntryPool.Release(handlerEntry);
            }

            _signalEventHandlerEntryListPool.Release(handlers);
        }

        /// <summary>
        /// イベントハンドラの開放
        /// </summary>
        private void ReleaseRangeEventHandler(RangeEventHandlerEntry handlerEntry) {
            if (handlerEntry?.Info?.RangeHandlerPool != null && handlerEntry.Handler != null) {
                handlerEntry.Info.RangeHandlerPool.Release(handlerEntry.Handler);
                handlerEntry.Handler = null;
            }
        }

        /// <summary>
        /// RangeEventのハンドラエントリリストを解放
        /// </summary>
        private void ReleaseRangeEventHandlerEntries(List<RangeEventHandlerEntry> handlers, RangeSequenceEvent rangeEvent,
            bool cancelEnteredHandlers) {
            if (handlers == null) {
                return;
            }

            foreach (var handlerEntry in handlers) {
                if (cancelEnteredHandlers && handlerEntry.Handler != null && handlerEntry.Handler.IsEntered) {
                    handlerEntry.Handler.Cancel(rangeEvent);
                }

                ReleaseRangeEventHandler(handlerEntry);
                handlerEntry.Info = null;
                _rangeEventHandlerEntryPool.Release(handlerEntry);
            }

            _rangeEventHandlerEntryListPool.Release(handlers);
        }
    }
}
