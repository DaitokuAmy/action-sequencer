using System;
using System.Collections;

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
}
