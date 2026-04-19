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
        private readonly SequencePlayer _player;
        private readonly int _playingId;

        /// <summary>再生完了しているか</summary>
        public bool IsDone => _player == null || !_player.IsPlaying(_playingId);

        /// <summary>IEnumerator用</summary>
        object IEnumerator.Current => null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        internal SequenceHandle(SequencePlayer player, int playingId) {
            _player = player;
            _playingId = playingId;
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
            if (_player == null) {
                return;
            }

            _player.Stop(_playingId);
        }
    }

    /// <summary>
    /// Sequence再生用プレイヤー
    /// </summary>
    public sealed class SequencePlayer : IReadOnlySequencePlayer, IDisposable {
        /// <summary>
        /// SignalEventのハンドラ実体情報
        /// </summary>
        private sealed class SignalEventHandlerEntry {
            public EventHandlerInfo Info { get; set; }
            public ISignalSequenceEventHandler Handler { get; set; }
        }

        /// <summary>
        /// RangeEventのハンドラ実体情報
        /// </summary>
        private sealed class RangeEventHandlerEntry {
            public EventHandlerInfo Info { get; set; }
            public IRangeSequenceEventHandler Handler { get; set; }
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

            /// <summary>再生しているClip</summary>
            public SequenceClip Clip { get; set; }
            /// <summary>再生ID</summary>
            public int Id { get; set; }
            /// <summary>現在の再生時間</summary>
            public float Time { get; set; }

            /// <summary>再生完了しているか</summary>
            public bool IsDone => ActiveSignalEvents.Count <= 0 && ActiveRangeEvents.Count <= 0;
        }

        /// <summary>
        /// EventHandler情報
        /// </summary>
        private sealed class EventHandlerInfo {
            public Type Type { get; set; }
            public Action<object> InitAction { get; set; }
            public Action<object> ReadyAction { get; set; }
            public ObjectPool<ISignalSequenceEventHandler> SignalHandlerPool { get; set; }
            public ObjectPool<IRangeSequenceEventHandler> RangeHandlerPool { get; set; }
        }

        /// <summary>
        /// Dispose時アクション登録用
        /// </summary>
        private sealed class DisposableAction : IDisposable {
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
        private readonly Dictionary<int, PlayingInfo> _playingInfoMap = new();
        private readonly ObjectPool<PlayingInfo> _playingInfoPool;
        private int _nextPlayingId = 1;
        // 削除対象のPlayingInfoIndexリスト（高速化用にメンバー化）
        private readonly List<int> _removePlayingIndices = new();

        /// <summary>再生中のSequenceClipが存在するか</summary>
        public bool HasPlayingClip => _playingInfos.Count > 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SequencePlayer() {
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
            // 再生用の情報を追加
            var playingInfo = CreatePlayingInfo(clip, startOffset);
            _playingInfos.Add(playingInfo);
            _playingInfoMap.Add(playingInfo.Id, playingInfo);

            return new SequenceHandle(this, playingInfo.Id);
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
                            if (handler.Handler.IsEntered) {
                                handler.Handler.Cancel(rangeEvent);
                            }

                            ReleaseRangeEventHandler(handler);
                        }
                    }
                }

                ReleasePlayingInfo(playingInfo);
            }

            _playingInfos.Clear();
            _playingInfoMap.Clear();
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
                            handler.Handler.Invoke(signalEvent);
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
                            if (!handler.Handler.IsEntered) {
                                enterFrame = true;
                                handler.Handler.Enter(rangeEvent);
                            }

                            handler.Handler.Update(rangeEvent, elapsedTime);

                            // 終了していたらExit実行してリストから除外
                            if (rangeEvent.exitTime <= playingInfo.Time) {
                                // Enter/Exitが同時に呼ばれるのを回避する対応
                                if (!enterFrame || !rangeEvent.MustOneFrame) {
                                    handler.Handler.Exit(rangeEvent);
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
                var playingInfo = _playingInfos[index];
                _playingInfoMap.Remove(playingInfo.Id);
                _playingInfos.RemoveAt(index);
                ReleasePlayingInfo(playingInfo);
            }

            _removePlayingIndices.Clear();
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

            // リストから除外
            if (!_playingInfos.Remove(playingInfo)) {
                return;
            }

            _playingInfoMap.Remove(playingInfo.Id);

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
                        if (handler.Handler.IsEntered) {
                            handler.Handler.Cancel(rangeEvent);
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
            playingInfo.Id = GetNextPlayingId();
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
                                    handlers = new List<RangeEventHandlerEntry>();
                                    playingInfo.RangeEventHandlers.Add(rangeEvent, handlers);
                                }

                                foreach (var handlerInfo in handlerInfos) {
                                    var handler = GetRangeEventHandler(handlerInfo);
                                    if (handler != null) {
                                        handlers.Add(new RangeEventHandlerEntry {
                                            Info = handlerInfo,
                                            Handler = handler
                                        });
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
                                    handlers = new List<SignalEventHandlerEntry>();
                                    playingInfo.SignalEventHandlers.Add(signalEvent, handlers);
                                }

                                foreach (var handlerInfo in handlerInfos) {
                                    var handler = GetSignalEventHandler(handlerInfo);
                                    if (handler != null) {
                                        handlers.Add(new SignalEventHandlerEntry {
                                            Info = handlerInfo,
                                            Handler = handler
                                        });
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
            playingInfo.Id = 0;
            playingInfo.Time = 0.0f;
            playingInfo.ActiveSignalEvents.Clear();
            playingInfo.ActiveRangeEvents.Clear();
            foreach (var pair in playingInfo.SignalEventHandlers) {
                pair.Value.Clear();
            }

            foreach (var pair in playingInfo.RangeEventHandlers) {
                pair.Value.Clear();
            }

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
            }
        }

        /// <summary>
        /// イベントハンドラの開放
        /// </summary>
        private void ReleaseRangeEventHandler(RangeEventHandlerEntry handlerEntry) {
            if (handlerEntry?.Info?.RangeHandlerPool != null && handlerEntry.Handler != null) {
                handlerEntry.Info.RangeHandlerPool.Release(handlerEntry.Handler);
            }
        }
    }
}
