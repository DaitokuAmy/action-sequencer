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
    public struct SequenceHandle : IEnumerator {
        private readonly SequenceController.PlayingInfo _playingInfo;

        // 再生完了しているか
        public bool IsDone => _playingInfo != null ? _playingInfo.IsDone : true;

        // IEnumerator用
        object IEnumerator.Current => null;

        public SequenceHandle(SequenceController.PlayingInfo playingInfo) {
            _playingInfo = playingInfo;
        }

        public override int GetHashCode() => _playingInfo != null ? _playingInfo.GetHashCode() : 0;

        public override bool Equals(object obj) {
            if (obj == null) {
                return _playingInfo == null;
            }

            return GetHashCode() == obj.GetHashCode();
        }

        bool IEnumerator.MoveNext() => !IsDone;

        void IEnumerator.Reset() {
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
            // 再生しているClip
            public SequenceClip Clip;

            // イベントとHandlerの紐付け
            public Dictionary<SignalSequenceEvent, ISignalSequenceEventHandler> SignalEventHandlers = new();
            public Dictionary<RangeSequenceEvent, IRangeSequenceEventHandler> RangeEventHandlers = new();

            // 有効なイベント
            public List<SignalSequenceEvent> ActiveSignalEvents = new List<SignalSequenceEvent>();
            public List<RangeSequenceEvent> ActiveRangeEvents = new List<RangeSequenceEvent>();

            // 現在の再生時間
            public float Time;

            // 再生完了しているか
            public bool IsDone => ActiveSignalEvents.Count <= 0 && ActiveRangeEvents.Count <= 0;
        }

        /// <summary>
        /// EventHandler情報
        /// </summary>
        private class EventHandlerInfo {
            public Type Type;
            public Action<object> InitAction;
        }

        // Event > EventHandler情報の紐付け
        private static readonly Dictionary<Type, EventHandlerInfo> GlobalSignalEventHandlerInfos = new();
        private static readonly Dictionary<Type, EventHandlerInfo> GlobalRangeEventHandlerInfos = new();

        private readonly Dictionary<Type, EventHandlerInfo> _signalEventHandlerInfos = new();
        private readonly Dictionary<Type, EventHandlerInfo> _rangeEventHandlerInfos = new();

        // 再生中情報リスト
        private readonly List<PlayingInfo> _playingInfos = new();
        // 削除対象のPlayingInfoIndexリスト（高速化用にメンバー化）
        private readonly List<int> _removePlayingIndices = new();
        // ハンドラインスタンス用Pool
        private readonly Dictionary<Type, ObjectPool<ISignalSequenceEventHandler>> _signalHandlerPools = new();
        private readonly Dictionary<Type, ObjectPool<IRangeSequenceEventHandler>> _rangeHandlerPools = new();

        /// <summary>再生中のSequenceClipが存在するか</summary>
        public bool HasPlayingClip => _playingInfos.Count > 0;

        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理</param>
        public static void BindGlobalSignalEventHandler<TEvent, THandler>(Action<THandler> onInit = null)
            where TEvent : SignalSequenceEvent
            where THandler : SignalSequenceEventHandler<TEvent> {
            GlobalSignalEventHandlerInfos[typeof(TEvent)] = new EventHandlerInfo {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null
            };
        }

        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInvoke">イベント発火時処理</param>
        public static void BindGlobalSignalEventHandler<TEvent>(Action<TEvent> onInvoke)
            where TEvent : SignalSequenceEvent {
            BindGlobalSignalEventHandler<TEvent, ObserveSignalSequenceEventHandler<TEvent>>(handler => {
                handler.SetInvokeAction(onInvoke);
            });
        }

        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理</param>
        public void BindSignalEventHandler<TEvent, THandler>(Action<THandler> onInit = null)
            where TEvent : SignalSequenceEvent
            where THandler : SignalSequenceEventHandler<TEvent> {
            _signalEventHandlerInfos[typeof(TEvent)] = new EventHandlerInfo {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null
            };
        }

        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInvoke">イベント発火時処理</param>
        public void BindSignalEventHandler<TEvent>(Action<TEvent> onInvoke)
            where TEvent : SignalSequenceEvent {
            BindSignalEventHandler<TEvent, ObserveSignalSequenceEventHandler<TEvent>>(handler => {
                handler.SetInvokeAction(onInvoke);
            });
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
        /// <param name="onInit">ハンドラ生成時の処理</param>
        public static void BindGlobalRangeEventHandler<TEvent, THandler>(Action<THandler> onInit = null)
            where TEvent : RangeSequenceEvent
            where THandler : RangeSequenceEventHandler<TEvent> {
            GlobalRangeEventHandlerInfos[typeof(TEvent)] = new EventHandlerInfo {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null
            };
        }

        /// <summary>
        /// 範囲イベント用のハンドラを設定
        /// </summary>
        /// <param name="onEnter">区間開始時処理</param>
        /// <param name="onExit">区間終了時処理</param>
        /// <param name="onUpdate">区間中更新処理</param>
        /// <param name="onCancel">区間キャンセル時処理</param>
        public static void BindGlobalRangeEventHandler<TEvent>(Action<TEvent> onEnter, Action<TEvent> onExit,
            Action<TEvent, float> onUpdate = null, Action<TEvent> onCancel = null)
            where TEvent : RangeSequenceEvent {
            BindGlobalRangeEventHandler<TEvent, ObserveRangeSequenceEventHandler<TEvent>>(handler => {
                handler.SetEnterAction(onEnter);
                handler.SetExitAction(onExit);
                handler.SetUpdateAction(onUpdate);
                handler.SetCancelAction(onCancel);
            });
        }

        /// <summary>
        /// 範囲イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理</param>
        public void BindRangeEventHandler<TEvent, THandler>(Action<THandler> onInit = null)
            where TEvent : RangeSequenceEvent
            where THandler : RangeSequenceEventHandler<TEvent> {
            _rangeEventHandlerInfos[typeof(TEvent)] = new EventHandlerInfo {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null
            };
        }

        /// <summary>
        /// 範囲イベント用のハンドラを設定
        /// </summary>
        /// <param name="onEnter">区間開始時処理</param>
        /// <param name="onExit">区間終了時処理</param>
        /// <param name="onUpdate">区間中更新処理</param>
        /// <param name="onCancel">区間キャンセル時処理</param>
        public void BindRangeEventHandler<TEvent>(Action<TEvent> onEnter, Action<TEvent> onExit,
            Action<TEvent, float> onUpdate = null, Action<TEvent> onCancel = null)
            where TEvent : RangeSequenceEvent {
            BindRangeEventHandler<TEvent, ObserveRangeSequenceEventHandler<TEvent>>(handler => {
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
        public static void ResetGlobalEventHandlers() {
            ResetGlobalSignalEventHandlers();
            ResetGlobalSignalEventHandlers();
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

            return new SequenceHandle(playingInfo);
        }

        /// <summary>
        /// 強制停止処理
        /// </summary>
        /// <param name="handle">停止対象を表すHandle</param>
        public void Stop(SequenceHandle handle) {
            var playingInfo = _playingInfos.FirstOrDefault(x => handle.GetHashCode() == x.GetHashCode());
            if (playingInfo == null) {
                return;
            }

            // 実行中の物を全部キャンセル
            for (var i = playingInfo.ActiveSignalEvents.Count - 1; i >= 0; i--) {
                var signalEvent = playingInfo.ActiveSignalEvents[i];
                if (playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var handler)) {
                    ReleaseSignalEventHandler(handler);
                }
            }

            for (var i = playingInfo.ActiveRangeEvents.Count - 1; i >= 0; i--) {
                var rangeEvent = playingInfo.ActiveRangeEvents[i];
                if (playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handler)) {
                    if (handler.IsEntered) {
                        handler.Cancel(rangeEvent);
                    }

                    ReleaseRangeEventHandler(handler);
                }
            }

            // 再生中リストから除外
            _playingInfos.Remove(playingInfo);
        }

        /// <summary>
        /// 全クリップの強制停止
        /// </summary>
        public void StopAll() {
            foreach (var playingInfo in _playingInfos) {
                // 実行中の物を全部キャンセル
                for (var i = playingInfo.ActiveSignalEvents.Count - 1; i >= 0; i--) {
                    var signalEvent = playingInfo.ActiveSignalEvents[i];
                    if (playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var handler)) {
                        ReleaseSignalEventHandler(handler);
                    }
                }

                for (var i = playingInfo.ActiveRangeEvents.Count - 1; i >= 0; i--) {
                    var rangeEvent = playingInfo.ActiveRangeEvents[i];
                    if (playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handler)) {
                        if (handler.IsEntered) {
                            handler.Cancel(rangeEvent);
                        }

                        ReleaseRangeEventHandler(handler);
                    }
                }
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

                    if (playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var handler)) {
                        // 発火通知
                        handler.Invoke(signalEvent);
                        // 解放
                        ReleaseSignalEventHandler(handler);
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

                    if (playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handler)) {
                        var elapsedTime = Mathf.Min(playingInfo.Time - rangeEvent.enterTime, rangeEvent.Duration);
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
        /// 再生用情報の作成
        /// </summary>
        private PlayingInfo CreatePlayingInfo(SequenceClip clip, float startOffset) {
            var playingInfo = new PlayingInfo {
                Clip = clip,
                Time = startOffset
            };

            bool TryGetHandlerInfo(Dictionary<Type, EventHandlerInfo> localInfos,
                Dictionary<Type, EventHandlerInfo> globalInfos, Type type, out EventHandlerInfo handlerInfo) {
                if (localInfos.TryGetValue(type, out handlerInfo)) {
                    return true;
                }

                if (globalInfos.TryGetValue(type, out handlerInfo)) {
                    return true;
                }

                return false;
            }

            foreach (var track in clip.tracks) {
                foreach (var ev in track.sequenceEvents) {
                    // 無効状態のEventは処理しない
                    if (!ev.active) {
                        continue;
                    }

                    if (ev is RangeSequenceEvent rangeEvent) {
                        // Handlerの生成
                        if (TryGetHandlerInfo(_rangeEventHandlerInfos, GlobalRangeEventHandlerInfos, ev.GetType(),
                                out var handlerInfo)) {
                            var handler = GetRangeEventHandler(handlerInfo.Type);
                            if (handler != null) {
                                playingInfo.RangeEventHandlers[rangeEvent] = handler;
                                handlerInfo.InitAction?.Invoke(handler);
                            }
                        }

                        // 待機リストへ登録
                        playingInfo.ActiveRangeEvents.Add(rangeEvent);
                    }
                    else if (ev is SignalSequenceEvent signalEvent) {
                        // Handlerの生成
                        if (TryGetHandlerInfo(_signalEventHandlerInfos, GlobalSignalEventHandlerInfos, ev.GetType(),
                                out var handlerInfo)) {
                            var handler = GetSignalEventHandler(handlerInfo.Type);
                            if (handler != null) {
                                playingInfo.SignalEventHandlers[signalEvent] = handler;
                                handlerInfo.InitAction?.Invoke(handler);
                            }
                        }

                        // 待機リストへ登録
                        playingInfo.ActiveSignalEvents.Add(signalEvent);
                    }
                }
            }

            // リストのソート(終了時間の降順)
            playingInfo.ActiveRangeEvents.Sort((a, b) => b.exitTime.CompareTo(a.exitTime));
            playingInfo.ActiveSignalEvents.Sort((a, b) => b.time.CompareTo(a.time));

            return playingInfo;
        }

        /// <summary>
        /// イベントハンドラの取得
        /// </summary>
        private ISignalSequenceEventHandler GetSignalEventHandler(Type type) {
            if (!_signalHandlerPools.TryGetValue(type, out var pool)) {
                pool = new ObjectPool<ISignalSequenceEventHandler>(() => {
                    var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null) {
                        return null;
                    }

                    return (ISignalSequenceEventHandler)constructorInfo.Invoke(Array.Empty<object>());
                }, handler => { }, handler => { }, handler => { });
                _signalHandlerPools[type] = pool;
            }

            return pool.Get();
        }

        /// <summary>
        /// イベントハンドラの取得
        /// </summary>
        private IRangeSequenceEventHandler GetRangeEventHandler(Type type) {
            if (!_rangeHandlerPools.TryGetValue(type, out var pool)) {
                pool = new ObjectPool<IRangeSequenceEventHandler>(() => {
                    var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                    if (constructorInfo == null) {
                        return null;
                    }

                    return (IRangeSequenceEventHandler)constructorInfo.Invoke(Array.Empty<object>());
                }, handler => { }, handler => { }, handler => { });
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