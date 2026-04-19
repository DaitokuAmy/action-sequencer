using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ActionSequencer.Tests {
    /// <summary>
    /// SequencePlayer の再生挙動を検証するテスト
    /// </summary>
    [TestFixture]
    public sealed class SequencePlayerTests {
        private readonly List<ScriptableObject> _createdObjects = new();

        /// <summary>
        /// テストごとに生成した ScriptableObject を破棄
        /// </summary>
        [TearDown]
        public void TearDown() {
            SequencePlayer.ResetGlobalEventHandlers();

            for (var i = _createdObjects.Count - 1; i >= 0; i--) {
                var createdObject = _createdObjects[i];
                if (createdObject != null) {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            _createdObjects.Clear();
        }

        /// <summary>
        /// SignalEvent と RangeEvent が再生順に処理されることを検証
        /// </summary>
        [Test]
        public void Update_WhenSignalAndRangeEventsExist_InvokesHandlersInPlaybackOrder() {
            var clip = CreateClip(
                CreateSignalEvent<TestSignalSequenceEvent>(signalEvent => { signalEvent.time = 0.25f; }),
                CreateRangeEvent<TestRangeSequenceEvent>(rangeEvent => {
                    rangeEvent.enterTime = 0.10f;
                    rangeEvent.exitTime = 0.50f;
                }));

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindSignalEventHandler<TestSignalSequenceEvent>(_ => { log.Add("signal"); });
            player.BindRangeEventHandler<TestRangeSequenceEvent>(
                _ => { log.Add("enter"); },
                _ => { log.Add("exit"); },
                (_, elapsedTime) => { log.Add(GetUpdateLog(elapsedTime)); });

            var handle = player.Play(clip);

            Assert.That(player.HasPlayingClip, Is.True);
            Assert.That(handle.IsDone, Is.False);
            Assert.That(player.GetSequenceTime(clip), Is.EqualTo(0.0f).Within(0.0001f));

            player.Update(0.05f);
            Assert.That(log, Is.Empty);
            Assert.That(player.GetSequenceTime(clip), Is.EqualTo(0.05f).Within(0.0001f));

            player.Update(0.10f);
            player.Update(0.10f);
            player.Update(0.30f);

            Assert.That(log, Is.EqualTo(new[] {
                "enter",
                "update:0.05",
                "signal",
                "update:0.15",
                "update:0.40",
                "exit"
            }));
            Assert.That(handle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);
            Assert.That(player.GetSequenceTime(clip), Is.EqualTo(-1.0f).Within(0.0001f));
        }

        /// <summary>
        /// MustOneFrame な RangeEvent が次の更新まで維持されることを検証
        /// </summary>
        [Test]
        public void Update_WhenMustOneFrameRangeEventEndsImmediately_KeepsEventAliveUntilNextUpdate() {
            var clip = CreateClip(CreateRangeEvent<OneFrameRangeSequenceEvent>(rangeEvent => {
                rangeEvent.enterTime = 0.0f;
                rangeEvent.exitTime = 0.0f;
            }));

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindRangeEventHandler<OneFrameRangeSequenceEvent>(
                _ => { log.Add("enter"); },
                _ => { log.Add("exit"); },
                (_, elapsedTime) => { log.Add(GetUpdateLog(elapsedTime)); });

            var handle = player.Play(clip);

            player.Update(0.0f);

            Assert.That(log, Is.EqualTo(new[] { "enter", "update:0.00" }));
            Assert.That(handle.IsDone, Is.False);
            Assert.That(player.HasPlayingClip, Is.True);

            player.Update(0.0f);

            Assert.That(log, Is.EqualTo(new[] {
                "enter",
                "update:0.00",
                "update:0.00",
                "exit"
            }));
            Assert.That(handle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);
        }

        /// <summary>
        /// StopAll 実行時に再生中 RangeEvent の Cancel が呼ばれることを検証
        /// </summary>
        [Test]
        public void StopAll_WhenEnteredRangeEventExists_InvokesCancelInsteadOfExit() {
            var clip = CreateClip(CreateRangeEvent<TestRangeSequenceEvent>(rangeEvent => {
                rangeEvent.enterTime = 0.0f;
                rangeEvent.exitTime = 1.0f;
            }));

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindRangeEventHandler<TestRangeSequenceEvent>(
                _ => { log.Add("enter"); },
                _ => { log.Add("exit"); },
                null,
                _ => { log.Add("cancel"); });

            var handle = player.Play(clip);

            player.Update(0.25f);
            player.StopAll();

            Assert.That(log, Is.EqualTo(new[] { "enter", "cancel" }));
            Assert.That(handle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);
            Assert.That(player.GetSequenceTime(clip), Is.EqualTo(-1.0f).Within(0.0001f));
        }

        /// <summary>
        /// SequenceHandle から停止した場合も Cancel が呼ばれることを検証
        /// </summary>
        [Test]
        public void Stop_WhenHandleStopsPlayingRangeEvent_InvokesCancelInsteadOfExit() {
            var clip = CreateClip(CreateRangeEvent<TestRangeSequenceEvent>(rangeEvent => {
                rangeEvent.enterTime = 0.0f;
                rangeEvent.exitTime = 1.0f;
            }));

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindRangeEventHandler<TestRangeSequenceEvent>(
                _ => { log.Add("enter"); },
                _ => { log.Add("exit"); },
                null,
                _ => { log.Add("cancel"); });

            var handle = player.Play(clip);

            player.Update(0.25f);
            handle.Stop();

            Assert.That(log, Is.EqualTo(new[] { "enter", "cancel" }));
            Assert.That(handle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);
            Assert.That(player.GetSequenceTime(clip), Is.EqualTo(-1.0f).Within(0.0001f));
        }

        /// <summary>
        /// 同じ SignalEvent 型に複数ハンドラーを登録した場合に全件呼ばれることを検証
        /// </summary>
        [Test]
        public void Update_WhenMultipleSignalHandlersAreRegistered_InvokesAllHandlers() {
            var clip = CreateClip(CreateSignalEvent<TestSignalSequenceEvent>(signalEvent => { signalEvent.time = 0.1f; }));

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindSignalEventHandler<TestSignalSequenceEvent>(_ => { log.Add("signal-1"); });
            player.BindSignalEventHandler<TestSignalSequenceEvent>(_ => { log.Add("signal-2"); });
            player.BindSignalEventHandler<TestSignalSequenceEvent>(_ => { log.Add("signal-3"); });

            var handle = player.Play(clip);

            player.Update(0.1f);

            Assert.That(log, Is.EqualTo(new[] { "signal-1", "signal-2", "signal-3" }));
            Assert.That(handle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);
        }

        /// <summary>
        /// 同じ RangeEvent 型に複数ハンドラーを登録した場合に全件呼ばれることを検証
        /// </summary>
        [Test]
        public void Update_WhenMultipleRangeHandlersAreRegistered_InvokesAllHandlers() {
            var clip = CreateClip(CreateRangeEvent<TestRangeSequenceEvent>(rangeEvent => {
                rangeEvent.enterTime = 0.0f;
                rangeEvent.exitTime = 0.5f;
            }));

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindRangeEventHandler<TestRangeSequenceEvent>(
                _ => { log.Add("enter-1"); },
                _ => { log.Add("exit-1"); },
                (_, elapsedTime) => { log.Add(GetIndexedUpdateLog(1, elapsedTime)); });
            player.BindRangeEventHandler<TestRangeSequenceEvent>(
                _ => { log.Add("enter-2"); },
                _ => { log.Add("exit-2"); },
                (_, elapsedTime) => { log.Add(GetIndexedUpdateLog(2, elapsedTime)); });

            var handle = player.Play(clip);

            player.Update(0.25f);
            player.Update(0.25f);

            Assert.That(log, Is.EqualTo(new[] {
                "enter-1",
                "update-1:0.25",
                "enter-2",
                "update-2:0.25",
                "update-1:0.50",
                "exit-1",
                "update-2:0.50",
                "exit-2"
            }));
            Assert.That(handle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);
        }

        /// <summary>
        /// ローカルハンドラーが存在する場合にグローバルハンドラーより優先されることを検証
        /// </summary>
        [Test]
        public void Update_WhenLocalAndGlobalSignalHandlersExist_InvokesOnlyLocalHandlers() {
            var clip = CreateClip(CreateSignalEvent<TestSignalSequenceEvent>(signalEvent => { signalEvent.time = 0.1f; }));

            using var globalBinding = SequencePlayer.BindGlobalSignalEventHandler<TestSignalSequenceEvent>(_ => { Assert.Fail("global handler should not be invoked"); });
            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindSignalEventHandler<TestSignalSequenceEvent>(_ => { log.Add("local"); });

            var handle = player.Play(clip);

            player.Update(0.1f);

            Assert.That(log, Is.EqualTo(new[] { "local" }));
            Assert.That(handle.IsDone, Is.True);
        }

        /// <summary>
        /// includeClips に含まれるイベントも再生対象になることを検証
        /// </summary>
        [Test]
        public void Update_WhenClipIncludesOtherClips_InvokesEventsFromAllClips() {
            var includeClip = CreateClip(CreateSignalEvent<TestSignalSequenceEvent>(signalEvent => { signalEvent.time = 0.1f; }));
            var clip = CreateClip(CreateSignalEvent<TestSignalSequenceEvent>(signalEvent => { signalEvent.time = 0.2f; }));
            clip.includeClips = new[] { includeClip };

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindSignalEventHandler<TestSignalSequenceEvent>(sequenceEvent => { log.Add(GetSignalLog(sequenceEvent.time)); });

            var handle = player.Play(clip);

            player.Update(0.1f);
            player.Update(0.1f);

            Assert.That(log, Is.EqualTo(new[] { "signal:0.10", "signal:0.20" }));
            Assert.That(handle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);
        }

        /// <summary>
        /// 開始オフセット付き再生で過去時点のイベントが初回更新から反映されることを検証
        /// </summary>
        [Test]
        public void Update_WhenPlayStartsWithOffset_ProcessesEventsFromOffsetTime() {
            var clip = CreateClip(
                CreateSignalEvent<TestSignalSequenceEvent>(signalEvent => { signalEvent.time = 0.1f; }),
                CreateRangeEvent<TestRangeSequenceEvent>(rangeEvent => {
                    rangeEvent.enterTime = 0.2f;
                    rangeEvent.exitTime = 0.5f;
                }));

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindSignalEventHandler<TestSignalSequenceEvent>(_ => { log.Add("signal"); });
            player.BindRangeEventHandler<TestRangeSequenceEvent>(
                _ => { log.Add("enter"); },
                _ => { log.Add("exit"); },
                (_, elapsedTime) => { log.Add(GetUpdateLog(elapsedTime)); });

            var handle = player.Play(clip, 0.3f);

            Assert.That(player.GetSequenceTime(clip), Is.EqualTo(0.3f).Within(0.0001f));

            player.Update(0.0f);
            player.Update(0.2f);

            Assert.That(log, Is.EqualTo(new[] {
                "signal",
                "enter",
                "update:0.10",
                "update:0.30",
                "exit"
            }));
            Assert.That(handle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);
        }

        /// <summary>
        /// null や inactive なイベントが再生対象から除外されることを検証
        /// </summary>
        [Test]
        public void Update_WhenClipContainsNullTrackAndInactiveEvents_IgnoresInvalidEntries() {
            var activeEvent = CreateSignalEvent<TestSignalSequenceEvent>(signalEvent => { signalEvent.time = 0.1f; });
            var inactiveEvent = CreateSignalEvent<TestSignalSequenceEvent>(signalEvent => {
                signalEvent.time = 0.05f;
                signalEvent.active = false;
            });
            var track = CreateTrack((SequenceEvent)null, inactiveEvent, activeEvent);
            var clip = CreateAsset<SequenceClip>();
            clip.tracks = new SequenceTrack[] { null, track };

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindSignalEventHandler<TestSignalSequenceEvent>(_ => { log.Add("signal"); });

            var handle = player.Play(clip);

            player.Update(0.2f);

            Assert.That(log, Is.EqualTo(new[] { "signal" }));
            Assert.That(handle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);
        }

        /// <summary>
        /// ハンドラー未登録でも例外なく最後まで再生できることを検証
        /// </summary>
        [Test]
        public void Update_WhenNoHandlersAreRegistered_CompletesPlaybackWithoutErrors() {
            var clip = CreateClip(
                CreateSignalEvent<TestSignalSequenceEvent>(signalEvent => { signalEvent.time = 0.1f; }),
                CreateRangeEvent<TestRangeSequenceEvent>(rangeEvent => {
                    rangeEvent.enterTime = 0.0f;
                    rangeEvent.exitTime = 0.2f;
                }));

            using var player = new SequencePlayer();

            var handle = player.Play(clip);

            Assert.DoesNotThrow(() => {
                player.Update(0.1f);
                player.Update(0.1f);
            });
            Assert.That(handle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);
        }

        /// <summary>
        /// 同一イベントインスタンスが複数箇所から参照されても一度だけ処理されることを検証
        /// </summary>
        [Test]
        public void Update_WhenSameEventInstanceIsReferencedMultipleTimes_InvokesHandlerOnlyOnce() {
            var signalEvent = CreateSignalEvent<TestSignalSequenceEvent>(sequenceEvent => { sequenceEvent.time = 0.1f; });
            var firstTrack = CreateTrack(signalEvent);
            var secondTrack = CreateTrack(signalEvent);
            var clip = CreateAsset<SequenceClip>();
            clip.tracks = new[] { firstTrack, secondTrack };

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindSignalEventHandler<TestSignalSequenceEvent>(_ => { log.Add("signal"); });

            var handle = player.Play(clip);

            player.Update(0.1f);

            Assert.That(log, Is.EqualTo(new[] { "signal" }));
            Assert.That(handle.IsDone, Is.True);
        }

        /// <summary>
        /// Dispose で再生中イベントがキャンセルされローカルハンドラーが解除されることを検証
        /// </summary>
        [Test]
        public void Dispose_WhenPlaybackExists_CancelsActiveRangeAndClearsLocalHandlers() {
            var rangeClip = CreateClip(CreateRangeEvent<TestRangeSequenceEvent>(rangeEvent => {
                rangeEvent.enterTime = 0.0f;
                rangeEvent.exitTime = 1.0f;
            }));
            var signalClip = CreateClip(CreateSignalEvent<TestSignalSequenceEvent>(signalEvent => { signalEvent.time = 0.1f; }));

            using var player = new SequencePlayer();
            var log = new List<string>();

            player.BindRangeEventHandler<TestRangeSequenceEvent>(
                _ => { log.Add("enter"); },
                _ => { log.Add("exit"); },
                null,
                _ => { log.Add("cancel"); });
            player.BindSignalEventHandler<TestSignalSequenceEvent>(_ => { log.Add("signal"); });

            var rangeHandle = player.Play(rangeClip);

            player.Update(0.25f);
            player.Dispose();

            Assert.That(log, Is.EqualTo(new[] { "enter", "cancel" }));
            Assert.That(rangeHandle.IsDone, Is.True);
            Assert.That(player.HasPlayingClip, Is.False);

            var signalHandle = player.Play(signalClip);

            player.Update(0.1f);

            Assert.That(log, Is.EqualTo(new[] { "enter", "cancel" }));
            Assert.That(signalHandle.IsDone, Is.True);
        }

        /// <summary>
        /// 単一 Track を持つ Clip を生成
        /// </summary>
        /// <param name="sequenceEvents">Track に含めるイベント</param>
        /// <returns>生成した Clip</returns>
        private SequenceClip CreateClip(params SequenceEvent[] sequenceEvents) {
            var track = CreateTrack(sequenceEvents);
            var clip = CreateAsset<SequenceClip>();
            clip.tracks = new[] { track };

            return clip;
        }

        /// <summary>
        /// 指定イベントを含む Track を生成
        /// </summary>
        /// <param name="sequenceEvents">Track に含めるイベント</param>
        /// <returns>生成した Track</returns>
        private SequenceTrack CreateTrack(params SequenceEvent[] sequenceEvents) {
            var track = CreateAsset<SequenceTrack>();
            track.sequenceEvents = sequenceEvents ?? Array.Empty<SequenceEvent>();
            return track;
        }

        /// <summary>
        /// SignalEvent を生成
        /// </summary>
        /// <typeparam name="TEvent">生成するイベント型</typeparam>
        /// <param name="configure">生成後の設定処理</param>
        /// <returns>生成したイベント</returns>
        private TEvent CreateSignalEvent<TEvent>(Action<TEvent> configure = null)
            where TEvent : SignalSequenceEvent {
            var sequenceEvent = CreateAsset<TEvent>();
            configure?.Invoke(sequenceEvent);
            return sequenceEvent;
        }

        /// <summary>
        /// RangeEvent を生成
        /// </summary>
        /// <typeparam name="TEvent">生成するイベント型</typeparam>
        /// <param name="configure">生成後の設定処理</param>
        /// <returns>生成したイベント</returns>
        private TEvent CreateRangeEvent<TEvent>(Action<TEvent> configure = null)
            where TEvent : RangeSequenceEvent {
            var sequenceEvent = CreateAsset<TEvent>();
            configure?.Invoke(sequenceEvent);
            return sequenceEvent;
        }

        /// <summary>
        /// ScriptableObject を生成して破棄対象に登録
        /// </summary>
        /// <typeparam name="TAsset">生成するアセット型</typeparam>
        /// <returns>生成したアセット</returns>
        private TAsset CreateAsset<TAsset>()
            where TAsset : ScriptableObject {
            var asset = ScriptableObject.CreateInstance<TAsset>();
            _createdObjects.Add(asset);
            return asset;
        }

        /// <summary>
        /// RangeEvent の更新ログ文字列を生成
        /// </summary>
        /// <param name="elapsedTime">開始からの経過時間</param>
        /// <returns>ログ文字列</returns>
        private static string GetUpdateLog(float elapsedTime) {
            return FormattableString.Invariant($"update:{elapsedTime:0.00}");
        }

        /// <summary>
        /// ハンドラー番号付きの更新ログ文字列を生成
        /// </summary>
        /// <param name="index">ハンドラー番号</param>
        /// <param name="elapsedTime">開始からの経過時間</param>
        /// <returns>ログ文字列</returns>
        private static string GetIndexedUpdateLog(int index, float elapsedTime) {
            return FormattableString.Invariant($"update-{index}:{elapsedTime:0.00}");
        }

        /// <summary>
        /// SignalEvent のログ文字列を生成
        /// </summary>
        /// <param name="time">イベント時刻</param>
        /// <returns>ログ文字列</returns>
        private static string GetSignalLog(float time) {
            return FormattableString.Invariant($"signal:{time:0.00}");
        }

        /// <summary>
        /// SignalEvent テスト用イベント
        /// </summary>
        private sealed class TestSignalSequenceEvent : SignalSequenceEvent {
        }

        /// <summary>
        /// RangeEvent テスト用イベント
        /// </summary>
        private sealed class TestRangeSequenceEvent : RangeSequenceEvent {
        }

        /// <summary>
        /// 1 フレーム維持を検証するための RangeEvent
        /// </summary>
        private sealed class OneFrameRangeSequenceEvent : RangeSequenceEvent {
            public override bool MustOneFrame => true;
        }
    }
}
