using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActionSequencer
{
    /// <summary>
    /// Sequence再生管理用ハンドル
    /// </summary>
    public struct SequenceHandle
    {
        private readonly object _handle;

        public SequenceHandle(object handle)
        {
            _handle = handle;
        }

        public override int GetHashCode() => _handle != null ? _handle.GetHashCode() : 0;
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return _handle == null;
            }
            return GetHashCode() == obj.GetHashCode();
        }
    }
    
    /// <summary>
    /// Sequence再生用クラス
    /// </summary>
    public sealed class SequenceController
    {
        /// <summary>
        /// EventHandler情報
        /// </summary>
        private class EventHandlerInfo
        {
            public Type Type;
            public Action<object> InitAction;
        }
        
        /// <summary>
        /// 再生中情報
        /// </summary>
        private class PlayingInfo
        {
            // 再生しているClip
            public SequenceClip Clip;
            
            // イベントとHandlerの紐付け
            public Dictionary<SequenceSignalEvent, ISequenceSignalEventHandler> SignalEventHandlers =
                new Dictionary<SequenceSignalEvent, ISequenceSignalEventHandler>();
            public Dictionary<SequenceRangeEvent, ISequenceRangeEventHandler> RangeEventHandlers =
                new Dictionary<SequenceRangeEvent, ISequenceRangeEventHandler>();

            // 有効なイベント
            public List<SequenceSignalEvent> ActiveSignalEvents = new List<SequenceSignalEvent>();
            public List<SequenceRangeEvent> ActiveRangeEvents = new List<SequenceRangeEvent>();

            public float Time;
        }
        
        // Event > EventHandler情報の紐付け
        private static Dictionary<Type, EventHandlerInfo> _globalSignalEventHandlerInfos = new Dictionary<Type, EventHandlerInfo>();
        private static Dictionary<Type, EventHandlerInfo> _globalRangeEventHandlerInfos = new Dictionary<Type, EventHandlerInfo>();
        private Dictionary<Type, EventHandlerInfo> _signalEventHandlerInfos = new Dictionary<Type, EventHandlerInfo>();
        private Dictionary<Type, EventHandlerInfo> _rangeEventHandlerInfos = new Dictionary<Type, EventHandlerInfo>();

        // 再生中情報リスト
        private List<PlayingInfo> _playingInfos = new List<PlayingInfo>();
        // 削除対象のPlayingInfoIndexリスト（高速化用にメンバー化）
        private List<int> _removePlayingIndices = new List<int>();

        /// <summary>
        /// 単体イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理</param>
        public static void BindGlobalSignalEventHandler<TEvent, THandler>(Action<THandler> onInit = null)
            where TEvent : SequenceSignalEvent
            where THandler : SequenceSignalEventHandler<TEvent>
        {
            _globalSignalEventHandlerInfos[typeof(TEvent)] = new EventHandlerInfo
            {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj =>
                {
                    onInit.Invoke(obj as THandler);
                } : null
            };
        }

        /// <summary>
        /// 単体イベント用のハンドラの設定解除
        /// </summary>
        public static void ResetGlobalSignalEventHandler<TEvent>()
            where TEvent : SequenceSignalEvent
        {
            _globalSignalEventHandlerInfos.Remove(typeof(TEvent));
        }

        /// <summary>
        /// 範囲イベント用のハンドラを設定
        /// </summary>
        /// <param name="onInit">ハンドラ生成時の処理</param>
        public static void BindGlobalRangeEventHandler<TEvent, THandler>(Action<THandler> onInit = null)
            where TEvent : SequenceRangeEvent
            where THandler : SequenceRangeEventHandler<TEvent>
        {
            _globalRangeEventHandlerInfos[typeof(TEvent)] = new EventHandlerInfo
            {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj =>
                {
                    onInit.Invoke(obj as THandler);
                } : null
            };
        }

        /// <summary>
        /// 範囲イベント用のハンドラの設定解除
        /// </summary>
        public static void ResetGlobalRangeEventHandler<TEvent>()
            where TEvent : SequenceRangeEvent
        {
            _globalRangeEventHandlerInfos.Remove(typeof(TEvent));
        }
        
        /// <summary>
        /// 再生処理
        /// </summary>
        /// <param name="clip">再生対象のClip</param>
        /// <param name="startOffset">開始時間オフセット</param>
        public SequenceHandle Play(SequenceClip clip, float startOffset = 0.0f)
        {
            // 再生用の情報を追加
            var playingInfo = CreatePlayingInfo(clip);
            _playingInfos.Add(playingInfo);
            
            return new SequenceHandle(playingInfo);
        }

        /// <summary>
        /// 強制停止処理
        /// </summary>
        /// <param name="handle">停止対象を表すHandle</param>
        public void Stop(SequenceHandle handle)
        {
            var playingInfo = _playingInfos.FirstOrDefault(x => handle.GetHashCode() == x.GetHashCode());
            if (playingInfo == null)
            {
                return;
            }
            
            // 実行中の物を全部キャンセル
            for (var i = playingInfo.ActiveRangeEvents.Count - 1; i >= 0; i--)
            {
                var rangeEvent = playingInfo.ActiveRangeEvents[i];
                if (playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handler))
                {
                    if (handler.IsEntered)
                    {
                        handler.Cancel(rangeEvent);
                    }
                }
            }
            
            // 再生中リストから除外
            _playingInfos.Remove(playingInfo);
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        /// <param name="deltaTime">経過時間</param>
        public void Update(float deltaTime)
        {
            for (var i = 0; i < _playingInfos.Count; i++)
            {
                var playingInfo = _playingInfos[i];
                
                // 時間の更新
                playingInfo.Time += deltaTime;
                
                // 単発イベントの更新
                for (var j = playingInfo.ActiveSignalEvents.Count - 1; j >= 0; j--)
                {
                    var signalEvent = playingInfo.ActiveSignalEvents[j];
                    if (signalEvent.time > playingInfo.Time)
                    {
                        continue;
                    }
                    
                    if (playingInfo.SignalEventHandlers.TryGetValue(signalEvent, out var handler))
                    {
                        // 発火通知
                        handler.Invoke(signalEvent);
                    }
                    
                    // リストから除外
                    playingInfo.ActiveSignalEvents.RemoveAt(j);
                }

                // 範囲イベントの更新
                for (var j = playingInfo.ActiveRangeEvents.Count - 1; j >= 0; j--)
                {
                    var rangeEvent = playingInfo.ActiveRangeEvents[j];
                    if (rangeEvent.enterTime > playingInfo.Time)
                    {
                        continue;
                    }
                    
                    if (playingInfo.RangeEventHandlers.TryGetValue(rangeEvent, out var handler))
                    {
                        var elapsedTime = Mathf.Min(playingInfo.Time - rangeEvent.enterTime, rangeEvent.Duration);
                        // EnterしてなければEnter実行
                        if (!handler.IsEntered)
                        {
                            handler.Enter(rangeEvent);
                        }
                        handler.Update(rangeEvent, elapsedTime);
                        
                        // 終了していたらExit実行
                        if (rangeEvent.exitTime <= playingInfo.Time)
                        {
                            handler.Exit(rangeEvent);
                        }
                    }

                    if (rangeEvent.exitTime <= playingInfo.Time)
                    {
                        // リストから除外
                        playingInfo.ActiveRangeEvents.RemoveAt(j);
                    }
                }
                
                // ActiveなEventがなくなったら終了
                if (playingInfo.ActiveSignalEvents.Count <= 0 && playingInfo.ActiveRangeEvents.Count <= 0)
                {
                    _removePlayingIndices.Add(i);
                }
            }
            
            // 再生終了した物を除外
            for (var i = _removePlayingIndices.Count - 1; i >= 0; i--)
            {
                var index = _removePlayingIndices[i];
                _playingInfos.RemoveAt(index);
            }
            _removePlayingIndices.Clear();
        }

        /// <summary>
        /// 再生用情報の作成
        /// </summary>
        private PlayingInfo CreatePlayingInfo(SequenceClip clip)
        {
            var playingInfo = new PlayingInfo();
            var events = clip.tracks
                .SelectMany(x => x.sequenceEvents)
                .ToArray();

            bool TryGetHandlerInfo(Dictionary<Type, EventHandlerInfo> localInfos, Dictionary<Type, EventHandlerInfo> globalInfos, Type type, out EventHandlerInfo handlerInfo)
            {
                if (localInfos.TryGetValue(type, out handlerInfo))
                {
                    return true;
                }
                if (globalInfos.TryGetValue(type, out handlerInfo))
                {
                    return true;
                }

                return false;
            }

            foreach (var ev in events)
            {
                if (ev is SequenceRangeEvent rangeEvent)
                {
                    // Handlerの生成
                    if (TryGetHandlerInfo(_rangeEventHandlerInfos, _globalRangeEventHandlerInfos, ev.GetType(), out var handlerInfo))
                    {
                        var constructorInfo = handlerInfo.Type.GetConstructor(Type.EmptyTypes);
                        if (constructorInfo != null)
                        {
                            var handler = (ISequenceRangeEventHandler)constructorInfo.Invoke(Array.Empty<object>());
                            playingInfo.RangeEventHandlers[rangeEvent] = handler;
                            handlerInfo.InitAction?.Invoke(handler);
                        }
                    }
                    
                    // 待機リストへ登録
                    playingInfo.ActiveRangeEvents.Add(rangeEvent);
                }
                else if (ev is SequenceSignalEvent signalEvent)
                {
                    // Handlerの生成
                    if (TryGetHandlerInfo(_signalEventHandlerInfos, _globalSignalEventHandlerInfos, ev.GetType(), out var handlerInfo))
                    {
                        var constructorInfo = handlerInfo.Type.GetConstructor(Type.EmptyTypes);
                        if (constructorInfo != null)
                        {
                            var handler = (ISequenceSignalEventHandler)constructorInfo.Invoke(Array.Empty<object>());
                            playingInfo.SignalEventHandlers[signalEvent] = handler;
                            handlerInfo.InitAction?.Invoke(handler);
                        }
                    }
                    
                    // 待機リストへ登録
                    playingInfo.ActiveSignalEvents.Add(signalEvent);
                }
            }
            
            // リストのソート(終了時間の降順)
            playingInfo.ActiveRangeEvents.Sort((a, b) => b.exitTime.CompareTo(a.exitTime));
            playingInfo.ActiveSignalEvents.Sort((a, b) => b.time.CompareTo(a.time));

            return playingInfo;
        }
    }
}